using Microsoft.EntityFrameworkCore;
using WhatsappSendMessages.Context;

namespace WhatsappSendMessages.Configurations.Extensions
{
    public static class PersistenceServiceExtensions
    {
        // DbContext. La cadena de conexion viene de "ConnectionStrings:WhatsAppMessages"
        // en appsettings; en testing, el WebApplicationFactory la sustituye por
        // SQLite in-memory.
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<WhatsappMessagesContext>(options =>
            {
                options.UseSqlServer(config.GetConnectionString("WhatsAppMessages"));
            });

            return services;
        }
    }
}
