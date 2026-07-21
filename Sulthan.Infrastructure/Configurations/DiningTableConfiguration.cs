using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sulthan.Core.Entities;

namespace Sulthan.Infrastructure.Configurations;

public class DiningTableConfiguration : IEntityTypeConfiguration<DiningTable>
{
    public void Configure(EntityTypeBuilder<DiningTable> builder)
    {
        builder.ToTable("DiningTables");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TableNumber)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(x => x.TableNumber)
            .IsUnique();

        builder.Property(x => x.TableType)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Capacity)
            .IsRequired();

        builder.Property(x => x.DisplayOrder)
            .IsRequired();
    }
}