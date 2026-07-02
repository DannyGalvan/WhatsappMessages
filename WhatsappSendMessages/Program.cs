
using Microsoft.EntityFrameworkCore;
using WhatsappSendMessages.Configurations.Extensions;
using WhatsappSendMessages.Context;
using WhatsappSendMessages.Services;

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

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<WhatsappMessagesContext>();
                if (!await db.ApiKeys.AnyAsync(k => k.IsAdmin && k.IsActive))
                {
                    var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
                    var (entity, rawKey) = await apiKeyService.CreateAsync(
                        "bootstrap-admin", isAdmin: true, expiresAt: null, CancellationToken.None);

                    app.Logger.LogWarning(
                        "No habia ninguna API key admin activa. Se genero una nueva (id {Id}): {RawKey}. " +
                        "Guardela ahora, no se volvera a mostrar.", entity.Id, rawKey);
                }
            }

            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseSwagger();
            //    app.UseSwaggerUI();
            //}

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
