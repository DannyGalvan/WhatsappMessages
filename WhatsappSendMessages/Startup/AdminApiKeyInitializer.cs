using Microsoft.EntityFrameworkCore;
using WhatsappSendMessages.Context;
using WhatsappSendMessages.Services;

namespace WhatsappSendMessages.Startup
{
    // Si no hay ninguna API key admin activa (primer arranque, o la unica que
    // habia se revoco), genera una y la loguea una sola vez para poder gestionar
    // el resto de keys via api/v1/ApiKeys sin tocar appsettings ni redeploy.
    public class AdminApiKeyInitializer(
        IServiceProvider services,
        IApiKeyService apiKeys,
        IHostEnvironment env,
        ILogger<AdminApiKeyInitializer> logger) : IStartupInitializer
    {
        public int Order => 100;

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            // En Testing, el factory de tests siembra las API keys manualmente
            // despues de EnsureCreated; este initializer se interpone entre
            // Build() y el primer request y reventaria contra una BD vacia.
            if (env.IsEnvironment("Testing")) return;

            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WhatsappMessagesContext>();
            if (await db.ApiKeys.AnyAsync(k => k.IsAdmin && k.IsActive, cancellationToken))
                return;

            var (entity, rawKey) = await apiKeys.CreateAsync(
                "bootstrap-admin", isAdmin: true, expiresAt: null, cancellationToken);

            logger.LogWarning(
                "No habia ninguna API key admin activa. Se genero una nueva (id {Id}): {RawKey}. " +
                "Guardela ahora, no se volvera a mostrar.", entity.Id, rawKey);
        }
    }
}
