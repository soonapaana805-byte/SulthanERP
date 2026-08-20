using System.Windows;
using SulthanERP.Cashier.ViewModels;
namespace SulthanERP.Cashier;
public partial class MainWindow : Window { public MainWindow() { InitializeComponent(); DataContext = new ShellViewModel(); } }
