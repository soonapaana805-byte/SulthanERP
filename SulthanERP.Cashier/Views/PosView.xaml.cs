using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using SulthanERP.Cashier.ViewModels;

namespace SulthanERP.Cashier.Views;

public partial class PosView : UserControl
{
    public PosView()
    {
        InitializeComponent();
    }

    private void CartGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        // The cell template is created after LoadingRow. Style only this row once it is ready,
        // rather than recursively walking the entire POS visual tree on every layout update.
        e.Row.Dispatcher.BeginInvoke(
            () => ApplyQuantityButtonStyle(e.Row),
            DispatcherPriority.Loaded);
    }

    private void KitchenBills_Click(object sender, RoutedEventArgs e)
    {
        ExecutePosCommand(viewModel => viewModel.ShowKitchenBillsCommand);
    }

    private void DaySummary_Click(object sender, RoutedEventArgs e)
    {
        ExecutePosCommand(viewModel => viewModel.ShowDailySalesCommand);
    }

    private void RefreshMenu_Click(object sender, RoutedEventArgs e)
    {
        ExecutePosCommand(viewModel => viewModel.LoadMenuCommand);
    }

    private void ExecutePosCommand(Func<PosViewModel, ICommand> commandSelector)
    {
        if (DataContext is not PosViewModel viewModel)
        {
            MessageBox.Show(
                "The POS screen is not ready. Close the Cashier window and start it again.",
                "Cashier action unavailable",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var command = commandSelector(viewModel);
        if (command.CanExecute(null))
            command.Execute(null);
    }

    private void ApplyQuantityButtonStyle(DependencyObject element)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            var child = VisualTreeHelper.GetChild(element, i);

            if (child is Button button && button.Content is string text && (text == "+" || text == "−" || text == "-"))
            {
                var isPlus = text == "+";
                button.Style = null;
                button.Content = new TextBlock
                {
                    Text = isPlus ? "+" : "-",
                    FontSize = 19,
                    FontWeight = FontWeights.Bold,
                    Foreground = isPlus ? Brushes.White : Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                button.Background = isPlus ? new SolidColorBrush(Color.FromRgb(37, 99, 235)) : new SolidColorBrush(Color.FromRgb(226, 232, 240));
                button.BorderThickness = new Thickness(0);
                button.Width = 24;
                button.Height = 28;
            }
            ApplyQuantityButtonStyle(child);
        }
    }
}
