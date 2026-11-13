using System.Windows;
using System.Windows.Threading;
using JoyConMouse.App.ViewModels;

namespace JoyConMouse.App.Views;

public partial class DiagnosticsWindow : Window
{
    private readonly DispatcherTimer _timer;
    private readonly Action _export;

    public DiagnosticsWindow(DiagnosticsViewModel vm, Action export)
    {
        InitializeComponent();
        DataContext = vm;
        _export = export;
        vm.Refresh();
        _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) => vm.Refresh(), Dispatcher);
        Closed += (_, _) => _timer.Stop();
    }

    private void OnExportClick(object sender, RoutedEventArgs e) => _export();
}
