using Microsoft.EntityFrameworkCore;
using WhatsappSendMessages.Entities;

namespace WhatsappSendMessages.Context
{
    public class WhatsappMessagesContext : DbContext
    {
        public WhatsappMessagesContext() { }

        public WhatsappMessagesContext(DbContextOptions<WhatsappMessagesContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warn =>
            {
                warn.Default(WarningBehavior.Ignore);
            });

            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseSqlServer("Name=ConnectionStrings:WhatsAppMessages");
        }

        public DbSet<MessagesTemplate> MessagesTemplate { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(WhatsappMessagesContext).Assembly);
        }
    }
}
