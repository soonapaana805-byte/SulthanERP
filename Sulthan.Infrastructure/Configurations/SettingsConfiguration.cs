using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sulthan.Core.Entities;

namespace Sulthan.Infrastructure.Configurations;

public class SettingsConfiguration : IEntityTypeConfiguration<Settings>
{
    public void Configure(EntityTypeBuilder<Settings> builder)
    {
        builder.ToTable("Settings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ShopName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Address)
            .HasMaxLength(300);

        builder.Property(x => x.Phone)
            .HasMaxLength(20);
    }
}