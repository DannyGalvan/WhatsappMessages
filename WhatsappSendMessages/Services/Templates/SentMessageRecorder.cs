using WhatsappBusiness.CloudApi.Response;
using WhatsappSendMessages.Context;
using WhatsappSendMessages.Entities;

namespace WhatsappSendMessages.Services.Templates
{
    public class SentMessageRecorder(WhatsappMessagesContext context) : ISentMessageRecorder
    {
        public async Task RecordAsync(WhatsAppResponse response, string templateName, CancellationToken cancellationToken)
        {
            MessagesTemplate message = new()
            {
                MessageId = response.Messages[0].Id,
                MessageStatus = "accepted",
                WaId = response.Contacts[0].WaId,
                ContactInput = response.Contacts[0].Input,
                MessageTemplateName = templateName,
                MessagingProduct = response.MessagingProduct
            };

            context.MessagesTemplate.Add(message);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
