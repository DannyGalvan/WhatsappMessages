using System;
using Microsoft.OpenApi.Models;
using WhatsappSendMessages.Configurations.Models;

namespace WhatsappSendMessages.Configurations.Extensions
{
    public static class ConfigGroup
    {
        public static IServiceCollection AddConfigGroup(this IServiceCollection services, WebApplicationBuilder builder)
        {
            string environment = builder.Environment.EnvironmentName;

            // Configure the ConfigurationBuilder y load the configurations del archive appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) // Load base file appsettings.json
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true) // Load environment-specific file
                .AddEnvironmentVariables()
                .Build();

            IConfiguration apiKeyConfig = configuration.GetSection("ApiKeyConfig");

            services.Configure<ApiKeyConfig>(apiKeyConfig);

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Whatsapp Messages Misol", Version = "v1" });

                // Configuración para API Key
                c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Description = "Clave de acceso via header. Ejemplo: X-API-KEY: xxxxxxxx",
                    Type = SecuritySchemeType.ApiKey,
                    Name = "X-API-KEY",
                    In = ParameterLocation.Header,
                    Scheme = "ApiKeyScheme"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKey"
                            },
                            In = ParameterLocation.Header
                        },
                        new List<string>()
                    }
                });
            });

            return services;
        }
    }
}
