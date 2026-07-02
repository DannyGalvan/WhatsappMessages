using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using WhatsappBusiness.CloudApi.Configurations;
using WhatsappBusiness.CloudApi.Extensions;
using WhatsappSendMessages.Context;
using Serilog;
using Serilog.Extensions.Logging;
using Serilog.Sinks.MSSqlServer;


namespace WhatsappSendMessages.Configurations.Extensions
{
    public static class ServicesGroup
    {
        public static IServiceCollection AddServicesGroup(this IServiceCollection services, IConfiguration config)
        {

            services.AddDbContext<WhatsappMessagesContext>(options =>
            {
                options.UseSqlServer(config.GetConnectionString("WhatsAppMessages"));
            });

            services.AddLogging(logginBuilder =>
            {
                var columnOptions = new ColumnOptions
                {
                    AdditionalColumns = new Collection<SqlColumn>
                {
                    new() { ColumnName = "RequestId", DataType = System.Data.SqlDbType.NVarChar, DataLength = 50 }
                }
                };

                logginBuilder.AddConfiguration(config.GetSection("Serilog"));

                var log = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.MSSqlServer(
                        connectionString: config.GetConnectionString("WhatsAppMessages"),
                        sinkOptions: new MSSqlServerSinkOptions { TableName = "Logs", AutoCreateSqlTable = true },
                        columnOptions: columnOptions,
                        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
                    .CreateLogger();

                logginBuilder.AddProvider(new SerilogLoggerProvider(log));
            });

            WhatsAppBusinessCloudApiConfig whatsAppConfig = config.GetSection("WhatsAppBusinessCloudApiConfiguration")
                .Get<WhatsAppBusinessCloudApiConfig>()!;

            services.AddWhatsAppBusinessCloudApiService(whatsAppConfig, "v22.0");

            return services;
        }
    }
}