namespace Sulthan.Core.Entities;

public class Settings : BaseEntity
{
    // Hotel Information
    public string ShopName { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Website { get; set; }

    public string? GstNumber { get; set; }

    // Billing
    public string CurrencySymbol { get; set; } = "₹";

    public int DecimalPlaces { get; set; } = 2;

    public bool ShowTaxOnCustomerBill { get; set; } = false;

    public bool ShowGstNumberOnBill { get; set; } = false;

    public bool ShowShopPhoneOnBill { get; set; } = true;

    public bool ShowShopAddressOnBill { get; set; } = true;

    // Printer
    public string PrinterWidth { get; set; } = "80MM";

    public bool AutoPrintAfterPayment { get; set; } = true;

    // Receipt Messages
    public string HeaderMessage { get; set; } = string.Empty;

    public string FooterMessage { get; set; } = "Thank You • Visit Again";

    // Business
    public string TimeZone { get; set; } = "Asia/Kolkata";

    public bool IsRestaurantOpen { get; set; } = true;
}