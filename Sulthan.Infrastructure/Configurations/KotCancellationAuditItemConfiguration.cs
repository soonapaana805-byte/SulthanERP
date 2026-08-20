using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sulthan.Core.Entities;

namespace Sulthan.Infrastructure.Configurations;

public sealed class KotCancellationAuditItemConfiguration
    : IEntityTypeConfiguration<KotCancellationAuditItem>
{
    public void Configure(EntityTypeBuilder<KotCancellationAuditItem> builder)
    {
        builder.ToTable("KotCancellationAuditItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ItemName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.KitchenName).IsRequired().HasMaxLength(150);
        builder.Property(x => x.CancelledQuantity).HasPrecision(18, 2);
        builder.HasOne(x => x.KotCancellationAudit).WithMany(x => x.Items)
            .HasForeignKey(x => x.KotCancellationAuditId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
