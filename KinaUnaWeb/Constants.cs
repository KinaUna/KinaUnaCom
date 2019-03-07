namespace KinaUnaWeb
{
    public static class Constants
    {
        public const string AppName = "KinaUnaDemo";
        public const string WebAppUrl = "https://kinaunademoweb.azurewebsites.net"; // Url of Web App, find it in Azure portal in the App Service's overview.
        public const string AuthAppUrl = "https://kinaunademoauth.azurewebsites.net"; // Url of Auth App, find it in Azure portal in the App Service's overview.
        public const string ProgenyApiUrl = "https://kinaunademoprogeny.azurewebsites.net"; // // Url of Progeny App, find it in Azure portal in the App Service's overview.
        public const string MediaApiUrl = "https://kinaunademomedia.azurewebsites.net"; // Url of Media App, find it in Azure portal in the App Service's overview.
        public const string AppRootDomain = "azurewebsites.net";
        public const string SmtpServer = "smtp.office365.com";
        public const string SmtpUsername = "support@kinauna.com";
        public const string SmtpFrom = "support@kinauna.com";
        public const string SupportEmail = "support@kinauna.com";
        public const string CloudBlobBase = "https://kinaunademostorage.blob.core.windows.net/"; // Storage end point, found in Azure portal under the storage account's properties: Primary Blob Service Endpoint
        public const string CloudBlobUsername = "kinaunademostorage"; // Storage account name, found in Azure portal under Azure Key Vault - Access Keys.
        public const string KeyVaultEndPoint = "https://kinaunademo.vault.azure.net/"; // Address of the Azure Key Vault, shown as DNS Name in the Azure Portal

        public const string AdminEmail = "per.mogensen@gmail.com";
        public const string OwnerName = "Per Rosing Mogensen";

        public const int DefaultChildId = 2;
        public const string DefaultUserEmail = "testuser@kinauna.com";
        public const string DefaultTimezone = "Romance Standard Time";

        public const string LanguageCookieName = "KinaUnaLanguage";

        public const string ProfilePictureUrl = "https://kinaunademoweb.azurewebsites.net/photodb/profile.jpg";

        public const string HereMapsId = "bJjORf0UVOc5U4LjADgX"; // Here.com API Id
        public const string HereMapsCode = "GNDm65qsmujcmIK9-2X_Uw"; // Here.com API Code

        public const string ProgenyApiName = "kinaunaprogenyapi";
        public const string MediaApiName = "kinaunamediaapi";
    }
}
