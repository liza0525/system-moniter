using System.Windows;
using System.Windows.Input;
using SysMonWidget.Models;
using SysMonWidget.ViewModels;

namespace SysMonWidget.Views;

public partial class WidgetWindow : Window
{
    public event EventHandler? TogglePopupRequested;

    public WidgetWindow(WidgetViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public void ApplySettings(AppSettings settings)
    {
        Left = settings.WindowLeft;
        Top = settings.WindowTop;
        Opacity = settings.Opacity;
    }

    public (double Left, double Top) CurrentPosition => (Left, Top);

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            DragMove();
        }
    }

    private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        TogglePopupRequested?.Invoke(this, EventArgs.Empty);
    }
}
