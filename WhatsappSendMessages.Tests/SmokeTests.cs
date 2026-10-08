using WhatsappSendMessages.Tests.Infrastructure;

namespace WhatsappSendMessages.Tests;

/// <summary>
/// Smoke test: solo verifica que el factory arranca y Sirve una respuesta.
/// Lo demás (tests de contrato) vienen despues.
/// </summary>
public class SmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SmokeTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Factory_Starts_AndSeed_Runs()
    {
        var seed = await _factory.EnsureSeededAsync();
        Assert.NotNull(seed.AdminKey);
        Assert.NotNull(seed.NormalKey);
        Assert.NotEqual(seed.AdminKey, seed.NormalKey);
    }
}
