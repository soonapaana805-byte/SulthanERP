using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SulthanERP.Cashier.ViewModels;

namespace SulthanERP.Cashier.Views;

public partial class LoginView : UserControl
{
    private bool _showPassword;
    public LoginView() => InitializeComponent();

    private void PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && !_showPassword) vm.Password = PasswordInput.Password;
    }

    private void VisiblePasswordChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && _showPassword) vm.Password = VisiblePasswordInput.Text;
    }

    private void TogglePassword(object sender, RoutedEventArgs e)
    {
        _showPassword = !_showPassword;
        VisiblePasswordInput.Text = PasswordInput.Password;
        PasswordInput.Visibility = _showPassword ? Visibility.Collapsed : Visibility.Visible;
        VisiblePasswordInput.Visibility = _showPassword ? Visibility.Visible : Visibility.Collapsed;
        PasswordToggle.Content = _showPassword ? "Hide" : "Show";
        if (_showPassword) VisiblePasswordInput.Focus(); else PasswordInput.Focus();
    }

    private void SubmitLogin(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || DataContext is not LoginViewModel vm || !vm.LoginCommand.CanExecute(null)) return;
        vm.LoginCommand.Execute(null);
        e.Handled = true;
    }
}
