namespace WhatsappSendMessages.Entities.Request
{
    public class CreateApiKeyRequest
    {
        public string Name { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
