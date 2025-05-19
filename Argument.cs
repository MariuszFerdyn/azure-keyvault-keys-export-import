// Arguments.cs
using System;

namespace AzureKeyVaultTool
{
    public class Arguments
    {
        public string Command { get; }
        public string KeyVaultName { get; }
        public string FilePath { get; }
        public bool IncludeAllVersions { get; }
        public bool SkipExisting { get; }

        public Arguments(string command, string keyVaultName, string filePath,
                        bool includeAllVersions = false, bool skipExisting = false)
        {
            Command = command?.ToLowerInvariant() ?? "";
            KeyVaultName = keyVaultName ?? "";
            FilePath = filePath ?? "";
            IncludeAllVersions = includeAllVersions;
            SkipExisting = skipExisting;
        }

        public override string ToString()
        {
            string result = $"Command: {Command}\n" +
                            $"KeyVaultName: {KeyVaultName}\n" +
                            $"FilePath: {FilePath}";

            if (Command == "export")
            {
                result += $"\nIncludeAllVersions: {IncludeAllVersions}";
            }
            else if (Command == "import")
            {
                result += $"\nSkipExisting: {SkipExisting}";
            }

            return result;
        }

        public bool Validate(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(Command))
            {
                errorMessage = "Command is required. Use 'export' or 'import'.";
                return false;
            }

            if (Command != "export" && Command != "import")
            {
                errorMessage = $"Invalid command: '{Command}'. Use 'export' or 'import'.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(KeyVaultName))
            {
                errorMessage = "KeyVaultName is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(FilePath))
            {
                errorMessage = "FilePath is required.";
                return false;
            }

            // For import, check if file exists
            if (Command == "import" && !System.IO.File.Exists(FilePath))
            {
                errorMessage = $"Import file not found: {FilePath}";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public static Arguments Parse(string[] args)
        {
            try
            {
                if (args.Length < 3)
                {
                    PrintUsage();
                    return null;
                }

                string command = args[0];
                string keyVaultName = null;
                string filePath = null;
                bool includeAllVersions = false;
                bool skipExisting = false;

                // Process named parameters
                for (int i = 1; i < args.Length; i++)
                {
                    string arg = args[i];

                    if (arg == "--key-vault-name" && i + 1 < args.Length)
                    {
                        keyVaultName = args[++i];
                    }
                    else if (arg == "--file-path" && i + 1 < args.Length)
                    {
                        filePath = args[++i];
                    }
                    else if (arg == "--include-all-versions")
                    {
                        includeAllVersions = true;
                    }
                    else if (arg == "--skip-existing")
                    {
                        skipExisting = true;
                    }
                }

                Arguments arguments = new Arguments(command, keyVaultName, filePath, includeAllVersions, skipExisting);

                string error;
                if (!arguments.Validate(out error))
                {
                    Console.WriteLine($"Error: {error}");
                    PrintUsage();
                    return null;
                }

                return arguments;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing arguments: {ex.Message}");
                PrintUsage();
                return null;
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Azure KeyVault Keys Import/Export Tool");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("Required parameters:");
            Console.WriteLine("  Command: export or import");
            Console.WriteLine("  --key-vault-name: The name of the KeyVault (not the full URL)");
            Console.WriteLine("  --file-path: The file path where keys will be saved or read from");
            Console.WriteLine("\nExport command options:");
            Console.WriteLine("  --include-all-versions: Include all versions of keys (default: false)");
            Console.WriteLine("\nImport command options:");
            Console.WriteLine("  --skip-existing: Skip keys that already exist (default: false)");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  Export keys:");
            Console.WriteLine("    AzureKeyVaultTool export --key-vault-name myvault --file-path keys.json");
            Console.WriteLine("  Export all versions:");
            Console.WriteLine("    AzureKeyVaultTool export --key-vault-name myvault --file-path keys.json --include-all-versions");
            Console.WriteLine("  Import keys:");
            Console.WriteLine("    AzureKeyVaultTool import --key-vault-name myvault --file-path keys.json");
            Console.WriteLine("  Import and skip existing keys:");
            Console.WriteLine("    AzureKeyVaultTool import --key-vault-name myvault --file-path keys.json --skip-existing");
        }
    }
}