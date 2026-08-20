using Sulthan.Core.Common;

namespace Sulthan.Core.Entities;

/// <summary>
/// Durable outbox entry for one KOT routed to one kitchen printer.
/// </summary>
public sealed class KitchenPrintJob : BaseEntity
{
    public int KitchenOrderTicketId { get; set; }

    public KitchenOrderTicket? KitchenOrderTicket { get; set; }

    public int? KotCancellationAuditId { get; set; }

    public KotCancellationAudit? KotCancellationAudit { get; set; }

    public string DocumentType { get; set; } = KitchenPrintDocumentType.OriginalKot;

    public string KitchenName { get; set; } = "Main Kitchen";

    public string? PrinterName { get; set; }

    public string Status { get; set; } = KitchenPrintJobStatus.Pending;

    public int Attempts { get; set; }

    public DateTime? LastAttemptOn { get; set; }

    public DateTime? NextAttemptOn { get; set; }

    public DateTime? CompletedOn { get; set; }

    public string? LastError { get; set; }
}
