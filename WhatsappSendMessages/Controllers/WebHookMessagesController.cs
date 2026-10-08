using Lombok.NET;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WhatsappSendMessages.Configurations.Options;

namespace WhatsappSendMessages.Controllers
{
    [AllArgsConstructor]
    [Route("api/v1/[controller]")]
    [ApiController]
    public partial class WebHookMessagesController : ControllerBase
    {
        private readonly ILogger<WebHookMessagesController> _logger;
        private readonly IOptions<WhatsAppWebhookOptions> _options;

        [HttpGet]
        public ActionResult<string> ConfigureWhatsAppMessageWebhook([FromQuery(Name = "hub.mode")] string? hubMode,
            [FromQuery(Name = "hub.challenge")] string? hubChallenge,
            [FromQuery(Name = "hub.verify_token")] string? hubVerifyToken)
        {

            if (hubMode == "subscribe" && hubVerifyToken == _options.Value.VerifyToken)
            {
                _logger.LogInformation("El WebHook ha sido verificado con exito");
                return Ok(hubChallenge);
            }

            _logger.LogWarning("el token no es el correcto {token}", hubVerifyToken);

            return Unauthorized("forbiden");
        }
    }
}
