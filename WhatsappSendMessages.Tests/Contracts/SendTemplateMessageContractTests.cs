using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using WhatsappBusiness.CloudApi.Response;
using WhatsappSendMessages.Tests.Infrastructure;

namespace WhatsappSendMessages.Tests.Contracts;

/// <summary>
/// Contrato de POST api/v1/SendTemplateMessage.
/// El endpoint retorna el WhatsAppResponse crudo (no envuelto en Response&lt;T&gt;).
/// </summary>
public class SendTemplateMessageContractTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SendTemplateMessageContractTests(CustomWebApplicationFactory factory) => _factory = factory;

    private async Task<HttpClient> CreateClientAsync()
    {
        var seed = await _factory.EnsureSeededAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", seed.NormalKey);
        return client;
    }

    [Fact]
    public async Task SinApiKey_Retorna401_ConCuerpoExacto()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/SendTemplateMessage", new
        {
            number = "5215512345678",
            templateName = "ignored"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var json = await response.Content.ReadAsStringAsync();
        var expected = JsonNode.Parse("""
            {
              "success": false,
              "message": "API KEY no enviado o invalida, revise sus encabezados porfavor",
              "data": null
            }
            """)!;
        var actual = JsonNode.Parse(json)!;

        Assert.True(JsonNode.DeepEquals(expected, actual),
            $"Body no coincide.\nEsperado: {expected.ToJsonString()}\nActual:   {actual.ToJsonString()}");
    }

    [Fact]
    public async Task ConApiKeyValida_YClienteExitoso_Retorna200_ConWhatsAppResponseCrudo()
    {
        var seed = await _factory.EnsureSeededAsync();
        _factory.Fake.NextResponse = new WhatsAppResponse
        {
            MessagingProduct = "whatsapp",
            Contacts = new List<Contact>
            {
                new() { Input = "5215512345678", WaId = "5215512345678" }
            },
            Messages = new List<Message>
            {
                new() { Id = "wamid.HBgLMTIxNDU1MDAwMDM5FQIAERgSREE1NDA3QzA5NEY0NkY5Rjc2QjA2AA==" }
            }
        };

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", seed.NormalKey);

        var response = await client.PostAsJsonAsync("/api/v1/SendTemplateMessage", new
        {
            number = "5215512345678",
            templateName = "compra_aprobada"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var raw = await response.Content.ReadAsStringAsync();
        // El serializador usa snake_case: messaging_product, no messagingProduct.
        var expected = JsonNode.Parse("""
            {
              "messaging_product": "whatsapp",
              "contacts": [
                { "input": "5215512345678", "wa_id": "5215512345678" }
              ],
              "messages": [
                { "id": "wamid.HBgLMTIxNDU1MDAwMDM5FQIAERgSREE1NDA3QzA5NEY0NkY5Rjc2QjA2AA==" }
              ]
            }
            """)!;
        var actual = JsonNode.Parse(raw)!;

        Assert.True(JsonNode.DeepEquals(expected, actual),
            $"Body no coincide.\nEsperado: {expected.ToJsonString()}\nActual:   {actual.ToJsonString()}");
    }

    [Fact]
    public async Task ConApiKeyValida_ClienteLanzaExcepcion_Retorna400_ConCuerpoExacto()
    {
        var seed = await _factory.EnsureSeededAsync();
        _factory.Fake.NextException = new InvalidOperationException("upstream boom");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", seed.NormalKey);

        var response = await client.PostAsJsonAsync("/api/v1/SendTemplateMessage", new
        {
            number = "5215512345678",
            templateName = "compra_aprobada"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var raw = await response.Content.ReadAsStringAsync();
        var expected = JsonNode.Parse("""
            {
              "success": false,
              "message": "Error al enviar la plantilla",
              "data": "upstream boom"
            }
            """)!;
        var actual = JsonNode.Parse(raw)!;

        Assert.True(JsonNode.DeepEquals(expected, actual),
            $"Body no coincide.\nEsperado: {expected.ToJsonString()}\nActual:   {actual.ToJsonString()}");
    }

    [Fact]
    public async Task ConApiKeyValida_YParametros_SeEnvianEnElCuerpo()
    {
        // Verifica que el mapeo a TextTemplateMessageRequest funciona con
        // parameters. El fake registra la llamada; comprobamos que se invoco.
        var seed = await _factory.EnsureSeededAsync();
        _factory.Fake.NextResponse = new WhatsAppResponse
        {
            MessagingProduct = "whatsapp",
            Contacts = new List<Contact> { new() { Input = "5215512345678", WaId = "5215512345678" } },
            Messages = new List<Message> { new() { Id = "wamid.x" } }
        };

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-KEY", seed.NormalKey);

        var response = await client.PostAsJsonAsync("/api/v1/SendTemplateMessage", new
        {
            number = "5215512345678",
            templateName = "saludo",
            parameters = new[]
            {
                new { type = "text", parameterName = "nombre", text = "Juan" },
                new { type = "text", parameterName = "saldo", text = "1500" }
            }
        });

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException(
                $"Status: {response.StatusCode}. Body: {body}");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(_factory.Fake.SendTextMessageTemplateCallCount > 0);
    }
}
