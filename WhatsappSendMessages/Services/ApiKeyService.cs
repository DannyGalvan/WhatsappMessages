using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using WhatsappSendMessages.Context;
using WhatsappSendMessages.Entities;

namespace WhatsappSendMessages.Services
{
    public class ApiKeyService(WhatsappMessagesContext context, IMemoryCache cache) : IApiKeyService
    {
        // Cache corto: evita golpear la BD en cada request sin retrasar una revocacion,
        // ya que RevokeAsync borra la entrada de cache del hash afectado al instante.
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

        public async Task<ApiKeyValidationResult?> ValidateAsync(string rawKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(rawKey))
                return null;

            var hash = Hash(rawKey);
            var cacheKey = CacheKeyFor(hash);

            if (cache.TryGetValue(cacheKey, out ApiKeyValidationResult? cached))
                return cached;

            var now = DateTime.UtcNow;
            var entity = await context.ApiKeys.AsNoTracking().FirstOrDefaultAsync(
                k => k.KeyHash == hash && k.IsActive && (k.ExpiresAt == null || k.ExpiresAt > now),
                cancellationToken);

            if (entity is null)
                return null;

            var result = new ApiKeyValidationResult(entity.Id, entity.Name, entity.IsAdmin);
            cache.Set(cacheKey, result, CacheTtl);
            return result;
        }

        public async Task<(ApiKey Entity, string RawKey)> CreateAsync(string name, bool isAdmin, DateTime? expiresAt,
            CancellationToken cancellationToken)
        {
            var rawKey = RandomNumberGenerator.GetHexString(64);

            var entity = new ApiKey
            {
                Name = name,
                KeyHash = Hash(rawKey),
                IsAdmin = isAdmin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };

            context.ApiKeys.Add(entity);
            await context.SaveChangesAsync(cancellationToken);

            return (entity, rawKey);
        }

        public async Task<bool> RevokeAsync(int id, CancellationToken cancellationToken)
        {
            var entity = await context.ApiKeys.FirstOrDefaultAsync(k => k.Id == id, cancellationToken);
            if (entity is null || !entity.IsActive)
                return false;

            entity.IsActive = false;
            entity.RevokedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            cache.Remove(CacheKeyFor(entity.KeyHash));
            return true;
        }

        public Task<List<ApiKey>> ListAsync(CancellationToken cancellationToken) =>
            context.ApiKeys.AsNoTracking().OrderByDescending(k => k.CreatedAt).ToListAsync(cancellationToken);

        private static string CacheKeyFor(string hash) => $"apikey:{hash}";

        private static string Hash(string rawKey) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
    }
}
