using WhatsappSendMessages.Configurations.Extensions;
using WhatsappSendMessages.Configurations.Options;
using WhatsappSendMessages.Context;
using WhatsappSendMessages.Middleware;
using Microsoft.Extensions.Options;

namespace WhatsappSendMessages
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSwaggerDocumentation(builder.Configuration);
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddServicesGroup(builder.Configuration);
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<WhatsappMessagesContext>();

            var app = builder.Build();

            await app.RunStartupInitializersAsync();

            app.UseMiddleware<OutOfMemoryRecoveryMiddleware>();

            if (app.Services.GetRequiredService<IOptions<SwaggerOptions>>().Value.Enabled)
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            // /health: sin auth, util para que el monitor (IIS, Kubernetes,
            // balanceador) detecte caidas como la del 30-sep-2026 desde fuera.
            app.MapHealthChecks("/health");

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
