namespace KinaUnaProgenyApi
{
    public static class Constants
    {
        public const string CloudBlobBase = "https://kinaunademostorage.blob.core.windows.net/"; // Storage end point, found in Azure portal under the storage account's properties: Primary Blob Service Endpoint
        public const string CloudBlobUsername = "kinaunademostorage"; // Storage account name, found in Azure portal under Azure Key Vault - Access Keys.
        public const string KeyVaultEndPoint = "https://kinaunademo.vault.azure.net/"; // Address of the Azure Key Vault, shown as DNS Name in the Azure Portal
    }
}
