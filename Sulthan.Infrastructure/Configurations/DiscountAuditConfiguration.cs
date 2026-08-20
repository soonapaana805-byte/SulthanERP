using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sulthan.Core.Entities;

namespace Sulthan.Infrastructure.Configurations;

public sealed class DiscountAuditConfiguration : IEntityTypeConfiguration<DiscountAudit>
{
    public void Configure(EntityTypeBuilder<DiscountAudit> builder)
    {
        builder.ToTable("DiscountAudits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SubTotal).HasPrecision(18, 2);
        builder.Property(x => x.PreviousDiscount).HasPrecision(18, 2);
        builder.Property(x => x.ApprovedDiscount).HasPrecision(18, 2);
        builder.Property(x => x.GrandTotal).HasPrecision(18, 2);
        builder.Property(x => x.Reason).HasMaxLength(250).IsRequired();
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.CreatedOn);
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
