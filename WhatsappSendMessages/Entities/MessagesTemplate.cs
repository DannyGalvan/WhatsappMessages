namespace WhatsappSendMessages.Entities
{
    public class MessagesTemplate
    {
        public long Id { get; set; }
        public string MessagingProduct { get; set; } = string.Empty;
        public string ContactInput { get; set; } = string.Empty;
        public string WaId { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public string MessageStatus { get; set; } = string.Empty;
        public string MessageTemplateName { get; set; } = string.Empty;
    }
}
