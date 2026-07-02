using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhatsappSendMessages.Entities;

namespace WhatsappSendMessages.Context.Configurations
{
    public class WhatsAppAccessTokenConfiguration : IEntityTypeConfiguration<WhatsAppAccessToken>
    {
        public void Configure(EntityTypeBuilder<WhatsAppAccessToken> entity)
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Token)
                .HasMaxLength(1000)
                .IsRequired();
        }
    }
}
