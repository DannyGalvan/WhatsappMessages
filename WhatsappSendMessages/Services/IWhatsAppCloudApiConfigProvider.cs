using WhatsappBusiness.CloudApi.Configurations;

namespace WhatsappSendMessages.Services
{
    public interface IWhatsAppCloudApiConfigProvider
    {
        Task<WhatsAppBusinessCloudApiConfig> GetCurrentConfigAsync(CancellationToken cancellationToken);

        Task SetAccessTokenAsync(string accessToken, CancellationToken cancellationToken);
    }
}
