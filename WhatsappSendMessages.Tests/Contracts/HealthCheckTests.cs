using System.Net;
using WhatsappSendMessages.Tests.Infrastructure;

namespace WhatsappSendMessages.Tests.Contracts;

/// <summary>
/// Contrato de GET /health. Sin auth. Verifica que la BD responde, asi
/// detectamos desde fuera (IIS, k8s) caidas como la del 30-sep-2026.
/// </summary>
public class HealthCheckTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HealthCheckTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_SinAuth_Retorna200_CuandoBDResponde()
    {
        await _factory.EnsureSeededAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
