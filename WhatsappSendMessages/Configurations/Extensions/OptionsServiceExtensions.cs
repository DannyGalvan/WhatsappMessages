using WhatsappSendMessages.Configurations.Options;

namespace WhatsappSendMessages.Configurations.Extensions
{
    public static class OptionsServiceExtensions
    {
        public static IServiceCollection AddConfigOptions(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<WhatsAppWebhookOptions>(config.GetSection("WhatsAppWebhook"));
            return services;
        }
    }
}
