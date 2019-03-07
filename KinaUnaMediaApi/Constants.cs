namespace KinaUnaMediaApi
{
    public static class Constants
    {
        public const string AppName = "KinaUnaDemo";
        public const string WebAppUrl = "https://kinaunademoweb.azurewebsites.net"; // Url of Web App, find it in Azure portal in the App Service's overview.

        public const string CloudBlobBase = "https://kinaunademostorage.blob.core.windows.net/"; // Storage end point, found in Azure portal under the storage account's properties: Primary Blob Service Endpoint
        public const string CloudBlobUsername = "kinaunademostorage"; // Storage account name, found in Azure portal under Azure Key Vault - Access Keys.
        public const string KeyVaultEndPoint = "https://kinaunademo.vault.azure.net/"; // Address of the Azure Key Vault, shown as DNS Name in the Azure Portal

        public const string AdminEmail = "per.mogensen@gmail.com";
        public const string DefaultTimezone = "Romance Standard Time";
    }
}
