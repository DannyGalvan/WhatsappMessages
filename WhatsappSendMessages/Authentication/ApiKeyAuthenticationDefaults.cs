namespace WhatsappSendMessages.Authentication
{
    public static class ApiKeyAuthenticationDefaults
    {
        public const string AuthenticationScheme = "ApiKey";
        public const string HeaderName = "X-API-KEY";
        public const string IsAdminClaimType = "IsAdmin";
        public const string AdminPolicy = "ApiKeyAdmin";
    }
}
