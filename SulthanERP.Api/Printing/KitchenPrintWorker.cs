using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sulthan.Core.Common;
using Sulthan.Core.Entities;
using Sulthan.Infrastructure.Data;

namespace SulthanERP.Api.Printing;

public sealed class KitchenPrintWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<KitchenPrintingOptions> _options;
    private readonly KitchenKotFormatter _formatter;
    private readonly KitchenKotCancellationFormatter _cancellationFormatter;
    private readonly IKitchenPrintTransport _transport;
    private readonly ILogger<KitchenPrintWorker> _logger;

    public KitchenPrintWorker(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<KitchenPrintingOptions> options,
        KitchenKotFormatter formatter,
        KitchenKotCancellationFormatter cancellationFormatter,
        IKitchenPrintTransport transport,
        ILogger<KitchenPrintWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _formatter = formatter;
        _cancellationFormatter = cancellationFormatter;
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
                if (_options.CurrentValue.Enabled)
                {
                    var processedJob = await ProcessNextJobAsync(stoppingToken);
                    if (processedJob)
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
                    "Kitchen print worker cycle failed.");
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
        var now = DateTime.UtcNow;
        var maxAttempts = _options.CurrentValue.MaxRetryAttempts;

        var job = await context.KitchenPrintJobs
            .Include(printJob => printJob.KitchenOrderTicket!)
                .ThenInclude(ticket => ticket.Order!)
                    .ThenInclude(order => order.DiningTable)
            .Include(printJob => printJob.KitchenOrderTicket!)
                .ThenInclude(ticket => ticket.Order!)
                    .ThenInclude(order => order.User)
            .Include(printJob => printJob.KitchenOrderTicket!)
                .ThenInclude(ticket => ticket.Items)
                    .ThenInclude(item => item.MenuItem)
            .Include(printJob => printJob.KotCancellationAudit!)
                .ThenInclude(audit => audit.Items)
            .Where(printJob =>
                printJob.IsActive &&
                (printJob.Status == KitchenPrintJobStatus.Pending ||
                 printJob.Status == KitchenPrintJobStatus.Failed) &&
                (!printJob.NextAttemptOn.HasValue ||
                 printJob.NextAttemptOn <= now) &&
                (maxAttempts <= 0 || printJob.Attempts < maxAttempts))
            .OrderBy(printJob => printJob.CreatedOn)
            .FirstOrDefaultAsync(cancellationToken);

        if (job is null)
            return false;

        job.Status = KitchenPrintJobStatus.Processing;
        job.Attempts++;
        job.LastAttemptOn = now;
        job.NextAttemptOn = null;
        job.LastError = null;
        job.PrinterName = ResolvePrinterName(job.KitchenName);
        job.UpdatedOn = now;
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            var isCancellation = string.Equals(
                job.DocumentType,
                KitchenPrintDocumentType.KotCancellation,
                StringComparison.OrdinalIgnoreCase);
            var content = isCancellation
                ? _cancellationFormatter.Format(job)
                : _formatter.Format(job);
            var document = new KitchenPrintDocument(
                job.Id,
                job.KitchenName,
                job.PrinterName,
                isCancellation
                    ? $"KOT-CANCEL-{job.KitchenOrderTicket!.KotNumber}"
                    : job.KitchenOrderTicket!.KotNumber,
                content);

            await _transport.PrintAsync(document, cancellationToken);

            job.Status = KitchenPrintJobStatus.Completed;
            job.CompletedOn = DateTime.UtcNow;
            job.UpdatedOn = job.CompletedOn;
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Printed {KotNumber} for {KitchenName} using {Mode}.",
                job.KitchenOrderTicket.KotNumber,
                job.KitchenName,
                _options.CurrentValue.Mode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            job.Status = KitchenPrintJobStatus.Failed;
            job.LastError = Truncate(exception.Message, 1000);
            job.NextAttemptOn = DateTime.UtcNow.AddSeconds(
                Math.Max(5, _options.CurrentValue.RetryIntervalSeconds));
            job.UpdatedOn = DateTime.UtcNow;
            await context.SaveChangesAsync(CancellationToken.None);

            _logger.LogError(
                exception,
                "Kitchen print job {PrintJobId} failed for {KitchenName}; retry scheduled.",
                job.Id,
                job.KitchenName);
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

        await context.KitchenPrintJobs
            .Where(job =>
                job.Status == KitchenPrintJobStatus.Processing &&
                job.LastAttemptOn.HasValue &&
                job.LastAttemptOn < staleBefore)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, KitchenPrintJobStatus.Failed)
                    .SetProperty(job => job.NextAttemptOn, DateTime.UtcNow)
                    .SetProperty(
                        job => job.LastError,
                        "Recovered after an interrupted print attempt.")
                    .SetProperty(job => job.UpdatedOn, DateTime.UtcNow),
                cancellationToken);
    }

    private string ResolvePrinterName(string kitchenName)
    {
        var options = _options.CurrentValue;
        var mapping = options.PrinterMappings.FirstOrDefault(pair =>
            string.Equals(
                pair.Key,
                kitchenName,
                StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(mapping.Value)
            ? options.DefaultPrinterName.Trim()
            : mapping.Value.Trim();
    }

    private static string Truncate(string value, int maximumLength)
    {
        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }
}
