// Program.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;

namespace AzureKeyVaultTool
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Parse and validate arguments
            var arguments = Arguments.Parse(args);
            if (arguments == null)
            {
                return 1; // Error exit code
            }

            // Display the arguments that will be used
            Console.WriteLine("Using arguments:");
            Console.WriteLine(arguments.ToString());
            Console.WriteLine();

            try
            {
                // Execute the appropriate command
                if (arguments.Command == "export")
                {
                    await ExportKeys(arguments.KeyVaultName, arguments.FilePath, arguments.IncludeAllVersions);
                    return 0;
                }
                else if (arguments.Command == "import")
                {
                    await ImportKeys(arguments.KeyVaultName, arguments.FilePath, arguments.SkipExisting);
                    return 0;
                }
                else
                {
                    Console.WriteLine($"Unknown command: {arguments.Command}");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
                return 1;
            }
        }

        private static async Task ExportKeys(string keyVaultName, string filePath, bool includeAllVersions)
        {
            try
            {
                Console.WriteLine($"Exporting keys from KeyVault: {keyVaultName}");
                Console.WriteLine($"Output file: {filePath}");
                Console.WriteLine($"Include all versions: {(includeAllVersions ? "Yes" : "No")}");

                var keyVaultUrl = $"https://{keyVaultName}.vault.azure.net/";
                var credential = new DefaultAzureCredential();
                var keyClient = new KeyClient(new Uri(keyVaultUrl), credential);

                var keyExportList = new List<KeyExportModel>();

                await foreach (var keyProperties in keyClient.GetPropertiesOfKeysAsync())
                {
                    Console.WriteLine($"Processing key: {keyProperties.Name}");

                    if (includeAllVersions)
                    {
                        await foreach (var keyVersionProperties in keyClient.GetPropertiesOfKeyVersionsAsync(keyProperties.Name))
                        {
                            var keyWithVersion = await keyClient.GetKeyAsync(keyProperties.Name, keyVersionProperties.Version);

                            // Get the key with its material
                            var fullKey = await keyClient.GetKeyAsync(keyProperties.Name, keyVersionProperties.Version);

                            // Export the key with its material
                            var keyExportModel = CreateKeyExportModel(fullKey.Value);
                            keyExportList.Add(keyExportModel);

                            Console.WriteLine($"  - Exported version: {keyVersionProperties.Version}");
                        }
                    }
                    else
                    {
                        // Get the key with its material
                        var key = await keyClient.GetKeyAsync(keyProperties.Name);

                        // Export the key with its material
                        var keyExportModel = CreateKeyExportModel(key.Value);
                        keyExportList.Add(keyExportModel);
                    }
                }

                // Write to file
                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                string json = JsonSerializer.Serialize(keyExportList, jsonOptions);
                await File.WriteAllTextAsync(filePath, json);

                Console.WriteLine($"Export completed successfully. {keyExportList.Count} keys exported to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting keys: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
                throw; // Rethrow to handle in Main
            }
        }

        private static async Task ImportKeys(string keyVaultName, string filePath, bool skipExisting)
        {
            try
            {
                Console.WriteLine($"Importing keys to KeyVault: {keyVaultName}");
                Console.WriteLine($"Input file: {filePath}");
                Console.WriteLine($"Skip existing keys: {(skipExisting ? "Yes" : "No")}");

                var keyVaultUrl = $"https://{keyVaultName}.vault.azure.net/";
                var credential = new DefaultAzureCredential();
                var keyClient = new KeyClient(new Uri(keyVaultUrl), credential);

                string json = await File.ReadAllTextAsync(filePath);
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var keysToImport = JsonSerializer.Deserialize<List<KeyExportModel>>(json, jsonOptions);

                if (keysToImport == null || keysToImport.Count == 0)
                {
                    Console.WriteLine("No keys found in the import file.");
                    return;
                }

                Console.WriteLine($"Found {keysToImport.Count} keys in file.");

                int importedCount = 0;
                int skippedCount = 0;
                int errorCount = 0;

                foreach (var keyModel in keysToImport)
                {
                    try
                    {
                        // Validate key model required fields
                        if (string.IsNullOrWhiteSpace(keyModel.Name))
                        {
                            Console.WriteLine("Error: Key name is missing in import file.");
                            errorCount++;
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(keyModel.KeyType))
                        {
                            Console.WriteLine($"Error: Key type is missing for key '{keyModel.Name}'.");
                            errorCount++;
                            continue;
                        }

                        bool skipKey = false;

                        try
                        {
                            var existingKey = await keyClient.GetKeyAsync(keyModel.Name);

                            if (skipExisting)
                            {
                                Console.WriteLine($"Skipping existing key: {keyModel.Name}");
                                skippedCount++;
                                skipKey = true;
                            }
                            else
                            {
                                Console.WriteLine($"Warning: Key '{keyModel.Name}' already exists. Will update properties.");
                            }
                        }
                        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
                        {
                            // Key doesn't exist, which is fine for import
                            skipKey = false;
                        }

                        if (skipKey)
                            continue;

                        // Parse key type string to KeyType enum
                        KeyType keyType;
                        if (!Enum.TryParse(keyModel.KeyType, out keyType))
                        {
                            Console.WriteLine($"Error: Invalid key type '{keyModel.KeyType}' for key '{keyModel.Name}'. Valid types are: EC, EC-HSM, RSA, RSA-HSM, Oct, Oct-HSM");
                            errorCount++;
                            continue;
                        }

                        // Check if we have key material to import
                        if (keyModel.KeyParameters == null)
                        {
                            Console.WriteLine($"Warning: No key material found for key '{keyModel.Name}'. Creating a new key instead.");

                            // Create options for the key
                            var keyOptions = new CreateKeyOptions
                            {
                                // Set key attributes
                                Enabled = keyModel.Enabled
                            };

                            // Set expiration date if provided
                            if (keyModel.ExpiresOn.HasValue)
                                keyOptions.ExpiresOn = keyModel.ExpiresOn.Value;

                            // Set activation date if provided
                            if (keyModel.NotBefore.HasValue)
                                keyOptions.NotBefore = keyModel.NotBefore.Value;

                            // Add tags if any
                            if (keyModel.Tags != null && keyModel.Tags.Count > 0)
                            {
                                foreach (var tag in keyModel.Tags)
                                {
                                    keyOptions.Tags.Add(tag.Key, tag.Value);
                                }
                            }

                            // Add key operations if any
                            if (keyModel.KeyOperations != null && keyModel.KeyOperations.Count > 0)
                            {
                                foreach (var operation in keyModel.KeyOperations)
                                {
                                    if (!keyOptions.KeyOperations.Contains(operation))
                                        keyOptions.KeyOperations.Add(operation);
                                }
                            }

                            // Create a new key (since we don't have material to import)
                            var keyResult = await keyClient.CreateKeyAsync(keyModel.Name, keyType, keyOptions);

                            Console.WriteLine($"Successfully created new key: {keyModel.Name}");
                        }
                        else
                        {
                            // For importing key material directly, we need to use ImportKeyOptions
                            // but its structure is different in newer SDK versions

                            // Check if we have valid key parameters
                            if (keyModel.KeyParameters == null)
                            {
                                Console.WriteLine($"Error: No valid key material found for key '{keyModel.Name}'.");
                                errorCount++;
                                continue;
                            }

                            try
                            {
                                // Create the key with properties similar to what we had before
                                var keyResult = await keyClient.ImportKeyAsync(
                                    new ImportKeyOptions(
                                        keyModel.Name,
                                        keyModel.KeyParameters
                                    )
                                );

                                // Now update the key properties separately if needed
                                // First check if we need to update properties
                                bool needPropertiesUpdate = keyModel.Enabled != null ||
                                                        keyModel.ExpiresOn.HasValue ||
                                                        keyModel.NotBefore.HasValue ||
                                                        (keyModel.Tags != null && keyModel.Tags.Count > 0);

                                if (needPropertiesUpdate)
                                {
                                    // Create update options
                                    var updateOptions = new KeyProperties(keyModel.Name)
                                    {
                                        Enabled = keyModel.Enabled
                                    };

                                    // Set expiration date if provided
                                    if (keyModel.ExpiresOn.HasValue)
                                        updateOptions.ExpiresOn = keyModel.ExpiresOn.Value;

                                    // Set activation date if provided
                                    if (keyModel.NotBefore.HasValue)
                                        updateOptions.NotBefore = keyModel.NotBefore.Value;

                                    // Add tags if any
                                    if (keyModel.Tags != null && keyModel.Tags.Count > 0)
                                    {
                                        // Create a new dictionary for tags since Tags is read-only
                                        var newTags = new Dictionary<string, string>();
                                        foreach (var tag in keyModel.Tags)
                                        {
                                            newTags[tag.Key] = tag.Value;
                                        }

                                        // Set tags using reflection if needed
                                        // updateOptions.Tags will be set before updating
                                        var tagsProperty = updateOptions.GetType().GetProperty("Tags");
                                        if (tagsProperty != null && tagsProperty.CanWrite)
                                        {
                                            tagsProperty.SetValue(updateOptions, newTags);
                                        }
                                        else
                                        {
                                            Console.WriteLine($"Warning: Unable to set tags for key {keyModel.Name}");
                                        }
                                    }

                                    // Update key properties
                                    var updateResult = await keyClient.UpdateKeyPropertiesAsync(updateOptions);
                                    Console.WriteLine($"Updated properties for key: {keyModel.Name}");
                                }

                                Console.WriteLine($"Successfully imported key with material: {keyModel.Name}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error importing key {keyModel.Name} with material: {ex.Message}");
                                errorCount++;
                                continue;
                            }
                        }

                        // Apply rotation policy if provided
                        if (keyModel.RotationPolicy != null)
                        {
                            // Note: Rotation policy requires key management API
                            // Implementation would vary based on policy details
                            Console.WriteLine($"Note: Rotation policy for {keyModel.Name} needs to be set manually");
                        }

                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error importing key {keyModel.Name}: {ex.Message}");
                        errorCount++;
                    }
                }

                Console.WriteLine($"Import completed. Results: {importedCount} imported, {skippedCount} skipped, {errorCount} errors.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing keys: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
                throw; // Rethrow to handle in Main
            }
        }

        private static KeyExportModel CreateKeyExportModel(KeyVaultKey key)
        {
            var keyExportModel = new KeyExportModel
            {
                Name = key.Name,
                Version = key.Properties.Version,
                KeyType = key.KeyType.ToString(),
                Enabled = key.Properties.Enabled,
                Created = key.Properties.CreatedOn,
                Updated = key.Properties.UpdatedOn,
                NotBefore = key.Properties.NotBefore,
                ExpiresOn = key.Properties.ExpiresOn,
                KeyOperations = key.KeyOperations?.ToList(),
                RecoveryLevel = key.Properties.RecoveryLevel,
                RecoverableDays = key.Properties.RecoverableDays,
                Tags = key.Properties.Tags,
                KeyParameters = key.Key // This includes the actual key material
            };

            // Note: Rotation policy would need to be retrieved separately
            // This would require additional API calls with different permissions

            return keyExportModel;
        }
    }
}