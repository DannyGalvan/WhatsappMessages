namespace WhatsappSendMessages.Entities.Request
{
    public class TemplateParameter
    {
        public string Type { get; set; } = string.Empty;
        public string ParameterName { get; set; } = string.Empty;
        public string? Text { get; set; } = string.Empty;
    }
}
