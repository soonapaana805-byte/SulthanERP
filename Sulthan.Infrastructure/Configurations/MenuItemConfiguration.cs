using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sulthan.Core.Entities;

namespace Sulthan.Infrastructure.Configurations;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.TamilName)
            .HasMaxLength(150);

        builder.Property(x => x.KitchenName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.ACPrice)
            .HasPrecision(10, 2);

        builder.Property(x => x.NonACPrice)
            .HasPrecision(10, 2);

        builder.Property(x => x.ParcelPrice)
            .HasPrecision(10, 2);

        builder.Property(x => x.DisplayOrder)
            .IsRequired();

        builder.Property(x => x.IsAvailable)
            .HasDefaultValue(true);

        builder.Property(x => x.IsParcelAvailable)
            .HasDefaultValue(true);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.MenuItems)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}