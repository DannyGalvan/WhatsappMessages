using Polly;
using Polly.Extensions.Http;
using WhatsappBusiness.CloudApi.Configurations;
using WhatsappBusiness.CloudApi.Extensions;
using WhatsappBusiness.CloudApi.Interfaces;
using WhatsappSendMessages.Services;

namespace WhatsappSendMessages.Configurations.Extensions
{
    public static class WhatsAppCloudApiServiceExtensions
    {
        public static IServiceCollection AddWhatsAppCloudApi(this IServiceCollection services, IConfiguration config)
        {
            // baseConfig es el singleton que registra la libreria desde appsettings
            // (PhoneNumberId, AppName, etc). El AccessToken de ese singleton se ignora:
            // se arma un config nuevo por llamada con el token vigente en BD, para
            // poder rotarlo sin reiniciar la app.
            WhatsAppBusinessCloudApiConfig whatsAppConfig = config.GetSection("WhatsAppBusinessCloudApiConfiguration")
                .Get<WhatsAppBusinessCloudApiConfig>()!;

            services.AddWhatsAppBusinessCloudApiService(whatsAppConfig, whatsAppConfig.Version);

            services.AddScoped<IWhatsAppCloudApiConfigProvider, WhatsAppCloudApiConfigProvider>();

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
