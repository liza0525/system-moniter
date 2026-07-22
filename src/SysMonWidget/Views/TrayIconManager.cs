using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace SysMonWidget.Views;

public class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Window _widgetWindow;

    public event EventHandler? OpenSettingsRequested;

    public TrayIconManager(Window widgetWindow, bool isFirstLaunch)
    {
        _widgetWindow = widgetWindow;

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("설정 열기", null, (_, _) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty));
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("종료", null, (_, _) => Application.Current.Shutdown());

        _notifyIcon = new NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            Text = "SysMonWidget (좌클릭: 표시/숨김, 우클릭: 메뉴)",
            ContextMenuStrip = contextMenu,
        };

        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                _widgetWindow.Visibility = _widgetWindow.IsVisible ? Visibility.Hidden : Visibility.Visible;
            }
        };

        if (isFirstLaunch)
        {
            _notifyIcon.BalloonTipTitle = "SysMonWidget 사용법";
            _notifyIcon.BalloonTipText = "좌클릭: 위젯 표시/숨김\n우클릭(위젯): 상세 설정 열기";
            _notifyIcon.ShowBalloonTip(5000);
        }
    }

    public void Dispose() => _notifyIcon.Dispose();
}
