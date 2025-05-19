# Azure Key Vault Tool - VS Code Project Structure

This document provides guidance on how to set up the project in Visual Studio Code.

## Project Structure

When you open the project in Visual Studio Code, your file structure should look like this:

```
AzureKeyVaultTool/
├── .vscode/
│   ├── launch.json
│   ├── tasks.json
│   └── settings.json
├── Program.cs
├── KeyExportModel.cs
├── AzureKeyVaultTool.csproj
├── AzureKeyVaultTool.sln
└── README.md
```

## Setting Up the Project

1. Create a new folder named `AzureKeyVaultTool`
2. Copy all the provided files into this folder
3. Create a `.vscode` subfolder and place the following files inside:
   - `launch.json`
   - `tasks.json`
   - `settings.json`

## Building and Running

1. Open the folder in Visual Studio Code
2. Ensure the C# extension is installed
3. Open a terminal in VS Code (Terminal > New Terminal)
4. Run the following commands:

```bash
# Restore packages
dotnet restore

# Build the project
dotnet build

# Run the application (example)
dotnet run export --key-vault-name your-keyvault-name --file-path keys-export.json
```

## Debugging

You can debug the application using the provided launch configurations:

1. Select "Run and Debug" in the VS Code sidebar
2. Choose either "Export Keys" or "Import Keys" from the dropdown
3. Modify the arguments in launch.json to match your KeyVault name and file paths
4. Press F5 to start debugging

## Dependencies

The project is configured to automatically restore the following NuGet packages:

- Azure.Identity (1.10.4)
- Azure.Security.KeyVault.Keys (4.6.0)
- System.CommandLine (2.0.0-beta4.22272.1)

## .NET 9 Requirements

This project targets .NET 9.0. Make sure you have the .NET 9 SDK installed on your machine. You can check your installed .NET versions with:

```bash
dotnet --list-sdks
```

If you don't have .NET 9 installed, you can download it from the official .NET website.

## Authentication

Make sure you're authenticated with Azure before running the tool:

```bash
az login
```

Or set up appropriate environment variables for DefaultAzureCredential.
