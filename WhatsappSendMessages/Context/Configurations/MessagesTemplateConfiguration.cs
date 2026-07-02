using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhatsappSendMessages.Entities;

namespace WhatsappSendMessages.Context.Configurations
{
    public class MessagesTemplateConfiguration : IEntityTypeConfiguration<MessagesTemplate>
    {
        public void Configure(EntityTypeBuilder<MessagesTemplate> entity)
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
        }
    }
}
