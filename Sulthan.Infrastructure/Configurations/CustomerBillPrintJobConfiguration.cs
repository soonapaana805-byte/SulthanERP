using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sulthan.Core.Common;
using Sulthan.Core.Entities;

namespace Sulthan.Infrastructure.Configurations;

public sealed class CustomerBillPrintJobConfiguration :
    IEntityTypeConfiguration<CustomerBillPrintJob>
{
    public void Configure(EntityTypeBuilder<CustomerBillPrintJob> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentType)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.RequestKey)
            .IsRequired()
            .HasMaxLength(180);

        builder.Property(x => x.PrinterName)
            .HasMaxLength(250);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(CustomerBillPrintJobStatus.Completed);

        builder.Property(x => x.LastError)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.RequestKey)
            .IsUnique();

        builder.HasIndex(x => new { x.Status, x.NextAttemptOn });

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RequestedByUser)
            .WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
