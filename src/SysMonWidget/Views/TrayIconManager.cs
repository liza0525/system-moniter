using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace SysMonWidget.Views;

public class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Window _widgetWindow;

    public TrayIconManager(Window widgetWindow)
    {
        _widgetWindow = widgetWindow;

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("종료", null, (_, _) => Application.Current.Shutdown());

        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "SysMonWidget",
            ContextMenuStrip = contextMenu,
        };

        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                _widgetWindow.Visibility = _widgetWindow.IsVisible ? Visibility.Hidden : Visibility.Visible;
            }
        };
    }

    public void Dispose() => _notifyIcon.Dispose();
}
