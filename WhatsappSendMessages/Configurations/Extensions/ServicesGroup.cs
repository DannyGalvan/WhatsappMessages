using System.Collections.ObjectModel;
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
using Serilog.Sinks.MSSqlServer;


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

            services.AddLogging(logginBuilder =>
            {
                var columnOptions = new ColumnOptions
                {
                    AdditionalColumns = new Collection<SqlColumn>
                {
                    new() { ColumnName = "RequestId", DataType = System.Data.SqlDbType.NVarChar, DataLength = 50 }
                }
                };

                logginBuilder.AddConfiguration(config.GetSection("Serilog"));

                var log = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.MSSqlServer(
                        connectionString: config.GetConnectionString("WhatsAppMessages"),
                        sinkOptions: new MSSqlServerSinkOptions { TableName = "Logs", AutoCreateSqlTable = true },
                        columnOptions: columnOptions,
                        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
                    .CreateLogger();

                logginBuilder.AddProvider(new SerilogLoggerProvider(log));
            });

            WhatsAppBusinessCloudApiConfig whatsAppConfig = config.GetSection("WhatsAppBusinessCloudApiConfiguration")
                .Get<WhatsAppBusinessCloudApiConfig>()!;

            services.AddWhatsAppBusinessCloudApiService(whatsAppConfig, "v22.0");

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
                    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

            return services;
        }
    }
}