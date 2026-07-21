using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sulthan.Core.Entities;

namespace Sulthan.Infrastructure.Configurations;

public class BillCounterConfiguration : IEntityTypeConfiguration<BillCounter>
{
    public void Configure(EntityTypeBuilder<BillCounter> builder)
    {
        builder.ToTable("BillCounters");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.BusinessDate)
            .IsUnique();

        builder.Property(x => x.LastBillNumber)
            .IsRequired();
    }
}