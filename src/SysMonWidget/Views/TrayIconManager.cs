using System.Windows.Forms;
using Application = System.Windows.Application;

namespace SysMonWidget.Views;

public class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly System.Drawing.Icon _icon;

    public event EventHandler? OpenSettingsRequested;

    public TrayIconManager(bool isFirstLaunch)
    {
        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add("설정 열기", null, (_, _) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty));
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("종료", null, (_, _) => Application.Current.Shutdown());

        var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"))!.Stream;
        _icon = new System.Drawing.Icon(iconStream);

        // NotifyIcon.ContextMenuStrip으로 지정해두면 우클릭 시 NotifyIcon이 자체적으로
        // SetForegroundWindow를 호출한 뒤 메뉴를 띄운다. 이전에는 좌클릭 핸들러에서
        // _contextMenu.Show()를 직접 호출했는데, 이 경로에는 그 foreground 전환이 없어서
        // 메뉴가 바깥 클릭으로 자동으로 닫히지 않는 버그가 있었다.
        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            Visible = true,
            Text = "SysMonWidget (우클릭: 메뉴)",
            ContextMenuStrip = _contextMenu,
        };

        if (isFirstLaunch)
        {
            _notifyIcon.BalloonTipTitle = "SysMonWidget 사용법";
            _notifyIcon.BalloonTipText = "트레이 아이콘 우클릭: 메뉴 열기\n위젯 우클릭: 상세 설정 열기";
            _notifyIcon.ShowBalloonTip(5000);
        }
    }

    public void Dispose()
    {
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        _icon.Dispose();
    }
}
