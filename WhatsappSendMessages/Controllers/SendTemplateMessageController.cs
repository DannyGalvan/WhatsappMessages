using Lombok.NET;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsappBusiness.CloudApi;
using WhatsappBusiness.CloudApi.Interfaces;
using WhatsappBusiness.CloudApi.Messages.Requests;
using WhatsappBusiness.CloudApi.Response;
using WhatsappSendMessages.Authentication;
using WhatsappSendMessages.Context;
using WhatsappSendMessages.Entities;
using WhatsappSendMessages.Entities.Request;
using WhatsappSendMessages.Entities.Response;

namespace WhatsappSendMessages.Controllers
{
    [AllArgsConstructor]
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.AuthenticationScheme)]
    public partial class SendTemplateMessageController : ControllerBase
    {
        private readonly IWhatsAppBusinessClient _whatsAppBusinessClient;
        private readonly ILogger<SendTemplateMessageController> _logger;
        private readonly WhatsappMessagesContext _context;

        [HttpPost]
        public async Task<IActionResult> SendTemplate(TemplateRequest request, CancellationToken cancellationToken)
        {
            try
            {
                Response<WhatsAppResponse> response = new Response<WhatsAppResponse>();
                
                TextTemplateMessageRequest textTemplateMessage = new TextTemplateMessageRequest
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

                WhatsAppResponse results = await _whatsAppBusinessClient.SendTextMessageTemplateAsync(
                    textTemplateMessage, cancellationToken: cancellationToken);

                MessagesTemplate message = new()
                {
                    MessageId = results.Messages[0].Id,
                    MessageStatus = "accepted",
                    WaId = results.Contacts[0].WaId,
                    ContactInput = results.Contacts[0].Input,
                    MessageTemplateName = request.TemplateName,
                    MessagingProduct = results.MessagingProduct
                };

                _context.MessagesTemplate.Add(message);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Plantilla {plantilla} enviada con exito", request.TemplateName);

                response.Success = true;
                response.Message = "Plantilla enviada con exito";
                response.Data = results;

                return Ok(results);
            }
            catch (Exception e)
            {
                Response<string> response = new Response<string>
                {
                    Success = false,
                    Message = "Error al enviar la plantilla",
                    Data = e.Message
                };

                _logger.LogError(e, "Ha Ocurrido un error al enviar la plantilla {plantilla}", request.TemplateName);

                return BadRequest(response);
            }
        }
    }
}