using Microsoft.OpenApi.Models;
using WhatsappSendMessages.Configurations.Options;

namespace WhatsappSendMessages.Configurations.Extensions
{
    public static class SwaggerServiceExtensions
    {
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<SwaggerOptions>(config.GetSection("Swagger"));
            var swagger = config.GetSection("Swagger").Get<SwaggerOptions>() ?? new SwaggerOptions();

            if (!swagger.Enabled) return services;

            services.AddSwaggerGen(c =>
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
