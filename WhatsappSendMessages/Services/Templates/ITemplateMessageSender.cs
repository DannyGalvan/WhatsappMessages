using WhatsappBusiness.CloudApi.Response;
using WhatsappSendMessages.Entities.Request;

namespace WhatsappSendMessages.Services.Templates
{
    /// <summary>
    /// Orquesta el envio de una plantilla: factory -> config provider ->
    /// cliente WhatsApp -> recorder. Devuelve la respuesta cruda de Meta.
    /// </summary>
    public interface ITemplateMessageSender
    {
        Task<WhatsAppResponse> SendAsync(TemplateRequest request, CancellationToken cancellationToken);
    }
}
