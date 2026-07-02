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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MessagesTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.ContactInput)
                    .HasMaxLength(20);
                entity.Property(e => e.MessageId)
                    .HasMaxLength(500);
                entity.Property(e => e.MessageStatus)
                    .HasMaxLength(50);
                entity.Property(e => e.MessageTemplateName)
                    .HasMaxLength(200);
                entity.Property(e => e.MessagingProduct)
                    .HasMaxLength(50);
                entity.Property(e => e.WaId)
                    .HasMaxLength(20);
            });
        }
    }
}
