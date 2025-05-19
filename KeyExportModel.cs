// KeyExportModel.cs
using System;
using System.Collections.Generic;
using Azure.Security.KeyVault.Keys;

namespace AzureKeyVaultTool
{
    public class KeyExportModel
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string KeyType { get; set; }
        public bool? Enabled { get; set; }
        public DateTimeOffset? Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        public DateTimeOffset? ExpiresOn { get; set; }
        public DateTimeOffset? NotBefore { get; set; }
        public IList<KeyOperation> KeyOperations { get; set; }
        public string RecoveryLevel { get; set; }
        public int? RecoverableDays { get; set; }
        public IDictionary<string, string> Tags { get; set; }
        public object RotationPolicy { get; set; } // Using object for flexibility

        // Add key material properties
        public string KeyMaterial { get; set; } // Base64-encoded key material
        public JsonWebKey KeyParameters { get; set; } // Direct access to the key parameters
    }
}