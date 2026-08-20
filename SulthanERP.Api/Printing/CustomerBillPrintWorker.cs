using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sulthan.Core.Common;
using Sulthan.Infrastructure.Data;

namespace SulthanERP.Api.Printing;

public sealed class CustomerBillPrintWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<CashierPrintingOptions> _options;
    private readonly ICashierPrintTransport _transport;
    private readonly ILogger<CustomerBillPrintWorker> _logger;

    public CustomerBillPrintWorker(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<CashierPrintingOptions> options,
        ICashierPrintTransport transport,
        ILogger<CustomerBillPrintWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _transport = transport;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverStaleJobsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_options.CurrentValue.Enabled &&
                    await ProcessNextJobAsync(stoppingToken))
                {
                    continue;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Customer bill print worker cycle failed.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(Math.Max(
                    1,
                    _options.CurrentValue.PollIntervalSeconds)),
                stoppingToken);
        }
    }

    private async Task<bool> ProcessNextJobAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
        var formatter = scope.ServiceProvider.GetRequiredService<CustomerBillFormatter>();
        var now = DateTime.UtcNow;
        var maxAttempts = _options.CurrentValue.MaxRetryAttempts;

        var job = await context.CustomerBillPrintJobs
            .Include(x => x.RequestedByUser)
            .Include(x => x.Order!)
                .ThenInclude(x => x.DiningTable)
            .Include(x => x.Order!)
                .ThenInclude(x => x.Customer)
            .Include(x => x.Order!)
                .ThenInclude(x => x.User)
            .Include(x => x.Order!)
                .ThenInclude(x => x.Items)
                    .ThenInclude(x => x.MenuItem)
            .Where(x =>
                x.IsActive &&
                (x.Status == CustomerBillPrintJobStatus.Pending ||
                 x.Status == CustomerBillPrintJobStatus.Failed) &&
                (!x.NextAttemptOn.HasValue || x.NextAttemptOn <= now) &&
                (maxAttempts <= 0 || x.Attempts < maxAttempts))
            .OrderBy(x => x.CreatedOn)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
            return false;

        job.Status = CustomerBillPrintJobStatus.Processing;
        job.Attempts++;
        job.LastAttemptOn = now;
        job.NextAttemptOn = null;
        job.LastError = null;
        job.PrinterName = _options.CurrentValue.PrinterName.Trim();
        job.UpdatedOn = now;
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            var payment = await context.Payments
                .Include(x => x.User)
                .Include(x => x.Allocations)
                .SingleOrDefaultAsync(
                    x => x.OrderId == job.OrderId,
                    cancellationToken);
            var content = await formatter.FormatAsync(job, payment);
            var document = new CashierPrintDocument(
                job.Id,
                job.PrinterName,
                BuildJobName(job.Order!.BillNumber, job.IsReprint),
                content);

            await _transport.PrintAsync(document, cancellationToken);

            var completedOn = DateTime.UtcNow;
            job.Status = CustomerBillPrintJobStatus.Completed;
            job.CompletedOn = completedOn;
            job.UpdatedOn = completedOn;

            if (!job.IsReprint &&
                string.Equals(
                    job.DocumentType,
                    CustomerBillDocumentType.PendingBill,
                    StringComparison.Ordinal) &&
                job.Order.BillPrintedOn is null)
            {
                job.Order.BillPrintedOn = completedOn;
                job.Order.UpdatedOn = completedOn;

                if (job.Order.DiningTable is not null &&
                    string.Equals(
                        job.Order.DiningTable.Status,
                        DiningTableStatus.BillRequested,
                        StringComparison.OrdinalIgnoreCase))
                {
                    job.Order.DiningTable.Status = DiningTableStatus.PaymentPending;
                    job.Order.DiningTable.UpdatedOn = completedOn;
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Printed customer document {DocumentType} for bill {BillNumber} using {Mode}.",
                job.DocumentType,
                job.Order.BillNumber,
                _options.CurrentValue.Mode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            job.Status = CustomerBillPrintJobStatus.Failed;
            job.LastError = Truncate(exception.Message, 1000);
            job.NextAttemptOn = DateTime.UtcNow.AddSeconds(
                Math.Max(5, _options.CurrentValue.RetryIntervalSeconds));
            job.UpdatedOn = DateTime.UtcNow;
            await context.SaveChangesAsync(CancellationToken.None);

            _logger.LogError(
                exception,
                "Customer bill print job {PrintJobId} failed; retry scheduled.",
                job.Id);
        }

        return true;
    }

    private async Task RecoverStaleJobsAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
        var staleBefore = DateTime.UtcNow.AddSeconds(
            -Math.Max(30, _options.CurrentValue.ProcessingTimeoutSeconds));

        await context.CustomerBillPrintJobs
            .Where(job =>
                job.Status == CustomerBillPrintJobStatus.Processing &&
                job.LastAttemptOn.HasValue &&
                job.LastAttemptOn < staleBefore)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        job => job.Status,
                        CustomerBillPrintJobStatus.Failed)
                    .SetProperty(job => job.NextAttemptOn, DateTime.UtcNow)
                    .SetProperty(
                        job => job.LastError,
                        "Recovered after an interrupted print attempt.")
                    .SetProperty(job => job.UpdatedOn, DateTime.UtcNow),
                cancellationToken);
    }

    private static string BuildJobName(string billNumber, bool isReprint)
    {
        return isReprint
            ? $"REPRINT-{billNumber}"
            : billNumber;
    }

    private static string Truncate(string value, int maximumLength)
    {
        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }
}
