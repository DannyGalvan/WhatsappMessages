using Microsoft.EntityFrameworkCore;
using WhatsappSendMessages.Context;
using WhatsappSendMessages.Services;

namespace WhatsappSendMessages.Configurations.Extensions
{
    public static class ApplicationGroup
    {
        // Si no hay ninguna API key admin activa (primer arranque, o la unica que
        // habia se revoco), genera una y la loguea una sola vez para poder gestionar
        // el resto de keys via api/v1/ApiKeys sin tocar appsettings ni redeploy.
        public static async Task EnsureAdminApiKeyAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<WhatsappMessagesContext>();
            if (await db.ApiKeys.AnyAsync(k => k.IsAdmin && k.IsActive))
                return;

            var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
            var (entity, rawKey) = await apiKeyService.CreateAsync(
                "bootstrap-admin", isAdmin: true, expiresAt: null, CancellationToken.None);

            app.Logger.LogWarning(
                "No habia ninguna API key admin activa. Se genero una nueva (id {Id}): {RawKey}. " +
                "Guardela ahora, no se volvera a mostrar.", entity.Id, rawKey);
        }
    }
}
