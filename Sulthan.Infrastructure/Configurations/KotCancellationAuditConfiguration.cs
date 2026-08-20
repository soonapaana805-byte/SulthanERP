using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sulthan.Core.Entities;

namespace Sulthan.Infrastructure.Configurations;

public sealed class KotCancellationAuditConfiguration
    : IEntityTypeConfiguration<KotCancellationAudit>
{
    public void Configure(EntityTypeBuilder<KotCancellationAudit> builder)
    {
        builder.ToTable("KotCancellationAudits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.KotNumber).IsRequired().HasMaxLength(30);
        builder.Property(x => x.BillNumber).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Source).IsRequired();
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(250);
        builder.Property(x => x.RequestedByName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.ApprovedByName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.PreviousStatus).IsRequired().HasMaxLength(20);
        builder.Property(x => x.NewStatus).IsRequired().HasMaxLength(20);
        builder.Property(x => x.PreviousSubTotal).HasPrecision(18, 2);
        builder.Property(x => x.PreviousDiscount).HasPrecision(18, 2);
        builder.Property(x => x.PreviousTax).HasPrecision(18, 2);
        builder.Property(x => x.PreviousGrandTotal).HasPrecision(18, 2);
        builder.Property(x => x.NewSubTotal).HasPrecision(18, 2);
        builder.Property(x => x.NewDiscount).HasPrecision(18, 2);
        builder.Property(x => x.NewTax).HasPrecision(18, 2);
        builder.Property(x => x.NewGrandTotal).HasPrecision(18, 2);
        builder.HasIndex(x => x.KitchenOrderTicketId).IsUnique();
        builder.HasIndex(x => x.CancelledOn);
        builder.HasOne(x => x.KitchenOrderTicket).WithMany()
            .HasForeignKey(x => x.KitchenOrderTicketId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Order).WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RequestedByUser).WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ApprovedByUser).WithMany()
            .HasForeignKey(x => x.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
