using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sulthan.Core.Entities;

namespace Sulthan.Infrastructure.Configurations;

public sealed class BillActionAuditConfiguration : IEntityTypeConfiguration<BillActionAudit>
{
    public void Configure(EntityTypeBuilder<BillActionAudit> builder)
    {
        builder.ToTable("BillActionAudits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BillNumber).IsRequired().HasMaxLength(20);
        builder.Property(x => x.ActionType).IsRequired();
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(250);
        builder.Property(x => x.ActionOn).IsRequired();
        builder.Property(x => x.PreviousOrderStatus).IsRequired().HasMaxLength(30);
        builder.Property(x => x.NewOrderStatus).IsRequired().HasMaxLength(30);
        builder.Property(x => x.PreviousPaymentStatus).HasMaxLength(30);
        builder.Property(x => x.NewPaymentStatus).HasMaxLength(30);
        builder.Property(x => x.FinancialAmount).HasPrecision(18, 2);
        builder.Property(x => x.PreviousTableStatus).HasMaxLength(20);
        builder.Property(x => x.NewTableStatus).HasMaxLength(20);
        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasIndex(x => x.ActionOn);
        builder.HasIndex(x => x.ActionType);
        builder.HasIndex(x => x.ApprovedByUserId);

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RequestedByUser)
            .WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ApprovedByUser)
            .WithMany()
            .HasForeignKey(x => x.ApprovedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
