using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using WhatsappSendMessages.Entities.Response;
using WhatsappSendMessages.Services;

namespace WhatsappSendMessages.Authentication
{
    // Handler de un scheme de autenticacion custom, igual que Cookies/JwtBearer traen los suyos.
    // Los controladores/acciones se protegen con [Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.AuthenticationScheme)],
    // igual que el Authorize nativo: sin el atributo, el endpoint queda abierto.
    public class ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyService apiKeyService)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.HeaderName, out var extractedApiKey) ||
                string.IsNullOrWhiteSpace(extractedApiKey))
            {
                return AuthenticateResult.Fail("API KEY no enviado, revise sus encabezados porfavor");
            }

            var validation = await apiKeyService.ValidateAsync(extractedApiKey!, Context.RequestAborted);
            if (validation is null)
            {
                return AuthenticateResult.Fail("API KEY invalida, revise porfavor");
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, validation.Id.ToString()),
                new(ClaimTypes.Name, validation.Name),
                new(ApiKeyAuthenticationDefaults.IsAdminClaimType, validation.IsAdmin ? "true" : "false")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.ContentType = "application/json";

            var body = new Response<string>
            {
                Success = false,
                Message = "API KEY no enviado o invalida, revise sus encabezados porfavor"
            };

            return Response.WriteAsJsonAsync(body);
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            Response.ContentType = "application/json";

            var body = new Response<string>
            {
                Success = false,
                Message = "No tiene permisos suficientes para acceder a este recurso"
            };

            return Response.WriteAsJsonAsync(body);
        }
    }
}
