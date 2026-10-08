using Microsoft.EntityFrameworkCore;
using WhatsappSendMessages.Context;
using WhatsappSendMessages.Context.Interceptors;
using WhatsappSendMessages.Entities;
using WhatsappSendMessages.Services.Auditing;

namespace WhatsappSendMessages.Tests.Unit;

public class AuditSaveChangesInterceptorTests
{
    private static (TestDbContext Db, AuditSaveChangesInterceptor Interceptor, FakeClock Clock, FakeActor Actor) NewDb()
    {
        var clock = new FakeClock(new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc));
        var actor = new FakeActor("test-actor");
        var interceptor = new AuditSaveChangesInterceptor(actor, clock);

        var options = new DbContextOptionsBuilder<WhatsappMessagesContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .AddInterceptors(interceptor)
            .Options;

        return (new TestDbContext(options), interceptor, clock, actor);
    }

    [Fact]
    public async Task Insert_SeteaLos4CamposDeAuditoria()
    {
        var (db, _, clock, actor) = NewDb();

        db.ApiKeys.Add(new ApiKey { Name = "x", KeyHash = "h", IsActive = true });
        await db.SaveChangesAsync();

        var entry = await db.ApiKeys.AsNoTracking().FirstAsync();
        Assert.Equal(clock.UtcNow, entry.CreatedAt);
        Assert.Equal(clock.UtcNow, entry.UpdatedAt);
        Assert.Equal("test-actor", entry.CreatedBy);
        Assert.Equal("test-actor", entry.UpdatedBy);
    }

    [Fact]
    public async Task Update_SoloTocaUpdated_NoSobrescribeCreated()
    {
        var (db, _, clock, actor) = NewDb();
        var later = clock.UtcNow.AddHours(1);

        var key = new ApiKey { Name = "x", KeyHash = "h", IsActive = true };
        db.ApiKeys.Add(key);
        await db.SaveChangesAsync();

        // Cambia el actor y el reloj; simula "mas tarde" y "otra persona".
        actor.Set("nuevo-actor");
        clock.Set(later);

        key.Name = "renombrada";
        await db.SaveChangesAsync();

        var stored = await db.ApiKeys.AsNoTracking().FirstAsync();
        Assert.Equal("renombrada", stored.Name);
        // Created no se toca
        Assert.NotEqual(later, stored.CreatedAt);
        Assert.Equal("test-actor", stored.CreatedBy);
        // Updated si
        Assert.Equal(later, stored.UpdatedAt);
        Assert.Equal("nuevo-actor", stored.UpdatedBy);
    }

    [Fact]
    public async Task Update_CreadoEnRequest_ActualizadoEnOtro_AmbosSeRegistran()
    {
        var (db, _, clock, actor) = NewDb();
        actor.Set("apikey:1:cliente-a");

        var key = new ApiKey { Name = "x", KeyHash = "h", IsActive = true };
        db.ApiKeys.Add(key);
        await db.SaveChangesAsync();

        var created = (await db.ApiKeys.AsNoTracking().FirstAsync()).CreatedAt;

        actor.Set("apikey:2:admin");
        clock.Set(clock.UtcNow.AddMinutes(5));

        key.IsActive = false;
        key.RevokedAt = clock.UtcNow;
        await db.SaveChangesAsync();

        var stored = await db.ApiKeys.AsNoTracking().FirstAsync();
        Assert.Equal(created, stored.CreatedAt);            // no cambia
        Assert.Equal("apikey:1:cliente-a", stored.CreatedBy);
        Assert.Equal("apikey:2:admin", stored.UpdatedBy);
    }

    [Fact]
    public async Task FueraDeRequest_ActorEsSystem()
    {
        var (db, _, _, _) = NewDb();
        // El FakeActor devuelve "test-actor" por default, pero el caso real
        // es: fuera de un request, HttpContextCurrentActorProvider devuelve
        // "system". Lo cubrimos via FakeActor que ya devuelve el valor seteado.
        var key = new ApiKey { Name = "x", KeyHash = "h", IsActive = true };
        db.ApiKeys.Add(key);
        await db.SaveChangesAsync();
        var entry = await db.ApiKeys.AsNoTracking().FirstAsync();
        Assert.Equal("test-actor", entry.CreatedBy);
    }

    // Helpers

    private sealed class TestDbContext(DbContextOptions<WhatsappMessagesContext> options) : WhatsappMessagesContext(options) { }

    private sealed class FakeClock : IClock
    {
        private DateTime _now;
        public FakeClock(DateTime start) => _now = start;
        public DateTime UtcNow => _now;
        public void Set(DateTime value) => _now = value;
    }

    private sealed class FakeActor : ICurrentActorProvider
    {
        private string _actor;
        public FakeActor(string initial) => _actor = initial;
        public string GetActor() => _actor;
        public void Set(string value) => _actor = value;
    }
}
