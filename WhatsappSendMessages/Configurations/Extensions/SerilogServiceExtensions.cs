using Serilog;
using Serilog.Extensions.Logging;

namespace WhatsappSendMessages.Configurations.Extensions
{
    public static class SerilogServiceExtensions
    {
        public static IServiceCollection AddSerilogLogging(this IServiceCollection services, IConfiguration config)
        {
            services.AddLogging(loggingBuilder =>
            {
                // WebApplication.CreateBuilder ya registro los providers default (Console,
                // Debug, etc.), que leen de "Logging:LogLevel" y no de "Serilog:MinimumLevel".
                // Sin ClearProviders quedan corriendo en paralelo y los overrides del appsettings
                // (ej. bajar EF Core a Warning) nunca les aplican.
                loggingBuilder.ClearProviders();

                // Niveles, sinks (Console/MSSqlServer) y columnas extra se definen enteramente
                // en la seccion "Serilog" de appsettings; nada queda hardcodeado en C#.
                var log = new LoggerConfiguration()
                    .ReadFrom.Configuration(config)
                    .CreateLogger();

                // dispose: true para que el sink MSSqlServer haga flush al apagar;
                // el middleware de OOM detiene la app con lifetime.StopApplication()
                // y sin esto se perderian los ultimos logs en lote.
                loggingBuilder.AddProvider(new SerilogLoggerProvider(log, dispose: true));
            });

            return services;
        }
    }
}
