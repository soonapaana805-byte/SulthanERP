using System.Globalization;
using System.Windows;
using Sulthan.Core.DTOs.Auth;
using SulthanERP.Cashier.Models;

namespace SulthanERP.Cashier.Views;

public partial class DiscountApprovalWindow : Window
{
    private readonly decimal _subTotal;

    public DiscountApprovalResult? Result { get; private set; }

    public DiscountApprovalWindow(decimal subTotal, decimal currentDiscount)
    {
        InitializeComponent();
        _subTotal = subTotal;
        SubTotalText.Text = $"Bill subtotal: ₹ {subTotal:N2}";
        DiscountAmountInput.Text = currentDiscount.ToString("0.00", CultureInfo.CurrentCulture);
        DiscountAmountInput.SelectAll();
        DiscountAmountInput.Focus();
    }

    private void Approve_Click(object sender, RoutedEventArgs e)
    {
        ValidationMessage.Text = string.Empty;

        if (!decimal.TryParse(
                DiscountAmountInput.Text,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out var discountAmount) ||
            discountAmount <= 0)
        {
            ValidationMessage.Text = "Enter a discount amount greater than zero.";
            return;
        }

        if (discountAmount >= _subTotal)
        {
            ValidationMessage.Text = "Discount must be less than the bill subtotal.";
            return;
        }

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
            ValidationMessage.Text = "Enter a discount reason with at least 3 characters.";
            return;
        }

        Result = new DiscountApprovalResult
        {
            DiscountAmount = decimal.Round(discountAmount, 2, MidpointRounding.AwayFromZero),
            Approval = new ManagerApprovalDto
            {
                UserName = userName,
                Password = password,
                Reason = reason
            }
        };

        DialogResult = true;
    }
}
