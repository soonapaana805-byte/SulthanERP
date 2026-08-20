using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sulthan.Core.DTOs.Auth;
using Sulthan.Core.DTOs.Checkout;
using Sulthan.Core.DTOs.Orders;
using Sulthan.Core.DTOs.PendingOrders;
using Sulthan.Core.Enums;
using SulthanERP.Cashier.Models;
using SulthanERP.Cashier.Services;

namespace SulthanERP.Cashier.ViewModels;

public partial class PosViewModel : ObservableObject
{
    private readonly ApiService _api;
    private readonly IUserDialogService _dialogService;
    private readonly Action _showKitchenBills;
    private readonly Action _showDailySales;
    private readonly DispatcherTimer _billNotificationTimer;
    private readonly HashSet<int> _announcedBillRequests = [];
    private readonly Dictionary<int, DateTime> _nextPaymentReminderByOrderId = [];
    private bool _isCheckingBillNotifications;
    private ManagerApprovalDto? _discountApproval;
    private decimal _discountApprovedForSubTotal;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string statusMessage = "Loading menu...";
    [ObservableProperty] private string paymentErrorMessage = string.Empty;
    [ObservableProperty] private string customerName = string.Empty;
    [ObservableProperty] private string nextBillNumber = "Loading...";
    [ObservableProperty] private CategoryDto? selectedCategory;
    [ObservableProperty] private CartLineViewModel? selectedCartLine;
    [ObservableProperty] private string paymentMethod = "Cash";
    [ObservableProperty] private string orderMode = "Take Away";
    [ObservableProperty] private decimal tenderedAmount;
    [ObservableProperty] private decimal discountAmount;
    [ObservableProperty] private bool isSplitPayment;
    [ObservableProperty] private decimal splitAmount;
    [ObservableProperty] private string secondPaymentMethod = "Card";
    [ObservableProperty] private SplitPaymentLineViewModel? selectedSplitPayment;
    [ObservableProperty] private bool isCheckingOut;
    [ObservableProperty] private bool isValidatingDiscount;
    [ObservableProperty] private bool isPrintingSelectedTableBill;
    [ObservableProperty] private PendingOrderDto? activePendingOrder;
    [ObservableProperty] private DiningTableDto? selectedDiningTable;
    [ObservableProperty] private PendingOrderDto? selectedTablePendingOrder;

    private readonly Dictionary<int, List<PendingOrderDto>> _pendingTableOrdersByTableId = [];

    public ObservableCollection<MenuItemDto> MenuItems { get; } = [];
    public ObservableCollection<MenuItemDto> FilteredMenuItems { get; } = [];
    public ObservableCollection<CategoryDto> Categories { get; } = [];
    public ObservableCollection<DiningTableDto> DiningTables { get; } = [];
    public ObservableCollection<CartLineViewModel> Cart { get; } = [];
    public ObservableCollection<SplitPaymentLineViewModel> SplitPayments { get; } = [];

    public decimal Total => Cart.Sum(x => x.LineTotal);
    public decimal PayableTotal => Math.Max(0, Total - DiscountAmount);
    public string SubTotalDisplay => $"₹ {Total:N2}";
    public string DiscountDisplay => $"₹ {DiscountAmount:N2}";
    public string TotalDisplay => $"₹ {PayableTotal:N2}";
    public decimal ChangeAmount => Math.Max(0, TenderedAmount - PayableTotal);
    public decimal SplitBalance { get => Math.Max(0, PayableTotal - SplitAmount); set { } }
    public bool HasDiscount => DiscountAmount > 0;
    public string DiscountButtonText => HasDiscount ? "Change approved discount" : "Apply discount";
    public bool CanApplyDiscount => !IsValidatingDiscount;
    public bool IsCashPayment => PaymentMethod == "Cash";
    public bool IsSingleCashPayment => !IsSplitPayment && IsCashPayment;
    public bool HasPaymentError => !string.IsNullOrWhiteSpace(PaymentErrorMessage);
    public bool IsPhoneOrder => string.Equals(OrderMode, "Phone Order", StringComparison.OrdinalIgnoreCase);
    public bool IsHomeDelivery => string.Equals(OrderMode, "Home Delivery", StringComparison.OrdinalIgnoreCase);
    public bool IsTableBook => string.Equals(OrderMode, "Table Book", StringComparison.OrdinalIgnoreCase);
    public bool IsTableSelectionVisible => IsTableBook;
    public bool IsMenuSelectionVisible => !IsTableBook;
    public bool HasSelectedDiningTable => SelectedDiningTable is not null;
    public bool HasDiningTables => DiningTables.Count > 0;
    public bool IsPendingPhoneOrder => ActivePendingOrder is not null;
    public bool IsMenuEditable => !IsPendingPhoneOrder;
    public bool CanSendPhoneOrderToKitchen =>
        (IsPhoneOrder || IsHomeDelivery) && !IsPendingPhoneOrder;
    public bool CanPrintSelectedTableBill =>
        !IsPrintingSelectedTableBill &&
        SelectedDiningTable?.IsBillRequested == true &&
        SelectedTablePendingOrder is not null;
    public bool CanOpenSelectedTableBill =>
        SelectedDiningTable?.IsPaymentPending == true &&
        SelectedTablePendingOrder is not null;
    public string CompletePaymentText => IsPendingPhoneOrder ? "Collect payment" : "Complete payment";
    public string CurrentBillDisplay => IsPendingPhoneOrder
        ? $"Bill No: {ActivePendingOrder!.BillNumber}"
        : $"Next Bill No: {NextBillNumber}";

    public PosViewModel(
        ApiService api,
        IUserDialogService dialogService,
        Action showKitchenBills,
        Action showDailySales)
    {
        _api = api;
        _dialogService = dialogService;
        _showKitchenBills = showKitchenBills;
        _showDailySales = showDailySales;
        _billNotificationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _billNotificationTimer.Tick += async (_, _) => await CheckBillNotificationsAsync();
        _billNotificationTimer.Start();
        _ = LoadMenuAsync();
        _ = LoadNextBillNumberAsync();
        _ = CheckBillNotificationsAsync();
    }

    partial void OnSearchTextChanged(string value) => FilterMenu();
    partial void OnSelectedCategoryChanged(CategoryDto? value) => FilterMenu();
    partial void OnPaymentErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasPaymentError));
    partial void OnDiscountAmountChanged(decimal value)
    {
        OnPropertyChanged(nameof(PayableTotal));
        OnPropertyChanged(nameof(DiscountDisplay));
        OnPropertyChanged(nameof(TotalDisplay));
        OnPropertyChanged(nameof(ChangeAmount));
        OnPropertyChanged(nameof(SplitBalance));
        OnPropertyChanged(nameof(HasDiscount));
        OnPropertyChanged(nameof(DiscountButtonText));
        ClearPaymentError();
    }
    partial void OnNextBillNumberChanged(string value) => OnPropertyChanged(nameof(CurrentBillDisplay));
    partial void OnIsValidatingDiscountChanged(bool value) =>
        OnPropertyChanged(nameof(CanApplyDiscount));
    partial void OnOrderModeChanged(string value)
    {
        ClearPaymentError();
        OnPropertyChanged(nameof(IsPhoneOrder));
        OnPropertyChanged(nameof(IsHomeDelivery));
        OnPropertyChanged(nameof(IsTableBook));
        OnPropertyChanged(nameof(IsTableSelectionVisible));
        OnPropertyChanged(nameof(IsMenuSelectionVisible));
        OnPropertyChanged(nameof(CanSendPhoneOrderToKitchen));

        if (!IsTableBook)
        {
            SelectedDiningTable = null;
            SelectedTablePendingOrder = null;
        }
        else
            _ = LoadDiningTablesAsync();

        ApplyCurrentPricing();
    }
    partial void OnSelectedDiningTableChanged(DiningTableDto? value)
    {
        OnPropertyChanged(nameof(IsTableSelectionVisible));
        OnPropertyChanged(nameof(IsMenuSelectionVisible));
        OnPropertyChanged(nameof(HasSelectedDiningTable));
        OnPropertyChanged(nameof(CanPrintSelectedTableBill));
        OnPropertyChanged(nameof(CanOpenSelectedTableBill));
    }
    partial void OnSelectedTablePendingOrderChanged(PendingOrderDto? value)
    {
        OnPropertyChanged(nameof(CanPrintSelectedTableBill));
        OnPropertyChanged(nameof(CanOpenSelectedTableBill));
    }
    partial void OnIsPrintingSelectedTableBillChanged(bool value) =>
        OnPropertyChanged(nameof(CanPrintSelectedTableBill));
    partial void OnActivePendingOrderChanged(PendingOrderDto? value)
    {
        OnPropertyChanged(nameof(IsPendingPhoneOrder));
        OnPropertyChanged(nameof(IsMenuEditable));
        OnPropertyChanged(nameof(CanSendPhoneOrderToKitchen));
        OnPropertyChanged(nameof(CurrentBillDisplay));
        OnPropertyChanged(nameof(CompletePaymentText));
    }
    partial void OnTenderedAmountChanged(decimal value)
    {
        OnPropertyChanged(nameof(ChangeAmount));
        if (value >= PayableTotal)
            ClearPaymentError();
    }
    partial void OnPaymentMethodChanged(string value)
    {
        OnPropertyChanged(nameof(IsCashPayment));
        OnPropertyChanged(nameof(IsSingleCashPayment));
        if (value != "Cash")
            TenderedAmount = 0;
        ClearPaymentError();
    }
    partial void OnIsSplitPaymentChanged(bool value)
    {
        OnPropertyChanged(nameof(IsSingleCashPayment));
        OnPropertyChanged(nameof(SplitBalance));
        ClearPaymentError();
    }
    partial void OnSplitAmountChanged(decimal value)
    {
        OnPropertyChanged(nameof(SplitBalance));
        ClearPaymentError();
    }

    [RelayCommand]
    private async Task LoadMenuAsync()
    {
        try
        {
            StatusMessage = "Loading menu...";
            var menuTask = _api.GetAsync("MenuItems");
            var categoryTask = _api.GetAsync("Categories");
            await Task.WhenAll(menuTask, categoryTask);

            if (!menuTask.Result.IsSuccessful || !categoryTask.Result.IsSuccessful)
            {
                StatusMessage = "Could not load menu data.";
                return;
            }

            var items = JsonConvert.DeserializeObject<List<MenuItemDto>>(menuTask.Result.Content ?? "[]") ?? [];
            var categories = JsonConvert.DeserializeObject<List<CategoryDto>>(categoryTask.Result.Content ?? "[]") ?? [];

            MenuItems.Clear();
            foreach (var item in items.Where(x => x.IsAvailable))
            {
                item.DisplayPrice = GetCurrentUnitPrice(item);
                MenuItems.Add(item);
            }

            Categories.Clear();
            Categories.Add(new CategoryDto { Id = 0, Name = "All items", IsActive = true });
            foreach (var category in categories.Where(x => x.IsActive))
                Categories.Add(category);

            SelectedCategory = Categories.FirstOrDefault();
            FilterMenu();
            StatusMessage = $"Menu refreshed at {DateTime.Now:HH:mm:ss}";
        }
        catch
        {
            StatusMessage = "Unable to connect to the API.";
        }
    }

    private async Task LoadNextBillNumberAsync()
    {
        if (IsPendingPhoneOrder)
            return;

        try
        {
            var response = await _api.GetAsync("PendingOrders/next-bill-number");
            if (!response.IsSuccessful)
            {
                NextBillNumber = "New";
                return;
            }

            var json = response.Content ?? string.Empty;
            var token = JToken.Parse(json);
            NextBillNumber = token.Type == JTokenType.String
                ? token.Value<string>() ?? "New"
                : ApiService.ReadString(json, "billNumber", "nextBillNumber") ?? "New";
        }
        catch
        {
            NextBillNumber = "New";
        }
    }

    [RelayCommand]
    private void AddMenuItem(MenuItemDto? menuItem)
    {
        if (menuItem is null || !EnsureCartIsEditable())
            return;

        var line = Cart.FirstOrDefault(x => x.Item.Id == menuItem.Id);
        if (line is null)
            Cart.Add(new CartLineViewModel(menuItem, GetCurrentUnitPrice(menuItem)));
        else
            line.Quantity++;

        RefreshTotals();
    }

    [RelayCommand]
    private void SetOrderMode(string? mode)
    {
        if (IsPendingPhoneOrder)
        {
            SetPaymentError("This pending bill is already sent to kitchen. Its items cannot be changed.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(mode))
            OrderMode = mode;
    }

    [RelayCommand]
    private async Task LoadDiningTablesAsync()
    {
        try
        {
            StatusMessage = "Loading table status...";
            var tableTask = _api.GetAsync("DiningTables");
            var pendingOrderTask = _api.GetAsync("PendingOrders");
            await Task.WhenAll(tableTask, pendingOrderTask);

            if (!tableTask.Result.IsSuccessful || !pendingOrderTask.Result.IsSuccessful)
            {
                StatusMessage = "Could not load table status.";
                return;
            }

            var tables = JsonConvert.DeserializeObject<List<DiningTableDto>>(tableTask.Result.Content ?? "[]") ?? [];
            var pendingOrders = JsonConvert.DeserializeObject<List<PendingOrderDto>>(pendingOrderTask.Result.Content ?? "[]") ?? [];
            var selectedTableId = SelectedDiningTable?.Id;
            var activeTables = tables
                .Where(table => table.IsActive)
                .OrderBy(table => table.TableNumber)
                .ToList();

            _pendingTableOrdersByTableId.Clear();
            foreach (var pendingOrder in pendingOrders.Where(order =>
                         order.OrderType == OrderType.DineIn &&
                         order.DiningTableId.HasValue))
            {
                var tableId = pendingOrder.DiningTableId!.Value;
                if (!_pendingTableOrdersByTableId.TryGetValue(tableId, out var tableOrders))
                {
                    tableOrders = [];
                    _pendingTableOrdersByTableId[tableId] = tableOrders;
                }

                tableOrders.Add(pendingOrder);
            }

            DiningTables.Clear();
            foreach (var table in activeTables)
                DiningTables.Add(table);

            SelectedDiningTable = DiningTables.FirstOrDefault(table => table.Id == selectedTableId);
            SelectedTablePendingOrder = FindPendingTableOrder(SelectedDiningTable?.Id);
            OnPropertyChanged(nameof(HasDiningTables));

            StatusMessage = DiningTables.Count == 0
                ? "No active dining tables. Add a table first."
                : "Click a table to view its cashier status and captain order details.";
        }
        catch
        {
            StatusMessage = "Unable to load table status.";
        }
    }

    [RelayCommand]
    private void SelectDiningTable(DiningTableDto? table)
    {
        if (table is null)
            return;

        SelectedDiningTable = table;
        SelectedTablePendingOrder = FindPendingTableOrder(table.Id);
        ClearPaymentError();
        StatusMessage = table.IsBillRequested
            ? $"{table.TableNumber} requested a bill. Review and print it before collecting payment."
            : table.IsPaymentPending
                ? $"{table.TableNumber} bill is printed and waiting for payment."
            : table.IsOccupied
                ? $"{table.TableNumber} is occupied. Review the captain order; open payment after the captain asks for the bill."
            : table.IsCleaningPending
                ? $"{table.TableNumber} is paid and waiting for cleaning."
                : table.IsAvailable
                    ? $"{table.TableNumber} is available for a captain order."
                    : $"{table.TableNumber} is currently booked.";
    }

    [RelayCommand]
    private async Task PrintSelectedTableBillAsync()
    {
        var pendingOrder = SelectedTablePendingOrder;
        if (pendingOrder is null || !CanPrintSelectedTableBill)
            return;

        try
        {
            IsPrintingSelectedTableBill = true;
            StatusMessage = $"Sending bill {pendingOrder.BillNumber} to the cash-counter printer...";
            var printResponse = await _api.PostAsync(
                $"PendingOrders/{pendingOrder.OrderId}/bill-printed",
                new { });
            if (!printResponse.IsSuccessful)
            {
                SetPaymentError(ApiService.ReadString(
                        printResponse.Content,
                        "message",
                        "title",
                        "detail")
                    ?? "The requested bill could not be queued for printing.");
                return;
            }

            ClearPaymentError();
            var printConfirmed = await WaitForBillPrintConfirmationAsync(
                pendingOrder.OrderId);
            await LoadDiningTablesAsync();
            StatusMessage = printConfirmed
                ? $"Bill {pendingOrder.BillNumber} printed. Payment reminder starts now."
                : $"Bill {pendingOrder.BillNumber} is queued; the printer will retry automatically.";
        }
        catch (Exception ex)
        {
            SetPaymentError($"Requested bill could not be printed: {ex.Message}");
        }
        finally
        {
            IsPrintingSelectedTableBill = false;
        }
    }

    private async Task<bool> WaitForBillPrintConfirmationAsync(int orderId)
    {
        const int maximumChecks = 16;
        for (var attempt = 0; attempt < maximumChecks; attempt++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));

            var response = await _api.GetAsync($"PendingOrders/{orderId}");
            if (!response.IsSuccessful)
                continue;

            var order = JsonConvert.DeserializeObject<PendingOrderDto>(
                response.Content ?? string.Empty);
            if (order?.BillPrintedOn.HasValue == true &&
                string.Equals(
                    order.TableStatus,
                    "PaymentPending",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    [RelayCommand]
    private async Task OpenSelectedTableBillAsync()
    {
        var pendingOrder = SelectedTablePendingOrder;
        if (pendingOrder is null || !CanOpenSelectedTableBill)
            return;

        StatusMessage = $"Opening bill {pendingOrder.BillNumber} for payment...";
        await LoadPendingPhoneOrderAsync(pendingOrder.OrderId);
    }

    [RelayCommand]
    private void RemoveSelectedLine()
    {
        if (SelectedCartLine is null || !EnsureCartIsEditable())
            return;

        Cart.Remove(SelectedCartLine);
        RefreshTotals();
    }

    [RelayCommand]
    private void IncreaseQuantity(CartLineViewModel? line)
    {
        if (line is null || !EnsureCartIsEditable())
            return;

        line.Quantity++;
        RefreshTotals();
    }

    [RelayCommand]
    private void DecreaseQuantity(CartLineViewModel? line)
    {
        if (line is null || line.Quantity <= 1 || !EnsureCartIsEditable())
            return;

        line.Quantity--;
        RefreshTotals();
    }

    [RelayCommand]
    private void ShowKitchenBills()
    {
        if (IsPendingPhoneOrder)
            ResetForNextSale();

        _showKitchenBills();
    }

    [RelayCommand]
    private void ShowDailySales() => _showDailySales();

    [RelayCommand]
    private void AddSplitPayment()
    {
        if (SplitAmount <= 0 || SplitAmount >= PayableTotal)
        {
            SetPaymentError("Enter a valid first payment amount.");
            return;
        }

        ClearPaymentError();
        StatusMessage = $"Second payment balance: {SplitBalance:C}";
    }

    [RelayCommand]
    private async Task ApplyDiscountAsync()
    {
        ClearPaymentError();

        if (Cart.Count == 0 || Total <= 0)
        {
            SetPaymentError("Add at least one item before applying a discount.");
            return;
        }

        var result = _dialogService.RequestDiscountApproval(
            Total,
            DiscountAmount);

        if (result is null)
            return;

        ResetDiscountApproval();

        try
        {
            IsValidatingDiscount = true;
            StatusMessage = "Validating Admin approval...";

            var response = await _api.PostAsync(
                "Auth/validate-discount-approval",
                result.Approval);

            if (!response.IsSuccessful)
            {
                ResetDiscountApproval();
                SetPaymentError(
                    response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? "Invalid Admin credentials."
                        : ApiService.ReadString(response.Content, "message")
                          ?? "Admin approval could not be validated.");
                return;
            }

            _discountApproval = result.Approval;
            _discountApprovedForSubTotal = Total;
            DiscountAmount = result.DiscountAmount;
            TenderedAmount = 0;
            SplitAmount = 0;
            StatusMessage =
                $"Discount of ₹ {DiscountAmount:N2} approved.";
        }
        catch
        {
            ResetDiscountApproval();
            SetPaymentError(
                "Unable to validate Admin credentials. Check the server connection.");
        }
        finally
        {
            IsValidatingDiscount = false;
        }
    }

    [RelayCommand]
    private void RemoveSplitPayment()
    {
        SplitAmount = 0;
        ClearPaymentError();
        StatusMessage = "Split payment cleared.";
    }

    [RelayCommand]
    private async Task SendPhoneOrderToKitchenAsync()
    {
        ClearPaymentError();

        if (!IsPhoneOrder && !IsHomeDelivery)
        {
            SetPaymentError("Choose Phone Order or Home Delivery before sending an unpaid KOT.");
            return;
        }

        if (IsHomeDelivery && string.IsNullOrWhiteSpace(CustomerName))
        {
            SetPaymentError("Customer name is required for home delivery.");
            return;
        }

        if (Cart.Count == 0)
        {
            SetPaymentError("Add at least one item before sending the KOT.");
            return;
        }

        try
        {
            IsCheckingOut = true;
            StatusMessage = IsHomeDelivery
                ? "Saving home delivery order and kitchen ticket..."
                : "Saving phone order and kitchen ticket...";

            var request = new
            {
                orderType = IsHomeDelivery
                    ? OrderType.HomeDelivery
                    : OrderType.Parcel,
                customerName = NormalizedCustomerName(),
                discount = DiscountAmount,
                discountApproval = _discountApproval,
                remarks = IsHomeDelivery
                    ? "Home delivery"
                    : "Phone order",
                items = Cart.Select(x => new AddOrderItemDto
                {
                    MenuItemId = x.Item.Id,
                    Quantity = x.Quantity,
                    Notes = null
                }).ToList()
            };

            var response = await _api.PostAsync("PendingOrders", request);
            if (!response.IsSuccessful)
            {
                SetPaymentError(ApiService.ReadString(response.Content, "message")
                    ?? $"Order could not be sent: {(int)response.StatusCode}");
                return;
            }

            var result = JsonConvert.DeserializeObject<PendingOrderDto>(response.Content ?? string.Empty);
            if (result is null || result.OrderId <= 0)
            {
                SetPaymentError("Order was saved, but the response was invalid.");
                return;
            }

            var orderLabel = IsHomeDelivery
                ? "Home delivery"
                : "Phone order";

            _dialogService.ShowInformation(
                $"{orderLabel} saved and sent to kitchen.\n\nBill: {result.BillNumber}\nKitchen ticket: {result.KitchenTicketNumber}\nAmount pending: ₹ {result.GrandTotal:N2}\n\nPress OK to start a new bill.",
                "KOT saved");

            ResetForNextSale();
        }
        catch (Exception ex)
        {
            SetPaymentError($"Order could not be sent: {ex.Message}");
        }
        finally
        {
            IsCheckingOut = false;
        }
    }

    [RelayCommand]
    private async Task CheckoutAsync()
    {
        ClearPaymentError();

        if (Cart.Count == 0)
        {
            SetPaymentError("Add at least one item before payment.");
            return;
        }

        if (IsHomeDelivery && string.IsNullOrWhiteSpace(CustomerName))
        {
            SetPaymentError("Customer name is required for home delivery.");
            return;
        }

        if (IsCheckingOut)
            return;

        if (!IsSplitPayment && IsCashPayment && TenderedAmount < PayableTotal)
        {
            SetPaymentError($"Tendered cash (₹ {TenderedAmount:N2}) is less than the bill total (₹ {PayableTotal:N2}).");
            return;
        }

        if (IsSplitPayment && (SplitAmount <= 0 || SplitAmount >= PayableTotal))
        {
            SetPaymentError("Enter the first payment amount; the second amount is the balance.");
            return;
        }

        try
        {
            IsCheckingOut = true;
            StatusMessage = "Completing payment...";

            var response = IsPendingPhoneOrder
                ? await _api.PostAsync(
                    $"PendingOrders/{ActivePendingOrder!.OrderId}/checkout",
                    new PendingOrderCheckoutDto
                    {
                        Discount = DiscountAmount,
                        DiscountApproval = _discountApproval,
                        Payments = BuildPaymentLines()
                    })
                : await _api.PostAsync("Checkout", new CreateCheckoutDto
                {
                    OrderType = IsHomeDelivery
                        ? OrderType.HomeDelivery
                        : OrderType.Parcel,
                    CustomerName = NormalizedCustomerName(),
                    Discount = DiscountAmount,
                    DiscountApproval = _discountApproval,
                    Tax = 0m,
                    Items = Cart.Select(x => new AddOrderItemDto
                    {
                        MenuItemId = x.Item.Id,
                        Quantity = x.Quantity,
                        Notes = null
                    }).ToList(),
                    Payments = BuildPaymentLines()
                });

            if (!response.IsSuccessful)
            {
                SetPaymentError(ApiService.ReadString(response.Content, "message")
                    ?? $"Checkout could not be completed: {(int)response.StatusCode}");
                return;
            }

            var result = JsonConvert.DeserializeObject<CheckoutResponseDto>(response.Content ?? string.Empty);
            if (result is null || result.OrderId <= 0)
            {
                SetPaymentError("Checkout completed but the bill response was invalid.");
                return;
            }

            var kitchenMessage = string.IsNullOrWhiteSpace(result.KitchenTicketNumber)
                ? string.Empty
                : $"\nKitchen ticket: {result.KitchenTicketNumber}";

            _dialogService.ShowInformation(
                $"Order completed successfully.\n\nBill: {result.BillNumber}{kitchenMessage}\nReceipt queued for the cash-counter printer.\n\nPress OK to start a new bill.",
                "Payment complete");

            ResetForNextSale();
        }
        catch (Exception ex)
        {
            SetPaymentError($"Checkout failed: {ex.Message}");
        }
        finally
        {
            IsCheckingOut = false;
        }
    }

    public async Task<bool> LoadPendingPhoneOrderAsync(int orderId)
    {
        try
        {
            var response = await _api.GetAsync($"PendingOrders/{orderId}");
            if (!response.IsSuccessful)
            {
                SetPaymentError(ApiService.ReadString(response.Content, "message")
                    ?? $"Pending bill could not be loaded: {(int)response.StatusCode}");
                return false;
            }

            var pendingOrder = JsonConvert.DeserializeObject<PendingOrderDto>(response.Content ?? string.Empty);
            if (pendingOrder is null || pendingOrder.OrderId <= 0 || pendingOrder.Items.Count == 0)
            {
                SetPaymentError("The pending bill response was invalid.");
                return false;
            }

            Cart.Clear();
            foreach (var item in pendingOrder.Items)
            {
                var menuItem = new MenuItemDto
                {
                    Id = item.MenuItemId,
                    Name = item.ItemName,
                    ParcelPrice = item.Price,
                    IsAvailable = true
                };
                Cart.Add(new CartLineViewModel(menuItem, item.Price) { Quantity = item.Quantity });
            }

            ActivePendingOrder = pendingOrder;
            _discountApproval = null;
            _discountApprovedForSubTotal = 0m;
            DiscountAmount = pendingOrder.Discount;
            CustomerName = pendingOrder.CustomerName ?? string.Empty;
            OrderMode = pendingOrder.OrderType switch
            {
                OrderType.DineIn => "Table Bill",
                OrderType.HomeDelivery => "Home Delivery",
                _ => "Phone Order"
            };
            SelectedCartLine = null;
            PaymentMethod = "Cash";
            SecondPaymentMethod = "Card";
            TenderedAmount = 0;
            SplitAmount = 0;
            IsSplitPayment = false;
            ClearPaymentError();
            StatusMessage = $"Pending bill {pendingOrder.BillNumber} loaded. Choose payment and collect it.";
            RefreshTotals();
            return true;
        }
        catch (Exception ex)
        {
            SetPaymentError($"Pending bill could not be loaded: {ex.Message}");
            return false;
        }
    }

    private async Task CheckBillNotificationsAsync()
    {
        if (_isCheckingBillNotifications)
            return;

        try
        {
            _isCheckingBillNotifications = true;
            var response = await _api.GetAsync("PendingOrders");
            if (!response.IsSuccessful)
                return;

            var pendingOrders =
                JsonConvert.DeserializeObject<List<PendingOrderDto>>(
                    response.Content ?? "[]") ?? [];
            var tableOrders = pendingOrders
                .Where(order => order.OrderType == OrderType.DineIn)
                .ToList();
            var activeOrderIds = tableOrders
                .Select(order => order.OrderId)
                .ToHashSet();

            _announcedBillRequests.IntersectWith(activeOrderIds);
            foreach (var inactiveOrderId in _nextPaymentReminderByOrderId.Keys
                         .Where(orderId => !activeOrderIds.Contains(orderId))
                         .ToList())
            {
                _nextPaymentReminderByOrderId.Remove(inactiveOrderId);
            }
            var hasNewBillRequest = false;

            foreach (var order in tableOrders.OrderBy(x => x.BillRequestedOn ?? x.CreatedOn))
            {
                if (string.Equals(
                        order.TableStatus,
                        "BillRequested",
                        StringComparison.OrdinalIgnoreCase) &&
                    _announcedBillRequests.Add(order.OrderId))
                {
                    hasNewBillRequest = true;
                    _dialogService.ShowInformation(
                        $"Table {order.TableNumber ?? "—"} requested the bill.\n\n" +
                        $"Bill: {order.BillNumber}\n" +
                        $"Captain: {order.CaptainName ?? "—"}\n" +
                        $"Amount: ₹ {order.GrandTotal:N2}\n\n" +
                        "Open Table Book, select the table, and print the bill.",
                        "Bill requested");
                }

                if (string.Equals(
                        order.TableStatus,
                        "PaymentPending",
                        StringComparison.OrdinalIgnoreCase) &&
                    order.BillPrintedOn.HasValue)
                {
                    if (!_nextPaymentReminderByOrderId.TryGetValue(
                            order.OrderId,
                            out var nextReminderOn))
                    {
                        nextReminderOn = order.BillPrintedOn.Value.AddMinutes(2);
                        _nextPaymentReminderByOrderId[order.OrderId] = nextReminderOn;
                    }

                    if (DateTime.UtcNow < nextReminderOn)
                        continue;

                    _dialogService.ShowInformation(
                        $"Payment is still pending for table {order.TableNumber ?? "—"}.\n\n" +
                        $"Bill: {order.BillNumber}\n" +
                        $"Amount: ₹ {order.GrandTotal:N2}\n\n" +
                        "This reminder will repeat every minute until payment is completed.",
                        "Payment reminder");

                    _nextPaymentReminderByOrderId[order.OrderId] =
                        DateTime.UtcNow.AddMinutes(1);
                }
            }

            if (IsTableBook && hasNewBillRequest)
                await LoadDiningTablesAsync();
        }
        catch
        {
            // Notification polling must never interrupt active billing.
        }
        finally
        {
            _isCheckingBillNotifications = false;
        }
    }

    private List<CheckoutPaymentDto> BuildPaymentLines()
    {
        if (!IsSplitPayment)
        {
            return
            [
                new CheckoutPaymentDto
                {
                    PaymentMethod = ToPaymentMode(PaymentMethod),
                    Amount = PayableTotal,
                    TenderedAmount = IsCashPayment ? TenderedAmount : null
                }
            ];
        }

        var firstPaymentMethod = ToPaymentMode(PaymentMethod);
        var secondPaymentMethod = ToPaymentMode(SecondPaymentMethod);

        return
        [
            new CheckoutPaymentDto
            {
                PaymentMethod = firstPaymentMethod,
                Amount = SplitAmount,
                TenderedAmount = firstPaymentMethod == PaymentMode.Cash ? SplitAmount : null
            },
            new CheckoutPaymentDto
            {
                PaymentMethod = secondPaymentMethod,
                Amount = SplitBalance,
                TenderedAmount = secondPaymentMethod == PaymentMode.Cash ? SplitBalance : null
            }
        ];
    }

    private static PaymentMode ToPaymentMode(string method) => method switch
    {
        "Cash" => PaymentMode.Cash,
        "UPI" => PaymentMode.Upi,
        "Card" => PaymentMode.Card,
        _ => throw new ArgumentException("Choose Cash, Card, or UPI.", nameof(method))
    };

    private bool EnsureCartIsEditable()
    {
        if (!IsPendingPhoneOrder)
            return true;

        SetPaymentError("This pending bill is already sent to kitchen. Its items cannot be changed.");
        return false;
    }

    private void ResetForNextSale()
    {
        Cart.Clear();
        SelectedCartLine = null;
        ActivePendingOrder = null;
        SelectedDiningTable = null;
        CustomerName = string.Empty;
        OrderMode = "Take Away";
        PaymentMethod = "Cash";
        SecondPaymentMethod = "Card";
        TenderedAmount = 0;
        _discountApproval = null;
        _discountApprovedForSubTotal = 0m;
        DiscountAmount = 0m;
        SplitAmount = 0;
        IsSplitPayment = false;
        ClearPaymentError();
        StatusMessage = "Ready for a new bill.";
        RefreshTotals();
        _ = LoadNextBillNumberAsync();
    }

    private string? NormalizedCustomerName() => string.IsNullOrWhiteSpace(CustomerName)
        ? null
        : CustomerName.Trim();

    private void SetPaymentError(string message)
    {
        var safeMessage = ToSafeErrorMessage(message);
        PaymentErrorMessage = safeMessage;
        StatusMessage = safeMessage;
    }

    private void ResetDiscountApproval()
    {
        _discountApproval = null;
        _discountApprovedForSubTotal = 0m;
        DiscountAmount = 0m;
    }

    private static string ToSafeErrorMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "The operation could not be completed.";

        var firstLine = message
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?
            .Trim();

        if (string.IsNullOrWhiteSpace(firstLine))
            return "The operation could not be completed.";

        var exceptionMarker = firstLine.IndexOf("Exception:", StringComparison.OrdinalIgnoreCase);
        if (exceptionMarker >= 0)
            firstLine = firstLine[(exceptionMarker + "Exception:".Length)..].Trim();

        const int maximumLength = 240;
        return firstLine.Length <= maximumLength
            ? firstLine
            : firstLine[..maximumLength] + "…";
    }

    private void ClearPaymentError()
    {
        if (!string.IsNullOrEmpty(PaymentErrorMessage))
            PaymentErrorMessage = string.Empty;
    }

    private void FilterMenu()
    {
        var items = MenuItems.Where(x =>
            (SelectedCategory is null || SelectedCategory.Id == 0 || x.CategoryId == SelectedCategory.Id) &&
            (string.IsNullOrWhiteSpace(SearchText) ||
                (x.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.TamilName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)));

        FilteredMenuItems.Clear();
        foreach (var item in items)
            FilteredMenuItems.Add(item);
    }

    private void RefreshTotals()
    {
        if (_discountApproval is not null &&
            _discountApprovedForSubTotal != Total)
        {
            _discountApproval = null;
            _discountApprovedForSubTotal = 0m;
            DiscountAmount = 0m;
            StatusMessage =
                "Cart changed. The previous discount approval was cleared.";
        }

        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(PayableTotal));
        OnPropertyChanged(nameof(SubTotalDisplay));
        OnPropertyChanged(nameof(DiscountDisplay));
        OnPropertyChanged(nameof(TotalDisplay));
        OnPropertyChanged(nameof(ChangeAmount));
        OnPropertyChanged(nameof(SplitBalance));
    }

    private decimal GetCurrentUnitPrice(MenuItemDto menuItem)
    {
        return menuItem.ParcelPrice;
    }

    private void ApplyCurrentPricing()
    {
        foreach (var menuItem in MenuItems)
            menuItem.DisplayPrice = GetCurrentUnitPrice(menuItem);

        foreach (var cartLine in Cart)
            cartLine.SetUnitPrice(GetCurrentUnitPrice(cartLine.Item));

        RefreshTotals();
    }

    private PendingOrderDto? FindPendingTableOrder(int? tableId)
    {
        if (!tableId.HasValue ||
            !_pendingTableOrdersByTableId.TryGetValue(tableId.Value, out var tableOrders))
        {
            return null;
        }

        return tableOrders
            .OrderByDescending(order => order.CreatedOn)
            .FirstOrDefault();
    }
}

public partial class CartLineViewModel : ObservableObject
{
    public CartLineViewModel(MenuItemDto item, decimal unitPrice)
    {
        Item = item;
        UnitPrice = unitPrice;
    }

    [ObservableProperty] private int quantity = 1;
    [ObservableProperty] private decimal unitPrice;

    public MenuItemDto Item { get; }
    public string Name => Item.Name ?? "Unnamed item";
    public decimal LineTotal => Quantity * UnitPrice;

    partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(LineTotal));
    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(LineTotal));

    public void SetUnitPrice(decimal price) => UnitPrice = price;
}

public sealed class SplitPaymentLineViewModel(string method, decimal amount)
{
    public string Method { get; } = method;
    public decimal Amount { get; } = amount;
    public string Description => $"{Method} - {Amount:C}";
}
