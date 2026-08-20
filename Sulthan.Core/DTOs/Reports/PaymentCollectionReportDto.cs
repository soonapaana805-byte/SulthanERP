namespace Sulthan.Core.DTOs.Reports;

public class PaymentCollectionReportDto
{
    public decimal TotalCollection { get; set; }

    public decimal CashCollection { get; set; }

    public decimal CardCollection { get; set; }

    public decimal UpiCollection { get; set; }

    public decimal MixedCollection { get; set; }
}