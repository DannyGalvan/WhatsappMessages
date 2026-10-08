using WhatsappBusiness.CloudApi.Response;
using WhatsappSendMessages.Entities;

namespace WhatsappSendMessages.Services.Templates
{
    /// <summary>
    /// Persiste un WhatsAppResponse aceptado por Meta en MessagesTemplate.
    /// Si la respuesta no trae Messages[0] o Contacts[0], se considera error
    /// de upstream y se propaga como excepcion (mismo comportamiento que el
    /// controller original).
    /// </summary>
    public interface ISentMessageRecorder
    {
        Task RecordAsync(WhatsAppResponse response, string templateName, CancellationToken cancellationToken);
    }
}
