using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using JoyConMouse.Core.Engine;
using JoyConMouse.Core.Model;
using JoyConMouse.Platform;

namespace JoyConMouse.App.Services;

/// <summary>트레이 아이콘·메뉴·풍선 알림. UI 스레드에서만 사용한다.</summary>
public sealed partial class TrayIconService : IDisposable
{
    private static readonly TimeSpan NotifyInterval = TimeSpan.FromSeconds(30);

    private readonly NotifyIcon _icon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _statusItem, _pauseItem, _devicesItem, _startupItem;
    private readonly Dictionary<string, (Icon Icon, nint Handle)> _icons = [];
    private readonly Action _openSettings;
    private readonly Action<DeviceKey> _setActive;
    private EngineStatus _status = EngineStatus.Initial;
    private DateTime _lastConnectionNotice = DateTime.MinValue;
    private bool _choiceNoticeShown;

    public TrayIconService(Action openSettings, Action togglePause, Action<DeviceKey> setActive, Action exportDiagnostics, Action exit)
    {
        _openSettings = openSettings;
        _setActive = setActive;

        _statusItem = new ToolStripMenuItem { Enabled = false };
        _pauseItem = new ToolStripMenuItem(Strings.Get("Tray.Menu.Pause"), null, (_, _) => togglePause());
        _devicesItem = new ToolStripMenuItem(Strings.Get("Tray.Menu.ActiveDevice"));
        _startupItem = new ToolStripMenuItem(Strings.Get("Tray.Menu.Startup"), null, async (_, _) =>
        {
            string? message = await StartupRegistration.SetAsync(!StartupRegistration.IsRegistered);
            if (message is not null)
            {
                ShowBalloon(message, ToolTipIcon.Warning);
            }
        });

        _menu = new ContextMenuStrip();
        _menu.Items.AddRange(
        [
            _statusItem,
            new ToolStripSeparator(),
            _pauseItem,
            _devicesItem,
            new ToolStripMenuItem(Strings.Get("Tray.Menu.Open"), null, (_, _) => openSettings()) { Font = new Font(SystemFonts.MenuFont ?? Control.DefaultFont, FontStyle.Bold) },
            new ToolStripSeparator(),
            _startupItem,
            new ToolStripMenuItem(Strings.Get("Tray.Menu.Diagnostics"), null, (_, _) => exportDiagnostics()),
            new ToolStripSeparator(),
            new ToolStripMenuItem(Strings.Get("Tray.Menu.Exit"), null, (_, _) => exit()),
        ]);
        _menu.Opening += (_, _) => RefreshMenu();

        _icon = new NotifyIcon
        {
            Icon = GetIcon("off"),
            Text = Strings.Get("Tray.Title"),
            ContextMenuStrip = _menu,
            Visible = true,
        };
        _icon.DoubleClick += (_, _) => openSettings();
        _icon.BalloonTipClicked += (_, _) => openSettings();
    }

    public void Update(EngineStatus s)
    {
        var prev = _status;
        _status = s;

        string kind = s.Error is not null ? "error"
            : s.State switch
            {
                EngineState.Active => "active",
                EngineState.WaitingNeutral => "waiting",
                EngineState.Paused => "paused",
                _ => "off",
            };
        _icon.Icon = GetIcon(kind);
        string text = $"JoyConMouse — {StatusLine(s)}";
        _icon.Text = text.Length > 127 ? text[..127] : text;

        // 활성 장치 끊김·재연결 안내(30초에 한 번)
        if (s.ActiveDevice is { } key && prev.ActiveDevice == key)
        {
            bool wasConnected = prev.State is EngineState.Active or EngineState.WaitingNeutral or EngineState.Paused;
            if (wasConnected && s.State == EngineState.Disconnected)
            {
                NotifyConnection(Strings.Format("Tray.Disconnected", Strings.Side(key.Side)));
            }
            else if (prev.State == EngineState.Disconnected && s.State != EngineState.Disconnected)
            {
                NotifyConnection(Strings.Format("Tray.Reconnected", Strings.Side(key.Side)));
            }
        }

        if (s.NeedsChoice && !_choiceNoticeShown)
        {
            _choiceNoticeShown = true;
            ShowBalloon(Strings.Get("Tray.NeedsChoice"));
        }
        else if (!s.NeedsChoice)
        {
            _choiceNoticeShown = false;
        }
    }

    public void ShowBalloon(string text, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _icon.ShowBalloonTip(5000, Strings.Get("Tray.Title"), text, icon);
    }

    private void NotifyConnection(string text)
    {
        var now = DateTime.UtcNow;
        if (now - _lastConnectionNotice < NotifyInterval)
        {
            return;
        }

        _lastConnectionNotice = now;
        ShowBalloon(text);
    }

    private void RefreshMenu()
    {
        var s = _status;
        _statusItem.Text = StatusLine(s);
        _pauseItem.Text = Strings.Get((s.Pause & PauseReason.User) != 0 ? "Tray.Menu.Resume" : "Tray.Menu.Pause");
        _pauseItem.Enabled = s.ActiveDevice is not null && s.Error is null;
        _startupItem.Checked = StartupRegistration.IsRegistered;

        _devicesItem.DropDownItems.Clear();
        if (s.Devices.Count == 0)
        {
            _devicesItem.DropDownItems.Add(new ToolStripMenuItem(Strings.Get("Tray.Menu.NoDevice")) { Enabled = false });
        }

        foreach (var d in s.Devices)
        {
            var key = d.Key;
            string shortId = key.Id.Length > 4 ? key.Id[^4..] : key.Id;
            _devicesItem.DropDownItems.Add(new ToolStripMenuItem($"{Strings.Side(key.Side)} …{shortId}", null, (_, _) => _setActive(key))
            {
                Checked = key == s.ActiveDevice,
            });
        }
    }

    private static string StatusLine(EngineStatus s)
    {
        string side = s.ActiveDevice is { } k ? Strings.Side(k.Side) : "";
        if (s.Error is not null)
        {
            return Strings.Format("Notice.SdlError", s.Error);
        }

        return s.State switch
        {
            EngineState.NoDevice when s.NeedsChoice => Strings.Get("State.NeedsChoice"),
            EngineState.NoDevice => Strings.Get("State.NoDevice"),
            EngineState.Disconnected => Strings.Format("State.Disconnected", side),
            EngineState.WaitingNeutral => Strings.Format("State.WaitingNeutral", side),
            EngineState.Active => Strings.Format("State.Active", side),
            _ when (s.Pause & PauseReason.InputTest) != 0 => Strings.Format("State.Testing", side),
            _ => Strings.Format("State.Paused", side),
        };
    }

    private Icon GetIcon(string kind)
    {
        if (_icons.TryGetValue(kind, out var cached))
        {
            return cached.Icon;
        }

        var dot = kind switch
        {
            "active" => Color.FromArgb(0x2E, 0xA0, 0x43),
            "waiting" => Color.FromArgb(0x3B, 0x82, 0xF6),
            "paused" => Color.FromArgb(0xE0, 0xA1, 0x00),
            "error" => Color.FromArgb(0xDC, 0x26, 0x26),
            _ => Color.FromArgb(0x9C, 0xA3, 0xAF),
        };
        nint handle = AppIconPainter.CreateHIcon(32, dot);
        var icon = Icon.FromHandle(handle);
        _icons[kind] = (icon, handle);
        return icon;
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _menu.Dispose();
        foreach (var (icon, handle) in _icons.Values)
        {
            icon.Dispose();
            DestroyIcon(handle);
        }

        _icons.Clear();
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DestroyIcon(nint hIcon);
}

/// <summary>트레이 상태 아이콘: 앱 아이콘 원본(Assets/icon.png) 위에 오른쪽 위 상태 점을 덧그린다.</summary>
public static class AppIconPainter
{
    private static readonly Lazy<Bitmap> Base = new(() =>
    {
        using var stream = typeof(AppIconPainter).Assembly.GetManifestResourceStream("JoyConMouse.icon.png")
            ?? throw new InvalidOperationException("아이콘 리소스(JoyConMouse.icon.png)가 없습니다.");
        return new Bitmap(stream);
    });

    public static Bitmap Paint(int size, Color dot)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);
        g.DrawImage(Base.Value, 0, 0, size, size);

        // 상태 점(흰 테두리)
        float s = size / 32f;
        using var ring = new SolidBrush(Color.White);
        using var fill = new SolidBrush(dot);
        g.FillEllipse(ring, 18 * s, 0, 14 * s, 14 * s);
        g.FillEllipse(fill, 20 * s, 2 * s, 10 * s, 10 * s);
        return bmp;
    }

    public static nint CreateHIcon(int size, Color dot)
    {
        using var bmp = Paint(size, dot);
        return bmp.GetHicon();
    }
}
