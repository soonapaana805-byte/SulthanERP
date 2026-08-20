using Microsoft.EntityFrameworkCore;
using Sulthan.Core.Entities;

namespace Sulthan.Infrastructure.Data;

public class RestaurantDbContext : DbContext
{
    public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options)
        : base(options)
    {
    }

    public DbSet<Restaurant> Restaurants => Set<Restaurant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    public DbSet<DiningTable> DiningTables => Set<DiningTable>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();

    public DbSet<CashClosing> CashClosings => Set<CashClosing>();

    public DbSet<Settings> Settings => Set<Settings>();

    public DbSet<BillCounter> BillCounters => Set<BillCounter>();

    // KOT
    public DbSet<KitchenOrderTicket> KitchenOrderTickets => Set<KitchenOrderTicket>();

    public DbSet<KitchenOrderTicketItem> KitchenOrderTicketItems => Set<KitchenOrderTicketItem>();

    public DbSet<KitchenPrintJob> KitchenPrintJobs => Set<KitchenPrintJob>();

    public DbSet<CustomerBillPrintJob> CustomerBillPrintJobs =>
        Set<CustomerBillPrintJob>();

    public DbSet<DiscountAudit> DiscountAudits => Set<DiscountAudit>();

    public DbSet<BillActionAudit> BillActionAudits => Set<BillActionAudit>();

    public DbSet<KotCancellationAudit> KotCancellationAudits =>
        Set<KotCancellationAudit>();

    public DbSet<KotCancellationAuditItem> KotCancellationAuditItems =>
        Set<KotCancellationAuditItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RestaurantDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureBillActionAuditsAreImmutable();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnsureBillActionAuditsAreImmutable();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnsureBillActionAuditsAreImmutable()
    {
        var changedAuditExists = ChangeTracker
            .Entries<BillActionAudit>()
            .Any(x => x.State is EntityState.Modified or EntityState.Deleted);

        if (changedAuditExists)
            throw new InvalidOperationException("Bill action audit records are immutable.");

        var changedKotAuditExists = ChangeTracker
            .Entries<KotCancellationAudit>()
            .Any(x => x.State is EntityState.Modified or EntityState.Deleted);
        var changedKotAuditItemExists = ChangeTracker
            .Entries<KotCancellationAuditItem>()
            .Any(x => x.State is EntityState.Modified or EntityState.Deleted);

        if (changedKotAuditExists || changedKotAuditItemExists)
            throw new InvalidOperationException("KOT cancellation audit records are immutable.");
    }
}
