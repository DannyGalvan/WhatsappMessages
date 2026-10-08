using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WhatsappSendMessages;

namespace WhatsappSendMessages.Tests.Contracts;

/// <summary>
/// Swagger debe poder desactivarse por configuracion. Cuando Enabled=false,
/// /swagger/v1/swagger.json debe devolver 404 (porque ni siquiera se
/// registra el middleware).
/// </summary>
public class SwaggerToggleTests
{
    [Fact]
    public async Task Swagger_Deshabilitado_Retorna404()
    {
        await using var factory = new SwaggerOffFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_HabilitadoPorDefault_Retorna200()
    {
        await using var factory = new SwaggerOnFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class SwaggerOffFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Swagger:Enabled"] = "false"
                });
            });
        }
    }

    private sealed class SwaggerOnFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
        }
    }
}
