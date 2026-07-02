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
    public class ApiKeysController(IApiKeyService apiKeyService) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            var keys = await apiKeyService.ListAsync(cancellationToken);
            var data = keys.Select(k => new ApiKeyDto
            {
                Id = k.Id,
                Name = k.Name,
                IsAdmin = k.IsAdmin,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt,
                ExpiresAt = k.ExpiresAt,
                RevokedAt = k.RevokedAt
            });

            return Ok(new Response<IEnumerable<ApiKeyDto>> { Success = true, Data = data });
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateApiKeyRequest request, CancellationToken cancellationToken)
        {
            var (entity, rawKey) = await apiKeyService.CreateAsync(
                request.Name, request.IsAdmin, request.ExpiresAt, cancellationToken);

            return Ok(new Response<ApiKeyCreatedResponse>
            {
                Success = true,
                Message = "Guarde esta key ahora, no se volvera a mostrar",
                Data = new ApiKeyCreatedResponse { Id = entity.Id, Name = entity.Name, RawKey = rawKey }
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Revoke(int id, CancellationToken cancellationToken)
        {
            var revoked = await apiKeyService.RevokeAsync(id, cancellationToken);
            if (!revoked)
                return NotFound(new Response<string> { Success = false, Message = "API key no encontrada o ya revocada" });

            return Ok(new Response<string> { Success = true, Message = "API key revocada" });
        }
    }
}
