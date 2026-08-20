using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;
using SulthanERP.Cashier.Models;
using SulthanERP.Cashier.Views;
using Sulthan.Core.DTOs.Auth;
using Sulthan.Core.Enums;

namespace SulthanERP.Cashier.Services;

public sealed class WpfUserDialogService : IUserDialogService
{
    public void ShowInformation(string message, string title)
    {
        var owner = Application.Current?.MainWindow;
        if (owner is null || !owner.IsLoaded)
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (owner.WindowState == WindowState.Minimized)
            owner.WindowState = WindowState.Normal;

        MessageBox.Show(
            owner,
            message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        if (owner.IsVisible)
        {
            owner.Activate();
            owner.Focus();
        }
    }

    public bool PrintReceipt(string receiptText, string jobName)
    {
        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true)
            return false;

        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            PagePadding = new Thickness(24),
            ColumnGap = 0,
            PageWidth = printDialog.PrintableAreaWidth,
            PageHeight = printDialog.PrintableAreaHeight,
            ColumnWidth = printDialog.PrintableAreaWidth
        };
        document.Blocks.Add(new Paragraph(new Run(receiptText))
        {
            Margin = new Thickness(0)
        });

        printDialog.PrintDocument(
            ((IDocumentPaginatorSource)document).DocumentPaginator,
            jobName);
        return true;
    }

    public DiscountApprovalResult? RequestDiscountApproval(
        decimal subTotal,
        decimal currentDiscount)
    {
        var dialog = new DiscountApprovalWindow(subTotal, currentDiscount);
        var owner = Application.Current?.MainWindow;
        if (owner is not null && owner.IsLoaded)
            dialog.Owner = owner;

        return dialog.ShowDialog() == true
            ? dialog.Result
            : null;
    }

    public BillActionApprovalResult? RequestBillActionApproval(
        BillActionType actionType,
        string billNumber,
        decimal amount,
        Func<ManagerApprovalDto, Task<string?>> submitAsync,
        string? actionLabel = null)
    {
        var dialog = new BillActionApprovalWindow(
            actionType,
            billNumber,
            amount,
            submitAsync,
            actionLabel);
        var owner = Application.Current?.MainWindow;
        if (owner is not null && owner.IsLoaded)
            dialog.Owner = owner;

        return dialog.ShowDialog() == true ? dialog.Result : null;
    }
}
