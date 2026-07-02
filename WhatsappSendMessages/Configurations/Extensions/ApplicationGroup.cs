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

        // Migra una sola vez el AccessToken de WhatsApp desde appsettings a BD, para no
        // romper el envio de mensajes en el primer deploy de este cambio. De ahi en
        // adelante se rota via PUT api/v1/WhatsAppAccessToken, sin volver a tocar config.
        public static async Task EnsureWhatsAppAccessTokenAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<WhatsappMessagesContext>();
            if (await db.WhatsAppAccessTokens.AnyAsync())
                return;

            var fallbackToken = app.Configuration["WhatsAppBusinessCloudApiConfiguration:AccessToken"];
            if (string.IsNullOrWhiteSpace(fallbackToken))
            {
                app.Logger.LogWarning(
                    "No hay AccessToken de WhatsApp en base de datos ni en appsettings. " +
                    "Configurelo via PUT api/v1/WhatsAppAccessToken antes de enviar mensajes.");
                return;
            }

            var configProvider = scope.ServiceProvider.GetRequiredService<IWhatsAppCloudApiConfigProvider>();
            await configProvider.SetAccessTokenAsync(fallbackToken, CancellationToken.None);

            app.Logger.LogWarning(
                "Se migro el AccessToken de WhatsApp desde appsettings a base de datos. " +
                "Ya puede eliminar la clave AccessToken de appsettings.Production.json.");
        }
    }
}
