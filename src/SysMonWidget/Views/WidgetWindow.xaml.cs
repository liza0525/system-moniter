using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using SysMonWidget.Models;
using SysMonWidget.ViewModels;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;

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

    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        if (button.ContextMenu is null)
        {
            return;
        }

        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.Placement = PlacementMode.Bottom;
        button.ContextMenu.IsOpen = true;
    }

    private void OpenSettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        TogglePopupRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
