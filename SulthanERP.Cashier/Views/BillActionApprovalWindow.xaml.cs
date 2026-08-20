using System.Windows;
using Sulthan.Core.DTOs.Auth;
using Sulthan.Core.Enums;
using SulthanERP.Cashier.Models;

namespace SulthanERP.Cashier.Views;

public partial class BillActionApprovalWindow : Window
{
    private readonly BillActionType _actionType;
    private readonly Func<ManagerApprovalDto, Task<string?>> _submitAsync;

    public BillActionApprovalResult? Result { get; private set; }

    public BillActionApprovalWindow(
        BillActionType actionType,
        string billNumber,
        decimal amount,
        Func<ManagerApprovalDto, Task<string?>> submitAsync,
        string? actionLabel = null)
    {
        InitializeComponent();
        _actionType = actionType;
        _submitAsync = submitAsync;
        var action = !string.IsNullOrWhiteSpace(actionLabel)
            ? actionLabel.Trim().ToUpperInvariant()
            : actionType == BillActionType.Cancel ? "CANCEL BILL" : "VOID PAID BILL";
        HeadingText.Text = action;
        Title = !string.IsNullOrWhiteSpace(actionLabel)
            ? $"{actionLabel.Trim()} approval"
            : actionType == BillActionType.Cancel ? "Cancel bill approval" : "Void bill approval";
        BillSummaryText.Text = $"Bill {billNumber}  |  Amount: ₹ {amount:N2}";
        ConfirmButton.Content = !string.IsNullOrWhiteSpace(actionLabel)
            ? actionLabel.Trim()
            : actionType == BillActionType.Cancel ? "Cancel bill" : "Void bill";
        ManagerUserNameInput.Focus();
    }

    private async void Confirm_Click(object sender, RoutedEventArgs e)
    {
        ValidationMessage.Text = string.Empty;
        var userName = ManagerUserNameInput.Text.Trim();
        var password = ManagerPasswordInput.Password;
        var reason = ReasonInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            ValidationMessage.Text = "Active Admin username and password are required.";
            return;
        }

        if (reason.Length < 3)
        {
            ValidationMessage.Text = $"Enter a {_actionType.ToString().ToLowerInvariant()} reason with at least 3 characters.";
            return;
        }

        var approval = new ManagerApprovalDto
        {
            UserName = userName,
            Password = password,
            Reason = reason
        };

        try
        {
            ConfirmButton.IsEnabled = false;
            var errorMessage = await _submitAsync(approval);
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                ValidationMessage.Text = errorMessage;
                ManagerPasswordInput.Clear();
                ManagerPasswordInput.Focus();
                return;
            }

            Result = new BillActionApprovalResult { Approval = approval };
            DialogResult = true;
        }
        catch
        {
            ValidationMessage.Text = "Bill action could not be completed. Please try again.";
        }
        finally
        {
            ConfirmButton.IsEnabled = true;
        }
    }
}
