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

If you don't see private key parameters (d, p, q, dp, dq, qi) in the exported keys, that's actually expected behavior due to Azure Key Vault's security design.

Here's why you only see public key components:

1. **By design, Azure Key Vault doesn't allow export of private key material**:
   * For keys generated inside Azure Key Vault (as opposed to imported), the private key material is specifically designed to be non-exportable
   * This is a critical security feature of Azure Key Vault as a Hardware Security Module (HSM)
   * When you create a key in Key Vault, the private material never leaves the secured environment

2. **What you can export**:
   * Public key components (n, e)
   * Key metadata (name, enabled status, activation/expiration dates, etc.)
   * Tags and other attributes

3. **What you cannot export**:
   * Private key components (d, p, q, dp, dq, qi)
   * The actual secret material needed for decryption or signing

This is an important security principle: **keys generated in Azure Key Vault are designed to be used there but not extracted**. The only keys where you would see private key material in an export would be those that you previously imported with private key material.

To clarify the tool's actual capabilities:

1. It can export:
   * Complete keys (public and private material) for keys that were originally imported with private material
   * Only public key material for keys generated within Azure Key Vault

2. It can import:
   * Complete keys with private material (if you have that material)
   * Public-only keys (which would only be useful for encryption operations)

If you need to truly "move" keys with their private material between key vaults, you would need to:
1. Import the original key material into the first key vault (rather than generating it there)
2. Export and then import to the second vault

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
