using WhatsappBusiness.CloudApi.Messages.Requests;
using WhatsappSendMessages.Entities.Request;

namespace WhatsappSendMessages.Services.Templates
{
    /// <summary>
    /// Mapea el request del API a la estructura que espera la libreria de Meta
    /// (language code, componente body, etc). Se extrae del controller para
    /// poder testearlo de forma aislada.
    /// </summary>
    public interface ITextTemplateMessageRequestFactory
    {
        TextTemplateMessageRequest Create(TemplateRequest request);
    }
}
