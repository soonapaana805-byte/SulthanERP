using Sulthan.Core.Common;

namespace Sulthan.Core.Entities;

/// <summary>
/// Durable outbox entry for a customer bill or paid receipt sent to the cash-counter printer.
/// </summary>
public sealed class CustomerBillPrintJob : BaseEntity
{
    public int OrderId { get; set; }

    public Order? Order { get; set; }

    public int RequestedByUserId { get; set; }

    public User? RequestedByUser { get; set; }

    public string DocumentType { get; set; } = CustomerBillDocumentType.PaidReceipt;

    public bool IsReprint { get; set; }

    public string RequestKey { get; set; } = string.Empty;

    public string? PrinterName { get; set; }

    public string Status { get; set; } = CustomerBillPrintJobStatus.Pending;

    public int Attempts { get; set; }

    public DateTime? LastAttemptOn { get; set; }

    public DateTime? NextAttemptOn { get; set; }

    public DateTime? CompletedOn { get; set; }

    public string? LastError { get; set; }
}
