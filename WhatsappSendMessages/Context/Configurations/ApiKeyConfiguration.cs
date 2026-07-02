using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhatsappSendMessages.Entities;

namespace WhatsappSendMessages.Context.Configurations
{
    public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
    {
        public void Configure(EntityTypeBuilder<ApiKey> entity)
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(e => e.KeyHash)
                .HasMaxLength(64)
                .IsRequired();

            entity.HasIndex(e => e.KeyHash).IsUnique();
        }
    }
}
