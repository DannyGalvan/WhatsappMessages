using Microsoft.AspNetCore.Authentication;
using WhatsappSendMessages.Authentication;
using WhatsappSendMessages.Services;

namespace WhatsappSendMessages.Configurations.Extensions
{
    public static class ApiKeyAuthenticationServiceExtensions
    {
        // Autenticacion por API key como scheme propio, igual patron que Cookies/JwtBearer:
        // los endpoints se protegen con [Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.AuthenticationScheme)].
        // Sin el atributo, el endpoint queda publico (igual que el Authorize nativo de .NET).
        public static IServiceCollection AddApiKeyAuthentication(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddScoped<IApiKeyService, ApiKeyService>();

            services.AddAuthentication(ApiKeyAuthenticationDefaults.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                    ApiKeyAuthenticationDefaults.AuthenticationScheme, _ => { });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(ApiKeyAuthenticationDefaults.AdminPolicy, policy => policy
                    .AddAuthenticationSchemes(ApiKeyAuthenticationDefaults.AuthenticationScheme)
                    .RequireClaim(ApiKeyAuthenticationDefaults.IsAdminClaimType, "true"));
            });

            return services;
        }
    }
}
