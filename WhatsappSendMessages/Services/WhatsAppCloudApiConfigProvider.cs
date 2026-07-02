using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WhatsappBusiness.CloudApi.Configurations;
using WhatsappSendMessages.Context;
using WhatsappSendMessages.Entities;

namespace WhatsappSendMessages.Services
{
    // baseConfig es el singleton que registra la libreria desde appsettings (PhoneNumberId,
    // AppName, etc). El AccessToken de ese singleton se ignora: se arma un config nuevo por
    // llamada con el token vigente en BD, para poder rotarlo sin reiniciar la app.
    public class WhatsAppCloudApiConfigProvider(
        WhatsAppBusinessCloudApiConfig baseConfig,
        WhatsappMessagesContext context,
        IMemoryCache cache) : IWhatsAppCloudApiConfigProvider
    {
        private const string CacheKey = "whatsapp:access-token";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

        public async Task<WhatsAppBusinessCloudApiConfig> GetCurrentConfigAsync(CancellationToken cancellationToken)
        {
            if (!cache.TryGetValue(CacheKey, out string? token) || token is null)
            {
                var entity = await context.WhatsAppAccessTokens.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
                if (entity is null)
                    throw new InvalidOperationException(
                        "No hay un AccessToken de WhatsApp configurado. Configurelo via PUT api/v1/WhatsAppAccessToken.");

                token = entity.Token;
                cache.Set(CacheKey, token, CacheTtl);
            }

            return new WhatsAppBusinessCloudApiConfig
            {
                WhatsAppBusinessPhoneNumberId = baseConfig.WhatsAppBusinessPhoneNumberId,
                WhatsAppBusinessAccountId = baseConfig.WhatsAppBusinessAccountId,
                WhatsAppBusinessId = baseConfig.WhatsAppBusinessId,
                AccessToken = token,
                AppName = baseConfig.AppName,
                Version = baseConfig.Version
            };
        }

        public async Task SetAccessTokenAsync(string accessToken, CancellationToken cancellationToken)
        {
            var entity = await context.WhatsAppAccessTokens.FirstOrDefaultAsync(cancellationToken);
            if (entity is null)
            {
                entity = new WhatsAppAccessToken { Token = accessToken, UpdatedAt = DateTime.UtcNow };
                context.WhatsAppAccessTokens.Add(entity);
            }
            else
            {
                entity.Token = accessToken;
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync(cancellationToken);
            cache.Remove(CacheKey);
        }
    }
}
