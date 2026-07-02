using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsappSendMessages.Authentication;
using WhatsappSendMessages.Entities.Request;
using WhatsappSendMessages.Entities.Response;
using WhatsappSendMessages.Services;

namespace WhatsappSendMessages.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.AuthenticationScheme,
        Policy = ApiKeyAuthenticationDefaults.AdminPolicy)]
    public class WhatsAppAccessTokenController(IWhatsAppCloudApiConfigProvider configProvider) : ControllerBase
    {
        [HttpPut]
        public async Task<IActionResult> Update(UpdateWhatsAppAccessTokenRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.AccessToken))
                return BadRequest(new Response<string> { Success = false, Message = "El AccessToken es requerido" });

            await configProvider.SetAccessTokenAsync(request.AccessToken, cancellationToken);

            return Ok(new Response<string> { Success = true, Message = "AccessToken de WhatsApp actualizado" });
        }
    }
}
