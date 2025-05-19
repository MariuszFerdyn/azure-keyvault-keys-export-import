# Azure KeyVault Keys Tool

A command-line tool for exporting and importing keys from Azure Key Vault, including their key material (when available), activation date, expiration date, tags, and rotation policy.

## Features

- Export keys from an Azure KeyVault with metadata and available key material
- Import keys to an Azure KeyVault with original key material (if available)
- Support for exporting all versions of keys
- Option to skip existing keys during import
- Preservation of key attributes including:
  - Key material (the actual cryptographic key, when exportable)
  - Activation date (NotBefore)
  - Expiration date (ExpiresOn)
  - Tags
  - Key operations
  - Recovery information

## Important Security Note on Key Material

**Azure KeyVault Security Design:** By default, keys generated within Azure KeyVault are designed to be non-exportable for security reasons. When exporting such keys:

1. **Keys generated in KeyVault**: Only public components will be exported (e.g., for RSA keys, only 'n' and 'e' values)
2. **Keys imported into KeyVault**: Both public and private components can be exported if they were originally imported with private material

When importing keys with only public components:
- They can only be used for encryption operations
- They cannot be used for decryption or signing operations
- The tool will clearly identify these limitations during import

This limitation is due to Azure KeyVault's security design and not a limitation of this tool. The tool displays clear warnings and counts of keys with complete or partial material.

## Prerequisites

- .NET 9.0 SDK
- Azure account with access to KeyVault
- Proper permissions to access KeyVault keys (**including Get Key permission**)

## Getting Started

### Building the Application

```bash
dotnet build
```

### Running the Application

#### Export Keys

Export current versions of keys:
```bash
AzureKeyVaultTool export --key-vault-name mykeyvault --file-path keys.json
```

Export all versions of keys:
```bash
AzureKeyVaultTool export --key-vault-name mykeyvault --file-path keys.json --include-all-versions
```

#### Import Keys

Import keys to a KeyVault:
```bash
AzureKeyVaultTool import --key-vault-name targetkeyvault --file-path keys.json
```

Import keys but skip existing ones:
```bash
AzureKeyVaultTool import --key-vault-name targetkeyvault --file-path keys.json --skip-existing
```

## Complete Key Material Backup Workflow

To fully backup keys with their private material, you must:

1. **Original Import**: First import keys with private material to the source KeyVault
   ```bash
   # This step is done outside of this tool using other methods
   ```

2. **Export Keys**: Use this tool to export the keys with their material
   ```bash
   AzureKeyVaultTool export --key-vault-name sourceVault --file-path keys.json
   ```

3. **Import Keys**: Import the keys to the target KeyVault
   ```bash
   AzureKeyVaultTool import --key-vault-name targetVault --file-path keys.json
   ```

## Authentication

This tool uses DefaultAzureCredential from the Azure.Identity package, which supports multiple authentication methods:

1. Environment variables
2. Managed identities
3. Visual Studio authentication
4. Azure CLI authentication
5. Interactive browser authentication

The simplest way to authenticate during development is to use the Azure CLI:

```bash
az login
```

## Key Properties Details

The tool exports and imports the following properties for each key:
- Name and version
- Key type (RSA, EC, etc.)
- Enabled status
- Creation and update dates
- Activation date (NotBefore)
- Expiration date (ExpiresOn)
- Key operations (allowed operations like encrypt, decrypt, sign, verify)
- Tags and metadata
- Key material (public parts always, private parts when available)
- Recovery information

## Notes on Rotation Policies

The current implementation includes placeholders for rotation policies. To fully implement rotation policy management, additional API calls with specific permissions would be required.

## Project Structure

- `Program.cs`: Main application logic
- `Arguments.cs`: Command-line argument parsing and validation
- `KeyExportModel.cs`: Data model for key export/import

## License

[MIT](LICENSE)
# Azure KeyVault Keys Tool

A command-line tool for exporting and importing keys from Azure Key Vault, including their key material, activation date, expiration date, tags, and rotation policy.

## Features

- Export keys from an Azure KeyVault **including their key material**
- Import keys to an Azure KeyVault with the same key material
- Support for exporting all versions of keys
- Option to skip existing keys during import
- Preservation of key attributes including:
  - Key material (the actual cryptographic key)
  - Activation date (NotBefore)
  - Expiration date (ExpiresOn)
  - Tags
  - Key operations
  - Recovery information

## Important Security Note

This tool exports the actual key material from Azure Key Vault. Be extremely careful with the exported files as they contain sensitive cryptographic material. Always ensure:

1. Exported key files are protected with appropriate permissions
2. Key material is not exposed to unauthorized individuals
3. Files are securely deleted when no longer needed
4. Transport of key material is done via secure channels

## Prerequisites

- .NET 9.0 SDK
- Azure account with access to KeyVault
- Proper permissions to access KeyVault keys (**including Get Key permission**)

## Getting Started

### Building the Application

```bash
dotnet build
```

### Running the Application

#### Export Keys

Export current versions of keys:
```bash
AzureKeyVaultTool export --key-vault-name mykeyvault --file-path keys.json
```

Export all versions of keys:
```bash
AzureKeyVaultTool export --key-vault-name mykeyvault --file-path keys.json --include-all-versions
```

#### Import Keys

Import keys to a KeyVault:
```bash
AzureKeyVaultTool import --key-vault-name targetkeyvault --file-path keys.json
```

Import keys but skip existing ones:
```bash
AzureKeyVaultTool import --key-vault-name targetkeyvault --file-path keys.json --skip-existing
```

## Authentication

This tool uses DefaultAzureCredential from the Azure.Identity package, which supports multiple authentication methods:

1. Environment variables
2. Managed identities
3. Visual Studio authentication
4. Azure CLI authentication
5. Interactive browser authentication

The simplest way to authenticate during development is to use the Azure CLI:

```bash
az login
```

## Notes on Rotation Policies

The current implementation includes placeholders for rotation policies. To fully implement rotation policy management, additional API calls with specific permissions would be required.

## Project Structure

- `Program.cs`: Main application logic
- `Arguments.cs`: Command-line argument parsing and validation
- `KeyExportModel.cs`: Data model for key export/import

## License

[MIT](LICENSE)
 parsing and validation
- `KeyExportModel.cs`: Data model for key export/import

## License

[MIT](LICENSE)
