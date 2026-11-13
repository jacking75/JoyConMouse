using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JoyConMouse.App.Services;
using JoyConMouse.Core.Buttons;
using JoyConMouse.Core.Engine;
using JoyConMouse.Core.Model;
using JoyConMouse.Core.Settings;
using JoyConMouse.Platform;
using JoyConMouse.Platform.Diagnostics;
using JoyConMouse.Platform.Sdl;

namespace JoyConMouse.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private static readonly Brush ActiveBrush = Freeze(Color.FromRgb(0x2E, 0xA0, 0x43));
    private static readonly Brush WaitingBrush = Freeze(Color.FromRgb(0x3B, 0x82, 0xF6));
    private static readonly Brush PausedBrush = Freeze(Color.FromRgb(0xE0, 0xA1, 0x00));
    private static readonly Brush OffBrush = Freeze(Color.FromRgb(0x9C, 0xA3, 0xAF));
    private static readonly Brush ErrorBrush = Freeze(Color.FromRgb(0xDC, 0x26, 0x26));

    private readonly InputLoop _loop;
    private readonly SettingsStore _settings;
    private EngineStatus _status = EngineStatus.Initial;
    private DeviceItemViewModel? _selectedDevice;
    private bool _updatingSelection;
    private JoyConSide _editSide;
    private ProfileViewModel _profile;
    private DeviceKey? _lastActive;
    private string _stateText = "";
    private string _batteryText = "";
    private int _batteryPercent = -1;
    private bool _batteryCharging;
    private bool _hasActiveBattery;
    private Brush _stateBrush = OffBrush;
    private string? _notice;
    private int _selectedTabIndex;
    private bool _windowShown;
    private bool _testModeOn;

    /// <summary>"연결 확인" 탭의 TabControl 순서.</summary>
    public const int TestTabIndex = 3;

    public MainViewModel(InputLoop loop, SettingsStore settings)
    {
        _loop = loop;
        _settings = settings;
        _editSide = settings.Current.LastActiveDevice?.Side ?? JoyConSide.Left;
        _profile = CreateProfileVm(_editSide);
        InputTest = new InputTestViewModel(loop);

        TogglePauseCommand = new RelayCommand(() => _loop.Post(new EngineRequest.ToggleUserPause()));
        OpenBluetoothCommand = new RelayCommand(OpenBluetoothSettings);
        RestoreDefaultsCommand = new RelayCommand(() => Profile.Load(JoyConProfile.DefaultFor(EditSide), notify: true));
        MirrorCommand = new RelayCommand(() =>
        {
            var other = _settings.Current.ProfileFor(Opposite(EditSide));
            var mirrored = DefaultLayouts.MirrorFrom(other.Buttons, EditSide);
            Profile.Load(Profile.ToProfile() with { Buttons = mirrored }, notify: true);
        });
        OpenDiagnosticsCommand = new RelayCommand(() => OpenDiagnosticsRequested?.Invoke());

        _notice = settings.LoadStatus switch
        {
            SettingsLoadStatus.Corrupt when settings.BrokenBackupPath is not null =>
                Strings.Format("Notice.SettingsBroken", System.IO.Path.GetFileName(settings.BrokenBackupPath)),
            SettingsLoadStatus.NewerVersion => Strings.Get("Notice.SettingsNewer"),
            _ => null,
        };
        ApplyStatusTexts();
    }

    public event Action? OpenDiagnosticsRequested;

    public RelayCommand TogglePauseCommand { get; }
    public RelayCommand OpenBluetoothCommand { get; }
    public RelayCommand RestoreDefaultsCommand { get; }
    public RelayCommand MirrorCommand { get; }
    public RelayCommand OpenDiagnosticsCommand { get; }

    public EngineStatus Status => _status;

    public InputTestViewModel InputTest { get; }

    /// <summary>쓰지 않을 때도 연결 유지(주기적 신호). 바꾸면 즉시 적용하고 저장한다.</summary>
    public bool KeepConnected
    {
        get => _settings.Current.KeepConnected;
        set
        {
            if (value == _settings.Current.KeepConnected)
            {
                return;
            }

            _settings.Update(s => s with { KeepConnected = value });
            _loop.Post(new EngineRequest.SetKeepAlive(value));
            OnPropertyChanged();
        }
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                OnPropertyChanged(nameof(IsTestTab));
                OnPropertyChanged(nameof(IsEditTab));
                OnPropertyChanged(nameof(DiagramSide));
                OnPropertyChanged(nameof(DiagramPressed));
                UpdateTestMode();
            }
        }
    }

    public bool IsTestTab => _selectedTabIndex == TestTabIndex;

    public bool IsEditTab => !IsTestTab;

    /// <summary>창이 보이는지(숨김·최소화가 아님). 창이 안 보이면 연결 확인 모드를 끈다.</summary>
    public void SetWindowShown(bool shown)
    {
        _windowShown = shown;
        UpdateTestMode();
    }

    /// <summary>연결 확인 탭이 보이는 동안만 실시간 입력을 받고 마우스 출력을 멈춘다.</summary>
    private void UpdateTestMode()
    {
        bool on = IsTestTab && _windowShown;
        if (on == _testModeOn)
        {
            return;
        }

        _testModeOn = on;
        _loop.Post(new EngineRequest.SetMonitoring(on));
        _loop.Post(new EngineRequest.SetPause(PauseReason.InputTest, on));
    }

    public ObservableCollection<DeviceItemViewModel> Devices { get; } = [];

    public bool HasNoDevices => Devices.Count == 0;

    public DeviceItemViewModel? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetProperty(ref _selectedDevice, value) && !_updatingSelection && value is not null)
            {
                SelectDevice(value.Key);
            }
        }
    }

    /// <summary>사용자가 활성 장치를 고름(목록·트레이 메뉴). 마지막 활성 장치로 저장한다.</summary>
    public void SelectDevice(DeviceKey key)
    {
        _loop.Post(new EngineRequest.SetActive(key));
        _loop.Post(new EngineRequest.SetSavedDevice(key));
        _settings.Update(s => s with { LastActiveDevice = key });
    }

    public string StateText { get => _stateText; private set => SetProperty(ref _stateText, value); }

    public string BatteryText { get => _batteryText; private set => SetProperty(ref _batteryText, value); }

    public int BatteryPercent { get => _batteryPercent; private set => SetProperty(ref _batteryPercent, value); }

    public bool BatteryCharging { get => _batteryCharging; private set => SetProperty(ref _batteryCharging, value); }

    public bool HasActiveBattery { get => _hasActiveBattery; private set => SetProperty(ref _hasActiveBattery, value); }

    public Brush StateBrush { get => _stateBrush; private set => SetProperty(ref _stateBrush, value); }

    public string PauseButtonText => Strings.Get((_status.Pause & PauseReason.User) != 0 ? "Button.Resume" : "Button.Pause");

    public bool CanTogglePause => _status.ActiveDevice is not null && _status.Error is null;

    public string? Notice { get => _notice; private set => SetProperty(ref _notice, value); }

    public string? ErrorText => _status.Error is { } e ? Strings.Format("Notice.SdlError", e) : null;

    public bool IsEditingLeft
    {
        get => _editSide == JoyConSide.Left;
        set
        {
            if (value)
            {
                EditSide = JoyConSide.Left;
            }
        }
    }

    public bool IsEditingRight
    {
        get => _editSide == JoyConSide.Right;
        set
        {
            if (value)
            {
                EditSide = JoyConSide.Right;
            }
        }
    }

    public JoyConSide EditSide
    {
        get => _editSide;
        set
        {
            if (_editSide == value)
            {
                return;
            }

            _editSide = value;
            Profile = CreateProfileVm(value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEditingLeft));
            OnPropertyChanged(nameof(IsEditingRight));
            OnPropertyChanged(nameof(DiagramSide));
            OnPropertyChanged(nameof(DiagramPressed));
        }
    }

    public ProfileViewModel Profile
    {
        get => _profile;
        private set => SetProperty(ref _profile, value);
    }

    /// <summary>그림에 표시할 측면: 연결 확인 중이면 활성 장치 측면, 아니면 편집 중인 측면.</summary>
    public JoyConSide DiagramSide => IsTestTab && _status.ActiveDevice is { } k ? k.Side : _editSide;

    /// <summary>그림 측면이 활성 장치 측면일 때만 눌린 버튼을 강조한다.</summary>
    public JoyConButtons DiagramPressed =>
        _status.ActiveDevice?.Side == DiagramSide ? _status.ActivePressed : JoyConButtons.None;

    /// <summary>입력 스레드 상태를 UI 스레드에서 반영한다.</summary>
    public void OnStatus(EngineStatus s)
    {
        var prev = _status;
        _status = s;

        // 활성 장치가 바뀌면 편집 측면도 따라가고, 마지막 활성 장치로 저장한다.
        if (s.ActiveDevice is { } active && active != _lastActive)
        {
            _lastActive = active;
            if (_settings.Current.LastActiveDevice != active)
            {
                _settings.Update(x => x with { LastActiveDevice = active });
            }

            EditSide = active.Side;
        }

        SyncDevices(s);
        ApplyStatusTexts();
        Profile.SetPressed(DiagramPressed);
        if (s.ActiveDevice is { } ad)
        {
            InputTest.SetDeadzone(_settings.Current.ProfileFor(ad.Side).Stick.Deadzone);
        }

        InputTest.OnStatus(s);

        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(DiagramSide));
        OnPropertyChanged(nameof(DiagramPressed));
        if (prev.Pause != s.Pause)
        {
            OnPropertyChanged(nameof(PauseButtonText));
        }

        OnPropertyChanged(nameof(CanTogglePause));
        OnPropertyChanged(nameof(ErrorText));
    }

    private void SyncDevices(EngineStatus s)
    {
        _updatingSelection = true;
        try
        {
            for (int i = Devices.Count - 1; i >= 0; i--)
            {
                if (s.Devices.All(d => d.Key != Devices[i].Key))
                {
                    Devices.RemoveAt(i);
                }
            }

            foreach (var d in s.Devices)
            {
                var vm = Devices.FirstOrDefault(x => x.Key == d.Key);
                if (vm is null)
                {
                    vm = new DeviceItemViewModel(d.Key);
                    Devices.Add(vm);
                }

                bool emphasize = s.Devices.Count(x => x.Key.Side == d.Key.Side) > 1;
                vm.Update(d, d.Key == s.ActiveDevice, emphasize);
            }

            SelectedDevice = Devices.FirstOrDefault(x => x.Key == s.ActiveDevice);
        }
        finally
        {
            _updatingSelection = false;
        }

        OnPropertyChanged(nameof(HasNoDevices));
    }

    private void ApplyStatusTexts()
    {
        var s = _status;
        string side = s.ActiveDevice is { } k ? Strings.Side(k.Side) : "";
        if (s.Error is not null)
        {
            StateText = Strings.Format("Notice.SdlError", s.Error);
            StateBrush = ErrorBrush;
        }
        else
        {
            (StateText, StateBrush) = s.State switch
            {
                EngineState.NoDevice when s.NeedsChoice => (Strings.Get("State.NeedsChoice"), PausedBrush),
                EngineState.NoDevice => (Strings.Get("State.NoDevice"), OffBrush),
                EngineState.Disconnected => (Strings.Format("State.Disconnected", side), OffBrush),
                EngineState.WaitingNeutral => (Strings.Format("State.WaitingNeutral", side), WaitingBrush),
                EngineState.Active => (Strings.Format("State.Active", side), ActiveBrush),
                _ when (s.Pause & PauseReason.InputTest) != 0 && (s.Pause & ~PauseReason.InputTest) == 0 =>
                    (Strings.Format("State.Testing", side), WaitingBrush),
                _ => (Strings.Format((s.Pause & PauseReason.User) != 0 ? "State.Paused" : "State.PausedSystem", side), PausedBrush),
            };
        }

        BatteryText = s.ActiveDevice is null ? "" : Strings.Battery(s.ActiveInfo);
        BatteryPercent = s.ActiveInfo?.BatteryPercent ?? -1;
        BatteryCharging = s.ActiveInfo?.Power == PowerSource.Charging;
        HasActiveBattery = s.ActiveDevice is not null;
    }

    private ProfileViewModel CreateProfileVm(JoyConSide side)
    {
        var vm = new ProfileViewModel(side, _settings.Current.ProfileFor(side));
        vm.Changed += OnProfileChanged;
        return vm;
    }

    private void OnProfileChanged(JoyConSide side, JoyConProfile profile)
    {
        _settings.Update(s => s.WithProfile(side, profile));
        _loop.Post(new EngineRequest.ApplyProfile(side, profile));
    }

    private static void OpenBluetoothSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo("ms-settings:bluetooth") { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Log.Error("Bluetooth 설정 열기 실패", ex);
        }
    }

    private static JoyConSide Opposite(JoyConSide s) => s == JoyConSide.Left ? JoyConSide.Right : JoyConSide.Left;

    private static SolidColorBrush Freeze(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
