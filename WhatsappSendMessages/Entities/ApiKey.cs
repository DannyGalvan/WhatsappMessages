using WhatsappSendMessages.Entities.Auditing;

namespace WhatsappSendMessages.Entities
{
    public class ApiKey : IAuditable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string KeyHash { get; set; } = string.Empty;
        public bool IsAdmin { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        // Auditoria: CreatedAt ya existia; el resto lo llena el interceptor.
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
