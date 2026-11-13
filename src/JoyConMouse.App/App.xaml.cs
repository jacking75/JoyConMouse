using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using JoyConMouse.App.Services;
using JoyConMouse.App.ViewModels;
using JoyConMouse.App.Views;
using JoyConMouse.Core.Engine;
using JoyConMouse.Core.Model;
using JoyConMouse.Platform;
using JoyConMouse.Platform.Diagnostics;
using JoyConMouse.Platform.Sdl;
using JoyConMouse.Platform.Win32;

namespace JoyConMouse.App;

public partial class App : Application
{
    private static readonly TimeSpan InputStopTimeout = TimeSpan.FromSeconds(1);

    private SingleInstance? _single;
    private SettingsStore? _settings;
    private InputLoop? _loop;
    private SystemEventsBridge? _bridge;
    private TrayIconService? _tray;
    private MainWindow? _window;
    private MainViewModel? _vm;
    private DiagnosticsWindow? _diagnostics;
    private int _inputStopped;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _single = SingleInstance.TryAcquire();
        if (_single is null)
        {
            // 이미 실행 중: 기존 인스턴스에 창 열기 신호만 보냈다.
            Shutdown();
            return;
        }

        bool trayStart = e.Args.Any(a => string.Equals(a, "--tray", StringComparison.OrdinalIgnoreCase));
        string version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";
        Log.Info($"앱 시작 v{version}{(trayStart ? " (--tray)" : "")}");

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        SessionEnding += (_, _) =>
        {
            // Windows 로그오프·종료: 트레이 숨김 대신 실제로 닫고, 눌린 입력을 해제한다.
            Log.Info("세션 종료");
            if (_window is not null)
            {
                _window.AllowClose = true;
            }

            StopInput("SessionEnding");
            _settings?.Flush();
        };
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        AppDomain.CurrentDomain.ProcessExit += (_, _) => StopInput("ProcessExit");

        _settings = SettingsStore.Load();
        var s = _settings.Current;

        _loop = new InputLoop(new SendInputMouseSink());
        _loop.Post(new EngineRequest.ApplyProfile(JoyConSide.Left, s.Left));
        _loop.Post(new EngineRequest.ApplyProfile(JoyConSide.Right, s.Right));
        _loop.Post(new EngineRequest.SetSavedDevice(s.LastActiveDevice));
        if (s.StartPaused)
        {
            _loop.Post(new EngineRequest.SetPause(PauseReason.User, true));
        }

        _vm = new MainViewModel(_loop, _settings);
        _vm.OpenDiagnosticsRequested += OpenDiagnostics;

        _window = new MainWindow { DataContext = _vm };
        _window.HiddenToTray += OnHiddenToTray;

        _tray = new TrayIconService(
            openSettings: OpenSettings,
            togglePause: () => _loop.Post(new EngineRequest.ToggleUserPause()),
            setActive: key => _vm.SelectDevice(key),
            exportDiagnostics: ExportDiagnostics,
            exit: ExitApp);

        _loop.StatusPublished += status => Dispatcher.BeginInvoke(() =>
        {
            _vm.OnStatus(status);
            _tray.Update(status);
        });

        _bridge = new SystemEventsBridge(_loop);
        _single.ListenForActivation(() => Dispatcher.BeginInvoke(OpenSettings));
        _loop.Start();

        if (trayStart)
        {
            if (_vm.Notice is { } notice)
            {
                _tray.ShowBalloon(notice);
            }
        }
        else
        {
            _window.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        StopInput("OnExit");
        _bridge?.Dispose();
        _tray?.Dispose();
        _settings?.Dispose();
        _single?.Dispose();
        Log.Info("앱 종료");
        base.OnExit(e);
    }

    private void OpenSettings() => _window?.BringToFront();

    private void OnHiddenToTray()
    {
        if (_settings is null || _tray is null || _settings.Current.TrayNoticeShown)
        {
            return;
        }

        _tray.ShowBalloon(Strings.Get("Tray.StillRunning"));
        _settings.Update(x => x with { TrayNoticeShown = true });
    }

    private void OpenDiagnostics()
    {
        if (_loop is null)
        {
            return;
        }

        if (_diagnostics is { IsLoaded: true })
        {
            _diagnostics.Activate();
            return;
        }

        _diagnostics = new DiagnosticsWindow(new DiagnosticsViewModel(_loop), ExportDiagnostics);
        if (_window is { IsVisible: true })
        {
            _diagnostics.Owner = _window;
        }

        _diagnostics.Show();
    }

    private void ExportDiagnostics()
    {
        if (_loop is null || _vm is null || _settings is null || _tray is null)
        {
            return;
        }

        try
        {
            string path = DiagnosticsExporter.Export(_loop, _vm.Status, _settings);
            _tray.ShowBalloon(Strings.Format("Tray.DiagnosticsSaved", Path.GetFileName(path)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("진단 정보 내보내기 실패", ex);
            _tray.ShowBalloon(Strings.Format("Tray.DiagnosticsFailed", ex.Message), System.Windows.Forms.ToolTipIcon.Warning);
        }
    }

    private void ExitApp()
    {
        if (_window is not null)
        {
            _window.AllowClose = true;
            _window.Close();
        }

        _diagnostics?.Close();
        Shutdown();
    }

    /// <summary>입력 스레드에 종료 요청 → 눌린 입력 해제 → SDL 종료. 여러 경로에서 불려도 한 번만 실행한다.</summary>
    private void StopInput(string reason)
    {
        if (_loop is null || Interlocked.Exchange(ref _inputStopped, 1) == 1)
        {
            return;
        }

        bool ok = _loop.Stop(InputStopTimeout);
        Log.Info($"입력 스레드 정지({reason}): {(ok ? "완료" : "시간 초과")}");
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error("처리되지 않은 UI 예외", e.Exception);
        StopInput("DispatcherUnhandledException");
        e.Handled = true;
        MessageBox.Show(e.Exception.Message, "JoyConMouse", MessageBoxButton.OK, MessageBoxImage.Error);
        ExitApp();
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Error("처리되지 않은 예외", e.ExceptionObject as Exception);
        StopInput("UnhandledException");
        Log.Flush();
    }
}
