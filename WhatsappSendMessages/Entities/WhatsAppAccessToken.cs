namespace WhatsappSendMessages.Entities
{
    public class WhatsAppAccessToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
