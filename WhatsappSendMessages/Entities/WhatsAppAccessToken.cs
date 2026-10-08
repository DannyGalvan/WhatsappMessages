using WhatsappSendMessages.Entities.Auditing;

namespace WhatsappSendMessages.Entities
{
    public class WhatsAppAccessToken : IAuditable
    {
        public int Id { get; set; }
        public string Token { get; set; } = string.Empty;

        // Auditoria: UpdatedAt ya existia (no-null, asignado a mano); el resto
        // lo llena el interceptor. CreatedAt se backfilea desde UpdatedAt en
        // la migracion AddAuditColumns.
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }
}
