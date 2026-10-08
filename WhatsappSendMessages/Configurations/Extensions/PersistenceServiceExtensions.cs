using Microsoft.EntityFrameworkCore;
using WhatsappSendMessages.Context;
using WhatsappSendMessages.Context.Interceptors;
using WhatsappSendMessages.Services.Auditing;

namespace WhatsappSendMessages.Configurations.Extensions
{
    public static class PersistenceServiceExtensions
    {
        // DbContext + interceptor de auditoria. La cadena de conexion viene de
        // "ConnectionStrings:WhatsAppMessages" en appsettings; en testing, el
        // WebApplicationFactory la sustituye por SQLite in-memory.
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<IClock, SystemClock>();
            services.AddScoped<ICurrentActorProvider, HttpContextCurrentActorProvider>();
            services.AddScoped<AuditSaveChangesInterceptor>();

            services.AddDbContext<WhatsappMessagesContext>((sp, options) =>
            {
                options.UseSqlServer(config.GetConnectionString("WhatsAppMessages"));
                options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
            });

            return services;
        }
    }
}
