namespace WhatsappSendMessages.Configurations.Options
{
    /// <summary>
    /// Verify token que Meta envia en la suscripcion inicial del webhook.
    /// Default "12345" para mantener compatibilidad si la seccion
    /// "WhatsAppWebhook" no existe en appsettings.
    /// </summary>
    public class WhatsAppWebhookOptions
    {
        public const string DefaultVerifyToken = "12345";

        public string VerifyToken { get; set; } = DefaultVerifyToken;
    }
}
