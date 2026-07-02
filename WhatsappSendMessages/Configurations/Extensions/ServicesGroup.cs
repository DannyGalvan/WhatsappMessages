using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using WhatsappBusiness.CloudApi.Configurations;
using WhatsappBusiness.CloudApi.Extensions;
using WhatsappBusiness.CloudApi.Interfaces;
using WhatsappSendMessages.Authentication;
using WhatsappSendMessages.Context;
using WhatsappSendMessages.Services;
using Serilog;
using Serilog.Extensions.Logging;


namespace WhatsappSendMessages.Configurations.Extensions
{
    public static class ServicesGroup
    {
        public static IServiceCollection AddServicesGroup(this IServiceCollection services, IConfiguration config)
        {

            services.AddDbContext<WhatsappMessagesContext>(options =>
            {
                options.UseSqlServer(config.GetConnectionString("WhatsAppMessages"));
            });

            services.AddMemoryCache();
            services.AddScoped<IApiKeyService, ApiKeyService>();
            services.AddScoped<IWhatsAppCloudApiConfigProvider, WhatsAppCloudApiConfigProvider>();

            // Autenticacion por API key como scheme propio, igual patron que Cookies/JwtBearer:
            // los endpoints se protegen con [Authorize(AuthenticationSchemes = ApiKeyAuthenticationDefaults.AuthenticationScheme)].
            // Sin el atributo, el endpoint queda publico (igual que el Authorize nativo de .NET).
            services.AddAuthentication(ApiKeyAuthenticationDefaults.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                    ApiKeyAuthenticationDefaults.AuthenticationScheme, _ => { });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(ApiKeyAuthenticationDefaults.AdminPolicy, policy => policy
                    .AddAuthenticationSchemes(ApiKeyAuthenticationDefaults.AuthenticationScheme)
                    .RequireClaim(ApiKeyAuthenticationDefaults.IsAdminClaimType, "true"));
            });

            services.AddLogging(loggingBuilder =>
            {
                // WebApplication.CreateBuilder ya registro los providers default (Console,
                // Debug, etc.), que leen de "Logging:LogLevel" y no de "Serilog:MinimumLevel".
                // Sin ClearProviders quedan corriendo en paralelo y los overrides del appsettings
                // (ej. bajar EF Core a Warning) nunca les aplican.
                loggingBuilder.ClearProviders();

                // Niveles, sinks (Console/MSSqlServer) y columnas extra se definen enteramente
                // en la seccion "Serilog" de appsettings; nada queda hardcodeado en C#.
                var log = new LoggerConfiguration()
                    .ReadFrom.Configuration(config)
                    .CreateLogger();

                loggingBuilder.AddProvider(new SerilogLoggerProvider(log));
            });

            WhatsAppBusinessCloudApiConfig whatsAppConfig = config.GetSection("WhatsAppBusinessCloudApiConfiguration")
                .Get<WhatsAppBusinessCloudApiConfig>()!;

            services.AddWhatsAppBusinessCloudApiService(whatsAppConfig, whatsAppConfig.Version);

            // La libreria registra el HttpClient tipado con Timeout de 10 minutos y sin
            // circuit breaker. Bajo un caida/lentitud del API de WhatsApp eso deja las
            // solicitudes colgadas reteniendo conexiones en vez de fallar rapido, causando
            // que se acumulen en produccion. Se reconfigura aqui (las opciones de un mismo
            // typed client se acumulan entre llamadas a AddHttpClient).
            services.AddHttpClient<IWhatsAppBusinessClient, WhatsappBusiness.CloudApi.WhatsAppBusinessClient>(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                })
                .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(20)))
                .AddPolicyHandler(HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)))
                // La libreria deja el HttpClientHandler con UseProxy = true (default), que en
                // Windows depende de WinHttpAutoProxySvc para resolver el proxy del sistema en
                // cada conexion saliente. Si ese servicio falla, la resolucion de proxy cuelga
                // y las requests a graph.facebook.com nunca salen. El servidor sale directo a
                // internet sin proxy corporativo, asi que se desactiva por completo.
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    UseProxy = false,
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                });

            return services;
        }
    }
}