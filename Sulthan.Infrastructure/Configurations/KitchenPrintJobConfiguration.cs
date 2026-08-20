using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sulthan.Core.Common;
using Sulthan.Core.Entities;

namespace Sulthan.Infrastructure.Configurations;

public sealed class KitchenPrintJobConfiguration : IEntityTypeConfiguration<KitchenPrintJob>
{
    public void Configure(EntityTypeBuilder<KitchenPrintJob> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.KitchenName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.DocumentType)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue(KitchenPrintDocumentType.OriginalKot);

        builder.Property(x => x.PrinterName)
            .HasMaxLength(250);

        // Completed is the database default so applying this migration never
        // queues historical KOTs. New jobs explicitly set Pending in code.
        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(KitchenPrintJobStatus.Completed);

        builder.Property(x => x.LastError)
            .HasMaxLength(1000);

        builder.HasIndex(x => new
            { x.KitchenOrderTicketId, x.KitchenName, x.DocumentType })
            .IsUnique();

        builder.HasIndex(x => new { x.Status, x.NextAttemptOn });

        builder.HasOne(x => x.KitchenOrderTicket)
            .WithMany()
            .HasForeignKey(x => x.KitchenOrderTicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.KotCancellationAudit)
            .WithMany()
            .HasForeignKey(x => x.KotCancellationAuditId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
