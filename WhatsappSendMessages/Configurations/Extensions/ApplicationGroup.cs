using WhatsappSendMessages.Startup;

namespace WhatsappSendMessages.Configurations.Extensions
{
    public static class ApplicationGroup
    {
        // Crea un scope, resuelve todos los IStartupInitializer y los ejecuta
        // ordenados por Order. Agregar un initializer nuevo = nueva clase +
        // registro en StartupServiceExtensions, sin tocar este runner (OCP).
        public static async Task RunStartupInitializersAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var initializers = scope.ServiceProvider
                .GetRequiredService<IEnumerable<IStartupInitializer>>()
                .OrderBy(i => i.Order);

            foreach (var initializer in initializers)
            {
                await initializer.InitializeAsync(app.Lifetime.ApplicationStopping);
            }
        }
    }
}
