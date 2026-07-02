using WhatsappSendMessages.Entities;

namespace WhatsappSendMessages.Services
{
    public record ApiKeyValidationResult(int Id, string Name, bool IsAdmin);

    public interface IApiKeyService
    {
        Task<ApiKeyValidationResult?> ValidateAsync(string rawKey, CancellationToken cancellationToken);

        Task<(ApiKey Entity, string RawKey)> CreateAsync(string name, bool isAdmin, DateTime? expiresAt,
            CancellationToken cancellationToken);

        Task<bool> RevokeAsync(int id, CancellationToken cancellationToken);

        Task<List<ApiKey>> ListAsync(CancellationToken cancellationToken);
    }
}
