using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sulthan.Core.Entities;

namespace Sulthan.Infrastructure.Configurations;

public sealed class CashClosingConfiguration : IEntityTypeConfiguration<CashClosing>
{
    public void Configure(EntityTypeBuilder<CashClosing> builder)
    {
        builder.ToTable("CashClosings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessDate)
            .IsRequired();

        builder.Property(x => x.ExpectedCash).HasPrecision(18, 2);
        builder.Property(x => x.CardCollection).HasPrecision(18, 2);
        builder.Property(x => x.UpiCollection).HasPrecision(18, 2);
        builder.Property(x => x.TotalCollection).HasPrecision(18, 2);
        builder.Property(x => x.CountedCash).HasPrecision(18, 2);
        builder.Property(x => x.Variance).HasPrecision(18, 2);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.Property(x => x.ClosedOn)
            .IsRequired();

        builder.HasIndex(x => new { x.CashierId, x.BusinessDate })
            .IsUnique();

        builder.HasOne(x => x.Cashier)
            .WithMany()
            .HasForeignKey(x => x.CashierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
