using WhatsappSendMessages.Services;

namespace WhatsappSendMessages.Startup
{
    // Migra una sola vez el AccessToken de WhatsApp desde appsettings a BD, para no
    // romper el envio de mensajes en el primer deploy de este cambio. De ahi en
    // adelante se rota via PUT api/v1/WhatsAppAccessToken, sin volver a tocar config.
    public class WhatsAppAccessTokenInitializer(
        IWhatsAppCloudApiConfigProvider configProvider,
        IHostEnvironment env,
        IConfiguration config,
        ILogger<WhatsAppAccessTokenInitializer> logger) : IStartupInitializer
    {
        public int Order => 200;

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            // Mismo motivo que AdminApiKeyInitializer: el factory de tests
            // siembra el token explicitamente.
            if (env.IsEnvironment("Testing")) return;

            // Check directo via provider (evita doble lectura de la BD). El
            // provider cachea 30s, pero en arranque siempre arranca vacio.
            // El initializer y el provider se registran en el mismo scope,
            // asi que vemos la misma instancia del cache.
            var existing = await configProvider.GetCurrentConfigAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(existing.AccessToken) &&
                !IsFromFallbackConfig(config, existing.AccessToken))
            {
                // Ya hay un AccessToken en BD distinto al de appsettings: nada que migrar.
                return;
            }

            var fallbackToken = config["WhatsAppBusinessCloudApiConfiguration:AccessToken"];
            if (string.IsNullOrWhiteSpace(fallbackToken))
            {
                logger.LogWarning(
                    "No hay AccessToken de WhatsApp en base de datos ni en appsettings. " +
                    "Configurelo via PUT api/v1/WhatsAppAccessToken antes de enviar mensajes.");
                return;
            }

            await configProvider.SetAccessTokenAsync(fallbackToken, cancellationToken);

            logger.LogWarning(
                "Se migro el AccessToken de WhatsApp desde appsettings a base de datos. " +
                "Ya puede eliminar la clave AccessToken de appsettings.Production.json.");
        }

        private static bool IsFromFallbackConfig(IConfiguration config, string token) =>
            string.Equals(config["WhatsAppBusinessCloudApiConfiguration:AccessToken"], token,
                StringComparison.Ordinal);
    }
}
