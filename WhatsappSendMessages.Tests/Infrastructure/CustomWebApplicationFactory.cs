using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WhatsappBusiness.CloudApi.Interfaces;
using WhatsappSendMessages.Context;
using WhatsappSendMessages.Services;

namespace WhatsappSendMessages.Tests.Infrastructure;

/// <summary>
/// WebApplicationFactory que sustituye:
///   - DbContext: SQLite in-memory (conexion abierta compartida, EnsureCreated).
///   - IWhatsAppBusinessClient: fake controlable por test.
/// Mantiene el resto del pipeline (auth, MVC, controllers, middlewares) intacto,
/// para que los tests de contrato detecten cualquier cambio no intencional.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    /// <summary>Fake tipado: permite configurar NextResponse/NextException/CallCount.</summary>
    public FakeWhatsAppBusinessClient Fake { get; } = (FakeWhatsAppBusinessClient)FakeWhatsAppBusinessClient.Create();

    /// <summary>Mismo fake visto como IWhatsAppBusinessClient para DI.</summary>
    public IWhatsAppBusinessClient FakeClient => (IWhatsAppBusinessClient)Fake;

    static CustomWebApplicationFactory()
    {
        // Estos valores los lee WebApplication.CreateBuilder ANTES de que el
        // factory pueda interceptar ConfigureAppConfiguration. Sin ellos, el
        // bloque que arma WhatsAppBusinessCloudApiConfig en ServicesGroup
        // recibe null y revienta con NullReferenceException.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("WhatsAppBusinessCloudApiConfiguration__WhatsAppBusinessPhoneNumberId", "000000000000000");
        Environment.SetEnvironmentVariable("WhatsAppBusinessCloudApiConfiguration__WhatsAppBusinessAccountId", "000000000000000");
        Environment.SetEnvironmentVariable("WhatsAppBusinessCloudApiConfiguration__WhatsAppBusinessId", "000000000000000");
        Environment.SetEnvironmentVariable("WhatsAppBusinessCloudApiConfiguration__AccessToken", "test-token");
        Environment.SetEnvironmentVariable("WhatsAppBusinessCloudApiConfiguration__AppName", "TestApp");
        Environment.SetEnvironmentVariable("WhatsAppBusinessCloudApiConfiguration__Version", "v22.0");

        // Evita que Serilog intente abrir el sink MSSqlServer en la BD real
        // de produccion (causa timeout durante EnsureCreated).
        Environment.SetEnvironmentVariable("Serilog__Using__0", "Serilog.Sinks.Console");
        Environment.SetEnvironmentVariable("Serilog__MinimumLevel__Default", "Warning");
        Environment.SetEnvironmentVariable("Serilog__WriteTo__0__Name", "Console");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseEnvironment("Testing");

        // Override de configuracion: sin Serilog (no se necesita sink en tests),
        // dummy de WhatsAppBusinessCloudApiConfiguration (la lib lo requiere
        // aunque la respuesta venga del fake).
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["Serilog:MinimumLevel:Default"] = "Warning",
                ["Serilog:Using:0"] = "Serilog.Sinks.Console",
                ["Serilog:WriteTo:0:Name"] = "Console",
                ["WhatsAppBusinessCloudApiConfiguration:WhatsAppBusinessPhoneNumberId"] = "000000000000000",
                ["WhatsAppBusinessCloudApiConfiguration:WhatsAppBusinessAccountId"] = "000000000000000",
                ["WhatsAppBusinessCloudApiConfiguration:WhatsAppBusinessId"] = "000000000000000",
                ["WhatsAppBusinessCloudApiConfiguration:AccessToken"] = "test-token",
                ["WhatsAppBusinessCloudApiConfiguration:AppName"] = "TestApp",
                ["WhatsAppBusinessCloudApiConfiguration:Version"] = "v22.0"
            };
            config.AddInMemoryCollection(overrides);
        });

        builder.ConfigureTestServices(services =>
        {
            // Quita el HttpClient tipado que registra la libreria y el cliente real
            services.RemoveAll<IWhatsAppBusinessClient>();
            services.AddSingleton(FakeClient);

            // Reemplaza el DbContext por uno SQLite in-memory. Hay que limpiar
            // TODAS las piezas que registro AddDbContext<WhatsappMessagesContext>
            // en ServicesGroup (el context, sus options, la configuracion del
            // provider). Si queda SqlServer en el service provider, EF lanza
            // "Only a single database provider can be registered".
            services.RemoveAll<WhatsappMessagesContext>();
            services.RemoveAll<DbContextOptions<WhatsappMessagesContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<WhatsappMessagesContext>>();
            services.AddDbContext<WhatsappMessagesContext>(opt =>
            {
                opt.UseSqlite(_connection);
                // Suprime el warning de "PendingModelChangesWarning" al usar EnsureCreated.
                opt.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            });
        });
    }

    /// <summary>
    /// Garantiza que la BD existe y siembra API keys + WhatsAppAccessToken.
    /// Idempotente: corre una sola vez por instancia de factory.
    /// </summary>
    private readonly SemaphoreSlim _seedGate = new(1, 1);
    private SeedData? _seed;

    public async Task<SeedData> EnsureSeededAsync()
    {
        // Reset del fake: las suites comparten la misma instancia via
        // IClassFixture; sin esto, un test que setea NextException contamina
        // al siguiente.
        Fake.Reset();

        if (_seed is not null) return _seed;
        await _seedGate.WaitAsync();
        try
        {
            if (_seed is not null) return _seed;

            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<WhatsappMessagesContext>();
            await db.Database.EnsureCreatedAsync();

            var apiKeys = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
            var configProvider = scope.ServiceProvider.GetRequiredService<IWhatsAppCloudApiConfigProvider>();

            // Admin: el initializer EnsureAdminApiKeyAsync ya corrio durante
            // builder.Build() y creo uno con name "bootstrap-admin". Si por algun
            // motivo no existe, lo creamos nosotros.
            var (admin, adminKey) = await SeedKeyAsync(apiKeys, "test-admin", isAdmin: true);
            var (normal, normalKey) = await SeedKeyAsync(apiKeys, "test-normal", isAdmin: false);

            // WhatsAppAccessToken: tambien es requerido para que GetCurrentConfigAsync funcione.
            await configProvider.SetAccessTokenAsync("test-access-token", CancellationToken.None);

            _seed = new SeedData(admin.Id, adminKey, normal.Id, normalKey);
            return _seed;
        }
        finally
        {
            _seedGate.Release();
        }
    }

    private static async Task<(WhatsappSendMessages.Entities.ApiKey Entity, string RawKey)> SeedKeyAsync(
        IApiKeyService apiKeys, string name, bool isAdmin)
    {
        var existing = apiKeys.GetType();
        // No hay "GetByName"; el initializer ya pudo haber creado "bootstrap-admin".
        // Para que el test sea determinista usamos un nombre unico por ejecucion.
        var (entity, raw) = await apiKeys.CreateAsync($"{name}-{Guid.NewGuid():N}", isAdmin, null, CancellationToken.None);
        return (entity, raw);
    }
}

public sealed record SeedData(int AdminId, string AdminKey, int NormalId, string NormalKey);
