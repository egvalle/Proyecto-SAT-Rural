using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SatRural.Domain.Entities;

namespace SatRural.Infrastructure.Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.Property(attachment => attachment.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(attachment => attachment.Urlcomplete)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(attachment => attachment.Urlrelative)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(attachment => attachment.Type)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(attachment => attachment.Data)
            .IsRequired();

        builder.HasOne(attachment => attachment.Clase)
            .WithMany(clase => clase.Attachments)
            .HasForeignKey(attachment => attachment.IdClase)
            .OnDelete(DeleteBehavior.Restrict);
    }
}