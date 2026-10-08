using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using WhatsappSendMessages.Tests.Infrastructure;

namespace WhatsappSendMessages.Tests.Contracts;

/// <summary>
/// Contrato de PUT api/v1/WhatsAppAccessToken. Requiere API key admin.
/// </summary>
public class WhatsAppAccessTokenContractTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WhatsAppAccessTokenContractTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Put_ConTokenVacio_Retorna400_ConMensajeExacto()
    {
        var seed = await _factory.EnsureSeededAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", seed.AdminKey);

        var response = await client.PutAsJsonAsync("/api/v1/WhatsAppAccessToken", new
        {
            accessToken = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        var expected = JsonNode.Parse("""
            {
              "success": false,
              "message": "El AccessToken es requerido",
              "data": null
            }
            """)!;
        var actual = JsonNode.Parse(raw)!;
        Assert.True(JsonNode.DeepEquals(expected, actual),
            $"Body no coincide.\nEsperado: {expected.ToJsonString()}\nActual:   {actual.ToJsonString()}");
    }

    [Fact]
    public async Task Put_ConTokenValido_Retorna200_ConMensajeExito()
    {
        var seed = await _factory.EnsureSeededAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", seed.AdminKey);

        var response = await client.PutAsJsonAsync("/api/v1/WhatsAppAccessToken", new
        {
            accessToken = "new-rotated-token"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        var expected = JsonNode.Parse("""
            {
              "success": true,
              "message": "AccessToken de WhatsApp actualizado",
              "data": null
            }
            """)!;
        var actual = JsonNode.Parse(raw)!;
        Assert.True(JsonNode.DeepEquals(expected, actual),
            $"Body no coincide.\nEsperado: {expected.ToJsonString()}\nActual:   {actual.ToJsonString()}");
    }

    [Fact]
    public async Task Put_ConApiKeyNormal_Retorna403()
    {
        var seed = await _factory.EnsureSeededAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", seed.NormalKey);

        var response = await client.PutAsJsonAsync("/api/v1/WhatsAppAccessToken", new
        {
            accessToken = "should-be-ignored"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
