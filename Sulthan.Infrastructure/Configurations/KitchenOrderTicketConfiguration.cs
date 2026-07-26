using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sulthan.Core.Entities;

namespace Sulthan.Infrastructure.Configurations;

public class KitchenOrderTicketConfiguration : IEntityTypeConfiguration<KitchenOrderTicket>
{
    public void Configure(EntityTypeBuilder<KitchenOrderTicket> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.KotNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(x => x.KotNumber)
            .IsUnique();

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}