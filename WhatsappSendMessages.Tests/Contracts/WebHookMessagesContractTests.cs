using System.Net;
using WhatsappSendMessages.Tests.Infrastructure;

namespace WhatsappSendMessages.Tests.Contracts;

/// <summary>
/// Contrato de GET api/v1/WebHookMessages.
/// El endpoint es publico (sin auth). Devuelve hub.challenge como texto
/// si el verify_token coincide; en caso contrario 401 "forbiden" (sic,
/// typo intencionado que ya consumen clientes en produccion).
/// </summary>
public class WebHookMessagesContractTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WebHookMessagesContractTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ConTokenCorrecto_Retorna200_ConChallengeComoTexto()
    {
        // Asegura el seed para que el host este armado.
        await _factory.EnsureSeededAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/WebHookMessages?hub.mode=subscribe&hub.challenge=1234567890&hub.verify_token=12345");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("1234567890", body);
    }

    [Fact]
    public async Task ConTokenIncorrecto_Retorna401_ConCuerpoForbiden()
    {
        await _factory.EnsureSeededAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/WebHookMessages?hub.mode=subscribe&hub.challenge=1234567890&hub.verify_token=WRONG");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("forbiden", body);
    }

    [Fact]
    public async Task ConModoIncorrecto_Retorna401_ConCuerpoForbiden()
    {
        await _factory.EnsureSeededAsync();
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/WebHookMessages?hub.mode=other&hub.challenge=1234567890&hub.verify_token=12345");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("forbiden", body);
    }
}
