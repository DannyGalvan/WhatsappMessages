using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using WhatsappSendMessages.Tests.Infrastructure;

namespace WhatsappSendMessages.Tests.Contracts;

/// <summary>
/// Contrato de los endpoints de /api/v1/ApiKeys.
/// GET/POST requieren API key admin. DELETE acepta cualquier API key valida.
/// </summary>
public class ApiKeysContractTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ApiKeysContractTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_SinApiKey_Retorna401_ConCuerpoExacto()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/ApiKeys");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        var expected = JsonNode.Parse("""
            {
              "success": false,
              "message": "API KEY no enviado o invalida, revise sus encabezados porfavor",
              "data": null
            }
            """)!;
        var actual = JsonNode.Parse(raw)!;
        Assert.True(JsonNode.DeepEquals(expected, actual),
            $"Body no coincide.\nEsperado: {expected.ToJsonString()}\nActual:   {actual.ToJsonString()}");
    }

    [Fact]
    public async Task Get_ConApiKeyNormal_Retorna403_ConCuerpoExacto()
    {
        var seed = await _factory.EnsureSeededAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", seed.NormalKey);

        var response = await client.GetAsync("/api/v1/ApiKeys");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        var expected = JsonNode.Parse("""
            {
              "success": false,
              "message": "No tiene permisos suficientes para acceder a este recurso",
              "data": null
            }
            """)!;
        var actual = JsonNode.Parse(raw)!;
        Assert.True(JsonNode.DeepEquals(expected, actual),
            $"Body no coincide.\nEsperado: {expected.ToJsonString()}\nActual:   {actual.ToJsonString()}");
    }

    [Fact]
    public async Task Get_ConApiKeyAdmin_Retorna200_ConListaEnvueltaEnResponse()
    {
        var seed = await _factory.EnsureSeededAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", seed.AdminKey);

        var response = await client.GetAsync("/api/v1/ApiKeys");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        var node = JsonNode.Parse(raw)!;

        Assert.True((bool?)node["success"] ?? false);
        Assert.IsType<JsonArray>(node["data"]);
        // El listado debe contener al menos las 2 keys de seed + la bootstrap
        Assert.NotEmpty((JsonArray)node["data"]!);
    }

    [Fact]
    public async Task Post_ConApiKeyAdmin_Retorna200_ConRawKey()
    {
        var seed = await _factory.EnsureSeededAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", seed.AdminKey);

        var response = await client.PostAsJsonAsync("/api/v1/ApiKeys", new
        {
            name = "test-created",
            isAdmin = false,
            expiresAt = (DateTime?)null
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        var node = JsonNode.Parse(raw)!;

        Assert.True((bool?)node["success"] ?? false);
        Assert.Equal("Guarde esta key ahora, no se volvera a mostrar", (string?)node["message"]);
        Assert.NotNull((string?)node["data"]?["rawKey"]);
        Assert.NotEmpty((string)node["data"]!["rawKey"]!);
    }

    [Fact]
    public async Task Delete_DeKeyInexistente_Retorna404_ConMensajeExacto()
    {
        var seed = await _factory.EnsureSeededAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", seed.AdminKey);

        var response = await client.DeleteAsync("/api/v1/ApiKeys/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        var expected = JsonNode.Parse("""
            {
              "success": false,
              "message": "API key no encontrada o ya revocada",
              "data": null
            }
            """)!;
        var actual = JsonNode.Parse(raw)!;
        Assert.True(JsonNode.DeepEquals(expected, actual),
            $"Body no coincide.\nEsperado: {expected.ToJsonString()}\nActual:   {actual.ToJsonString()}");
    }
}
