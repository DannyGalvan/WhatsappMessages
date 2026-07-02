using WhatsappSendMessages.Configurations.Extensions;

namespace WhatsappSendMessages
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddConfigGroup(builder);
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddServicesGroup(builder.Configuration);

            var app = builder.Build();

            await app.EnsureAdminApiKeyAsync();
            await app.EnsureWhatsAppAccessTokenAsync();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
