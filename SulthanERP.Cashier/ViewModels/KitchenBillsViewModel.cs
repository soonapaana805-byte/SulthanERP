using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Sulthan.Core.DTOs.Billing;
using Sulthan.Core.DTOs.PendingOrders;
using Sulthan.Core.Enums;
using SulthanERP.Cashier.Models;
using SulthanERP.Cashier.Services;

namespace SulthanERP.Cashier.ViewModels;

public partial class KitchenBillsViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly Action _returnToPos;
    private readonly Func<int, Task<bool>> _openPendingOrder;
    private readonly IUserDialogService _dialogService;
    private int _selectionVersion;

    [ObservableProperty] private string statusMessage = "Loading kitchen bills...";
    [ObservableProperty] private string detailStatusMessage = "Select a bill to see its details.";
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isDetailLoading;
    [ObservableProperty] private KitchenBillRowViewModel? selectedBill;
    [ObservableProperty] private KitchenBillDetailViewModel? selectedBillDetail;

    public ObservableCollection<KitchenBillRowViewModel> Bills { get; } = [];

    public bool CanCollectSelectedPendingOrder =>
        SelectedBill is not null && CanCollectPendingPayment(SelectedBill);
    public bool CanReprintSelectedBill =>
        SelectedBill?.CanReprint == true;
    public bool CanCancelSelectedBill => SelectedBill?.CanCancel == true;
    public bool CanCancelSelectedKot => SelectedBill?.CanCancelKot == true;
    public bool CanVoidSelectedBill => SelectedBill?.CanVoid == true;

    public KitchenBillsViewModel(
        ApiService api,
        Action returnToPos,
        Func<int, Task<bool>> openPendingOrder,
        IUserDialogService dialogService)
    {
        _api = api;
        _returnToPos = returnToPos;
        _openPendingOrder = openPendingOrder;
        _dialogService = dialogService;
        _ = LoadAsync();
    }

    partial void OnSelectedBillChanged(KitchenBillRowViewModel? value)
    {
        OnPropertyChanged(nameof(CanCollectSelectedPendingOrder));
        OnPropertyChanged(nameof(CanReprintSelectedBill));
        OnPropertyChanged(nameof(CanCancelSelectedBill));
        OnPropertyChanged(nameof(CanCancelSelectedKot));
        OnPropertyChanged(nameof(CanVoidSelectedBill));
        var selectionVersion = ++_selectionVersion;
        _ = LoadSelectedBillDetailAsync(value, selectionVersion);
    }

    [RelayCommand]
    private void BackToPos() => _returnToPos();

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsLoading)
            return;

        try
        {
            IsLoading = true;
            StatusMessage = "Loading kitchen bills...";

            var ticketsTask = _api.GetAsync("KitchenOrderTickets");
            var pendingOrdersTask = _api.GetAsync("PendingOrders");
            await Task.WhenAll(ticketsTask, pendingOrdersTask);

            var ticketsResponse = ticketsTask.Result;
            if (!ticketsResponse.IsSuccessful)
            {
                StatusMessage = ApiService.ReadString(ticketsResponse.Content, "message")
                    ?? $"Kitchen bills could not be loaded: {(int)ticketsResponse.StatusCode}";
                return;
            }

            var tickets = JsonConvert.DeserializeObject<List<KitchenOrderTicketDto>>(
                ticketsResponse.Content ?? "[]") ?? [];
            var pendingOrders = pendingOrdersTask.Result.IsSuccessful
                ? JsonConvert.DeserializeObject<List<PendingOrderDto>>(
                    pendingOrdersTask.Result.Content ?? "[]") ?? []
                : [];
            var pendingOrdersById = pendingOrders
                .GroupBy(order => order.OrderId)
                .ToDictionary(group => group.Key, group => group.First());

            Bills.Clear();
            foreach (var ticket in tickets.OrderByDescending(x => x.CreatedOn))
            {
                var order = ticket.Order;
                pendingOrdersById.TryGetValue(ticket.OrderId, out var pendingOrder);
                Bills.Add(new KitchenBillRowViewModel(
                    ticket.Id,
                    ticket.OrderId,
                    ticket.KotNumber,
                    order?.BillNumber ?? $"Order #{ticket.OrderId}",
                    ToOrderTypeLabel(order?.OrderType),
                    string.Equals(ticket.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
                        ? "KOT cancelled"
                        : ToStatusLabel(order?.BillStatus, pendingOrder),
                    order?.GrandTotal ?? 0m,
                    ticket.CreatedOn,
                    pendingOrder is not null,
                    CanCollectPendingPayment(pendingOrder, order?.BillStatus),
                    pendingOrder is not null,
                    string.Equals(ticket.Status, "Active", StringComparison.OrdinalIgnoreCase) &&
                        pendingOrder is not null &&
                        ticket.Items.Count > 0 &&
                        ticket.Items.All(x => x.OrderItemId.HasValue),
                    order?.BillStatus == (int)OrderStatus.Paid,
                    (pendingOrder is not null &&
                        CanCollectPendingPayment(pendingOrder, order?.BillStatus)) ||
                    order?.BillStatus == (int)OrderStatus.Paid,
                    ticket.Items.Select(x => new KitchenBillItemViewModel(
                        x.MenuItem?.Name ?? $"Item #{x.MenuItemId}",
                        (int)x.Quantity,
                        (x.OrderItem?.Price ?? 0m) * x.Quantity)).ToList(),
                    ticket.Items.Count > 0 && ticket.Items.All(x => x.OrderItemId.HasValue)));
            }

            SelectedBillDetail = null;
            SelectedBill = Bills.FirstOrDefault();
            StatusMessage = Bills.Count == 0
                ? "No kitchen bills have been created yet."
                : $"{Bills.Count} kitchen bill(s) loaded. Select any bill to see its details.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unable to connect to the API: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadSelectedBillDetailAsync(
        KitchenBillRowViewModel? bill,
        int selectionVersion)
    {
        if (bill is null)
        {
            SelectedBillDetail = null;
            DetailStatusMessage = "Select a bill to see its details.";
            IsDetailLoading = false;
            return;
        }

        try
        {
            IsDetailLoading = true;
            SelectedBillDetail = null;
            DetailStatusMessage = $"Loading {bill.BillNumber}...";

            var response = await _api.GetAsync($"Billing/{bill.OrderId}/lifecycle");

            if (!response.IsSuccessful)
            {
                if (selectionVersion == _selectionVersion)
                {
                    DetailStatusMessage = ApiService.ReadString(response.Content, "message")
                        ?? $"Bill details could not be loaded: {(int)response.StatusCode}";
                }
                return;
            }

            var detail = MapLifecycleBill(
                JsonConvert.DeserializeObject<BillLifecycleDto>(response.Content ?? string.Empty),
                bill);

            if (selectionVersion != _selectionVersion)
                return;

            SelectedBillDetail = detail;
            DetailStatusMessage = detail is null
                ? "The bill details response was invalid."
                : !bill.HasExplicitOwnership &&
                  !string.Equals(bill.Status, "KOT cancelled", StringComparison.OrdinalIgnoreCase)
                    ? "This KOT was created before item ownership tracking and cannot be cancelled separately. Use Cancel entire bill."
                : $"Details for {detail.BillNumber}.";
        }
        catch (Exception ex)
        {
            if (selectionVersion == _selectionVersion)
                DetailStatusMessage = $"Unable to load bill details: {ex.Message}";
        }
        finally
        {
            if (selectionVersion == _selectionVersion)
                IsDetailLoading = false;
        }
    }

    [RelayCommand]
    private async Task CollectPendingPaymentAsync()
    {
        if (SelectedBill is null)
        {
            DetailStatusMessage = "Select a pending-payment bill first.";
            return;
        }

        if (!CanCollectSelectedPendingOrder)
        {
            DetailStatusMessage = "The selected bill is not ready for payment.";
            return;
        }

        DetailStatusMessage = $"Opening pending bill {SelectedBill.BillNumber}...";
        var opened = await _openPendingOrder(SelectedBill.OrderId);
        if (!opened)
            DetailStatusMessage = "The pending bill could not be opened.";
    }

    [RelayCommand]
    private Task CancelEntireBillAsync() => ExecuteBillActionAsync(BillActionType.Cancel);

    [RelayCommand]
    private async Task CancelSelectedKotAsync()
    {
        var bill = SelectedBill;
        if (bill is null)
        {
            DetailStatusMessage = "Select a KOT first.";
            return;
        }

        if (!bill.HasExplicitOwnership)
        {
            DetailStatusMessage =
                "This KOT was created before item ownership tracking and cannot be cancelled separately. Use Cancel entire bill.";
            return;
        }

        if (!CanCancelSelectedKot)
        {
            DetailStatusMessage = "Only an active KOT from an unpaid bill can be cancelled.";
            return;
        }

        var approval = _dialogService.RequestBillActionApproval(
            BillActionType.Cancel,
            $"{bill.BillNumber} / {bill.KotNumber}",
            bill.GrandTotal,
            async submittedApproval =>
            {
                try
                {
                    var response = await _api.PostAsync(
                        $"KitchenOrderTickets/{bill.TicketId}/cancel",
                        submittedApproval);
                    if (response.IsSuccessful)
                        return null;
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                        return "Invalid Admin credentials.";
                    return ApiService.ReadString(
                        response.Content,
                        "message",
                        "title",
                        "detail") ?? "KOT cancellation could not be completed. Please try again.";
                }
                catch
                {
                    return "KOT cancellation could not be completed. Please try again.";
                }
            },
            "Cancel selected KOT");

        if (approval is null)
            return;

        _dialogService.ShowInformation(
            $"KOT {bill.KotNumber} was cancelled. A cancellation slip was queued.",
            "KOT cancelled");
        await LoadAsync();
    }

    [RelayCommand]
    private Task VoidSelectedBillAsync() => ExecuteBillActionAsync(BillActionType.Void);

    private async Task ExecuteBillActionAsync(BillActionType actionType)
    {
        var bill = SelectedBill;
        var isAllowed = actionType == BillActionType.Cancel
            ? CanCancelSelectedBill
            : CanVoidSelectedBill;
        if (bill is null || !isAllowed)
        {
            DetailStatusMessage = actionType == BillActionType.Cancel
                ? "Only an unpaid bill can be cancelled."
                : "Only a currently paid bill can be voided.";
            return;
        }

        var action = actionType == BillActionType.Cancel ? "cancel" : "void";
        var approval = _dialogService.RequestBillActionApproval(
            actionType,
            bill.BillNumber,
            bill.GrandTotal,
            async submittedApproval =>
            {
                try
                {
                    var response = await _api.PostAsync(
                        $"Billing/{bill.OrderId}/{action}",
                        submittedApproval);
                    if (response.IsSuccessful)
                        return null;

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                        return "Invalid Admin credentials.";

                    return ApiService.ReadString(
                        response.Content,
                        "message",
                        "title",
                        "detail") ?? "Bill action could not be completed. Please try again.";
                }
                catch
                {
                    return "Bill action could not be completed. Please try again.";
                }
            });
        if (approval is null)
            return;

        try
        {
            IsDetailLoading = true;
            _dialogService.ShowInformation(
                actionType == BillActionType.Cancel
                    ? $"Bill {bill.BillNumber} was cancelled."
                    : $"Bill {bill.BillNumber} was voided.",
                actionType == BillActionType.Cancel ? "Bill cancelled" : "Bill voided");
            await LoadAsync();
        }
        catch
        {
            DetailStatusMessage = "The bill was updated, but the screen could not be refreshed.";
        }
        finally
        {
            IsDetailLoading = false;
        }
    }

    [RelayCommand]
    private async Task ReprintSelectedBillAsync()
    {
        var bill = SelectedBill;
        if (bill is null || !CanReprintSelectedBill)
        {
            DetailStatusMessage =
                "Select a printed pending bill or a paid bill to reprint.";
            return;
        }

        try
        {
            DetailStatusMessage = $"Queueing reprint for {bill.BillNumber}...";
            var endpoint = bill.IsPendingOrder
                ? $"PendingOrders/{bill.OrderId}/bill-reprint"
                : $"Billing/{Uri.EscapeDataString(bill.BillNumber)}/reprint";
            var response = await _api.PostAsync(endpoint, new { });

            DetailStatusMessage = response.IsSuccessful
                ? $"Reprint for {bill.BillNumber} queued at the cash-counter printer."
                : ApiService.ReadString(
                    response.Content,
                    "message",
                    "title",
                    "detail") ?? $"Reprint failed: {(int)response.StatusCode}";
        }
        catch (Exception ex)
        {
            DetailStatusMessage = $"Reprint could not be queued: {ex.Message}";
        }
    }

    private static bool CanCollectPendingPayment(KitchenBillRowViewModel bill)
    {
        return bill.CanCollectPayment;
    }

    private static bool CanCollectPendingPayment(
        PendingOrderDto? bill,
        int? billStatus)
    {
        if (bill is null)
            return false;

        if (billStatus is not (
                (int)OrderStatus.Pending or
                (int)OrderStatus.Preparing or
                (int)OrderStatus.Ready or
                (int)OrderStatus.Served))
        {
            return false;
        }

        return bill.OrderType != OrderType.DineIn ||
               (string.Equals(
                    bill.TableStatus,
                    "PaymentPending",
                    StringComparison.OrdinalIgnoreCase) &&
                bill.BillPrintedOn.HasValue);
    }

    private static KitchenBillDetailViewModel? MapLifecycleBill(
        BillLifecycleDto? bill,
        KitchenBillRowViewModel row)
    {
        if (bill is null)
            return null;

        return new KitchenBillDetailViewModel(
            bill.KitchenTicketNumber ?? row.KotNumber,
            bill.BillNumber,
            ToOrderTypeLabel((int)bill.OrderType),
            bill.OrderStatus.ToString(),
            bill.CustomerName,
            bill.PaymentStatus?.ToString() ?? "Unpaid",
            bill.GrandTotal,
            row.KotItems);
    }

    private static KitchenBillDetailViewModel? MapPaidBill(
        BillResponseDto? bill,
        KitchenBillRowViewModel row)
    {
        if (bill is null)
            return null;

        return new KitchenBillDetailViewModel(
            row.KotNumber,
            bill.BillNumber,
            row.OrderType,
            row.Status,
            bill.CustomerName,
            string.IsNullOrWhiteSpace(bill.PaymentMethod) ? "Paid" : bill.PaymentMethod,
            bill.GrandTotal,
            bill.Items.Select(item => new KitchenBillItemViewModel(
                item.ItemName,
                item.Quantity,
                item.Total)).ToList());
    }

    private static KitchenBillDetailViewModel? MapPendingBill(
        PendingOrderDto? bill,
        KitchenBillRowViewModel row)
    {
        if (bill is null)
            return null;

        return new KitchenBillDetailViewModel(
            row.KotNumber,
            bill.BillNumber,
            row.OrderType,
            row.Status,
            bill.CustomerName,
            row.Status switch
            {
                "Open order" => "Captain order active",
                "Bill requested" => "Awaiting bill print",
                "Print required" => "Return to Table Book and print the bill",
                _ => "Payment pending"
            },
            bill.GrandTotal,
            bill.Items.Select(item => new KitchenBillItemViewModel(
                item.ItemName,
                item.Quantity,
                item.Total)).ToList());
    }

    private static string ToOrderTypeLabel(int? orderType) => orderType switch
    {
        1 => "Dine in",
        2 => "Parcel",
        3 => "Home Delivery",
        _ => "—"
    };

    private static string ToStatusLabel(
        int? billStatus,
        PendingOrderDto? pendingOrder)
    {
        if (pendingOrder is not null)
        {
            if (pendingOrder.OrderType != OrderType.DineIn)
                return "Pending payment";

            return pendingOrder.TableStatus switch
            {
                "BillRequested" => "Bill requested",
                "PaymentPending" when pendingOrder.BillPrintedOn.HasValue => "Pending payment",
                "PaymentPending" => "Print required",
                "Occupied" => "Open order",
                _ => "Pending"
            };
        }

        return billStatus switch
        {
            5 => "Paid",
            1 => "Pending",
            2 => "Preparing",
            3 => "Ready",
            4 => "Served",
            6 => "Cancelled",
            7 => "Voided",
            _ => "—"
        };
    }
}

public sealed class KitchenBillRowViewModel(
    int ticketId,
    int orderId,
    string kotNumber,
    string billNumber,
    string orderType,
    string status,
    decimal grandTotal,
    DateTime createdOn,
    bool isPendingOrder,
    bool canCollectPayment,
    bool canCancel,
    bool canCancelKot,
    bool canVoid,
    bool canReprint,
    IReadOnlyList<KitchenBillItemViewModel> kotItems,
    bool hasExplicitOwnership)
{
    public int TicketId { get; } = ticketId;
    public int OrderId { get; } = orderId;
    public string KotNumber { get; } = kotNumber;
    public string BillNumber { get; } = billNumber;
    public string ShortKotNumber { get; } = ToCompactNumber(kotNumber, "KOT");
    public string ShortBillNumber { get; } = ToCompactNumber(billNumber, "Bill");
    public string OrderType { get; } = orderType;
    public string Status { get; } = status;
    public decimal GrandTotal { get; } = grandTotal;
    public DateTime CreatedOn { get; } = createdOn;
    public bool IsPendingOrder { get; } = isPendingOrder;
    public bool CanCollectPayment { get; } = canCollectPayment;
    public bool CanCancel { get; } = canCancel;
    public bool CanCancelKot { get; } = canCancelKot;
    public bool CanVoid { get; } = canVoid;
    public bool CanReprint { get; } = canReprint;
    public IReadOnlyList<KitchenBillItemViewModel> KotItems { get; } = kotItems;
    public bool HasExplicitOwnership { get; } = hasExplicitOwnership;

    private static string ToCompactNumber(string number, string prefix)
    {
        var lastPart = number
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault();

        return string.IsNullOrWhiteSpace(lastPart) || lastPart == number
            ? number
            : $"{prefix}-{lastPart}";
    }
}

public sealed class KitchenBillDetailViewModel(
    string kotNumber,
    string billNumber,
    string orderType,
    string status,
    string? customerName,
    string paymentMethod,
    decimal grandTotal,
    IReadOnlyList<KitchenBillItemViewModel> items)
{
    public string KotNumber { get; } = kotNumber;
    public string BillNumber { get; } = billNumber;
    public string OrderType { get; } = orderType;
    public string Status { get; } = status;
    public string? CustomerName { get; } = customerName;
    public string PaymentMethod { get; } = paymentMethod;
    public decimal GrandTotal { get; } = grandTotal;
    public IReadOnlyList<KitchenBillItemViewModel> Items { get; } = items;
}

public sealed class KitchenBillItemViewModel(string itemName, int quantity, decimal total)
{
    public string ItemName { get; } = itemName;
    public int Quantity { get; } = quantity;
    public decimal Total { get; } = total;
}
