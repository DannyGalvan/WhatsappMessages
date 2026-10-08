using WhatsappSendMessages.Services.Templates;

namespace WhatsappSendMessages.Configurations.Extensions
{
    public static class TemplateServiceExtensions
    {
        public static IServiceCollection AddTemplateServices(this IServiceCollection services)
        {
            services.AddScoped<ITextTemplateMessageRequestFactory, TextTemplateMessageRequestFactory>();
            services.AddScoped<ISentMessageRecorder, SentMessageRecorder>();
            services.AddScoped<ITemplateMessageSender, TemplateMessageSender>();
            return services;
        }
    }
}
