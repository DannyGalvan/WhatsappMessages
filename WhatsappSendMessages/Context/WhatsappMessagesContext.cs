using Microsoft.EntityFrameworkCore;
using WhatsappSendMessages.Entities;
using WhatsappSendMessages.Entities.Auditing;

namespace WhatsappSendMessages.Context
{
    public class WhatsappMessagesContext : DbContext
    {
        public WhatsappMessagesContext() { }

        public WhatsappMessagesContext(DbContextOptions<WhatsappMessagesContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Antes: warn.Default(WarningBehavior.Ignore) escondia TODOS los
            // warnings de EF. Volvemos al default (Log) para que EF nos avise
            // si algo (esquema, query, etc) merece atencion.
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseSqlServer("Name=ConnectionStrings:WhatsAppMessages");
        }

        public DbSet<MessagesTemplate> MessagesTemplate { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }
        public DbSet<WhatsAppAccessToken> WhatsAppAccessTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(WhatsappMessagesContext).Assembly);

            // Convencion: cualquier entidad que implemente IAuditable recibe
            // los mappings default para CreatedBy/UpdatedBy. Las IEntityTypeConfiguration
            // existentes pueden sobreescribir si necesitan algo especifico.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(t => typeof(IAuditable).IsAssignableFrom(t.ClrType)))
            {
                modelBuilder.Entity(entityType.ClrType, b =>
                {
                    b.Property(nameof(IAuditable.CreatedBy)).HasMaxLength(250).IsRequired();
                    b.Property(nameof(IAuditable.UpdatedBy)).HasMaxLength(250).IsRequired();
                });
            }
        }
    }
}
