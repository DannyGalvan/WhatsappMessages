using WhatsappSendMessages.Startup;

namespace WhatsappSendMessages.Configurations.Extensions
{
    public static class StartupServiceExtensions
    {
        public static IServiceCollection AddStartupInitializers(this IServiceCollection services)
        {
            services.AddScoped<IStartupInitializer, AdminApiKeyInitializer>();
            services.AddScoped<IStartupInitializer, WhatsAppAccessTokenInitializer>();
            return services;
        }
    }
}
