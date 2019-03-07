namespace KinaUna.IDP
{
    public static class Constants
    {
        public const string AppName = "KinaUnaDemo";
        public const string WebAppUrl = "https://kinaunademoweb.azurewebsites.net"; // Url of Web App, find it in Azure portal in the App Service's overview.
        public const string AuthAppUrl = "https://kinaunademoauth.azurewebsites.net"; // Url of Auth App, find it in Azure portal in the App Service's overview.
        public const string ProgenyApiUrl = "https://kinaunademoprogeny.azurewebsites.net"; // // Url of Progeny App, find it in Azure portal in the App Service's overview.
        public const string MediaApiUrl = "https://kinaunademomedia.azurewebsites.net"; // Url of Media App, find it in Azure portal in the App Service's overview.
        public const string AppRootDomain = "azurewebsites.net";
        public const string SmtpServer = "smtp.office365.com"; // Mail server address.
        public const string SmtpUsername = "support@kinauna.com"; // Mail server user name.
        public const string SmtpFrom = "support@kinauna.com"; // From email address for notifications, used for confirmation emails, password reset.
        public const string SupportEmail = "support@kinauna.com"; // Contact email address.
        public const string KeyVaultEndPoint = "https://kinaunademo.vault.azure.net/"; // Address of the Azure Key Vault, shown as DNS Name in the Azure Portal

        public const string AdminEmail = "per.mogensen@gmail.com";
        public const string OwnerName = "Per Rosing Mogensen";

        public const string LanguageCookieName = "KinaUnaLanguage";

        public const string ProgenyApiName = "kinaunaprogenyapi";
        public const string MediaApiName = "kinaunamediaapi";

    }
}
