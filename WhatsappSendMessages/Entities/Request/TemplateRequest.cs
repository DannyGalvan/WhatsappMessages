using WhatsappBusiness.CloudApi.Messages.Requests;

namespace WhatsappSendMessages.Entities.Request
{
    public class TemplateRequest
    {
        public string Number { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public List<TemplateParameter>? Parameters { get; set; }
    }
}
