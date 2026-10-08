using Lombok.NET;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsappSendMessages.Authentication;
using WhatsappSendMessages.Entities.Request;
using WhatsappSendMessages.Entities.Response;
using WhatsappSendMessages.Services.Templates;

namespace WhatsappSendMessages.Controllers
{
    [AllArgsConstructor]
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.AuthenticationScheme)]
    public partial class SendTemplateMessageController : ControllerBase
    {
        private readonly ITemplateMessageSender _sender;
        private readonly ILogger<SendTemplateMessageController> _logger;

        [HttpPost]
        public async Task<IActionResult> SendTemplate(TemplateRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var results = await _sender.SendAsync(request, cancellationToken);

                _logger.LogInformation("Plantilla {plantilla} enviada con exito", request.TemplateName);

                return Ok(results);
            }
            // OOM no se traga como BadRequest: debe llegar a OutOfMemoryRecoveryMiddleware
            // para que reinicie el proceso.
            catch (Exception e) when (e is not OutOfMemoryException)
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
