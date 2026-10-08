namespace WhatsappSendMessages.Configurations.Extensions
{
    // Fachada: conserva la firma usada por Program.cs mientras la composicion
    // se reparte por responsabilidad (persistencia, auth, logging, cliente WhatsApp).
    // Cada Add* vive en su propio archivo y registra una sola concern.
    public static class ServicesGroup
    {
        public static IServiceCollection AddServicesGroup(this IServiceCollection services, IConfiguration config)
        {
            services.AddPersistence(config);
            services.AddApiKeyAuthentication();
            services.AddSerilogLogging(config);
            services.AddWhatsAppCloudApi(config);
            services.AddTemplateServices();
            services.AddStartupInitializers();
            services.AddConfigOptions(config);

            return services;
        }
    }
}
