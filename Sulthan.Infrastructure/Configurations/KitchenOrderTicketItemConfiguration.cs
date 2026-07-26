using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sulthan.Core.Entities;

namespace Sulthan.Infrastructure.Configurations;

public class KitchenOrderTicketItemConfiguration : IEntityTypeConfiguration<KitchenOrderTicketItem>
{
    public void Configure(EntityTypeBuilder<KitchenOrderTicketItem> builder)
    {
        builder.ToTable("KitchenOrderTicketItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 2);

        builder.HasOne(x => x.KitchenOrderTicket)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.KitchenOrderTicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.MenuItem)
            .WithMany()
            .HasForeignKey(x => x.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}