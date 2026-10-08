using WhatsappBusiness.CloudApi;
using WhatsappBusiness.CloudApi.Messages.Requests;
using WhatsappSendMessages.Entities.Request;

namespace WhatsappSendMessages.Services.Templates
{
    public class TextTemplateMessageRequestFactory : ITextTemplateMessageRequestFactory
    {
        public TextTemplateMessageRequest Create(TemplateRequest request)
        {
            var textTemplateMessage = new TextTemplateMessageRequest
            {
                To = request.Number,
                Template = new TextMessageTemplate
                {
                    Name = request.TemplateName,
                    Language = new TextMessageLanguage
                    {
                        Code = LanguageCode.Spanish_MEX
                    }
                }
            };

            if (request.Parameters != null)
            {
                textTemplateMessage.Template.Components = new List<TextMessageComponent>();

                TextMessageComponent textMessageComponent = new TextMessageComponent
                {
                    Parameters = [],
                    Type = "body"
                };

                foreach (var parameter in request.Parameters)
                {
                    textMessageComponent.Parameters.Add(new TextMessageParameter
                    {
                        ParameterName = parameter.ParameterName,
                        Type = parameter.Type,
                        Text = parameter.Text ?? ""
                    });
                }

                textTemplateMessage.Template.Components.Add(textMessageComponent);
            }

            return textTemplateMessage;
        }
    }
}
