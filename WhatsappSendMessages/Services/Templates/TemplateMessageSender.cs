using WhatsappBusiness.CloudApi.Interfaces;
using WhatsappBusiness.CloudApi.Response;
using WhatsappSendMessages.Entities.Request;

namespace WhatsappSendMessages.Services.Templates
{
    public class TemplateMessageSender(
        ITextTemplateMessageRequestFactory factory,
        IWhatsAppBusinessClient client,
        IWhatsAppCloudApiConfigProvider configProvider,
        ISentMessageRecorder recorder) : ITemplateMessageSender
    {
        public async Task<WhatsAppResponse> SendAsync(TemplateRequest request, CancellationToken cancellationToken)
        {
            var textTemplateMessage = factory.Create(request);
            var cloudApiConfig = await configProvider.GetCurrentConfigAsync(cancellationToken);

            var response = await client.SendTextMessageTemplateAsync(
                textTemplateMessage, cloudApiConfig, cancellationToken);

            await recorder.RecordAsync(response, request.TemplateName, cancellationToken);

            return response;
        }
    }
}
