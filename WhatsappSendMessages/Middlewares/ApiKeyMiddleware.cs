using System.Text.Json;
using Microsoft.Extensions.Options;
using WhatsappSendMessages.Configurations.Models;
using WhatsappSendMessages.Entities.Response;

namespace WhatsappSendMessages.Middlewares
{
    public class ApiKeyMiddleware(RequestDelegate next, IOptions<ApiKeyConfig> keyConfig)
    {
        private const string ApiKeyHeaderName = "X-API-KEY";
        private readonly ApiKeyConfig _apiKeyConfig = keyConfig.Value;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        public async Task InvokeAsync(HttpContext context)
        {
            Response<string> response = new Response<string>();

            var path = context.Request.Path.Value;

            // Permitir el acceso a rutas excluidas
            if (_apiKeyConfig.ExcludedPaths.Any(p => path!.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                await next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
            {
                response.Success = false;
                response.Data = null;
                response.Message = "API KEY no enviado, revise sus encabezados porfavor";

                context.Response.StatusCode = 401;
                await context.Response.WriteAsync(JsonSerializer.Serialize(response, options : _jsonOptions));
                return;
            }

            if (!_apiKeyConfig.ApiKey.Equals(extractedApiKey))
            {
                response.Success = false;
                response.Data = null;
                response.Message = "API KEY Invalida, revise porfavor";

                context.Response.StatusCode = 403;
                await context.Response.WriteAsync(JsonSerializer.Serialize(response, options: _jsonOptions));
                return;
            }

            await next(context);
        }
    }

}
