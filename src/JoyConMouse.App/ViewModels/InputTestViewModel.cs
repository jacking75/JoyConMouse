using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JoyConMouse.Core.Model;
using JoyConMouse.Platform;
using JoyConMouse.Platform.Diagnostics;
using JoyConMouse.Platform.Sdl;

namespace JoyConMouse.App.ViewModels;

/// <summary>체크리스트의 버튼 한 개.</summary>
public sealed class ButtonCheckItem(JoyConButtons button) : ObservableObject
{
    private bool _detected, _isPressed;
    private string _raw = "";

    public JoyConButtons Button { get; } = button;

    public string Name { get; } = JoyConButtonInfo.DisplayName(button);

    public bool Detected { get => _detected; set => SetProperty(ref _detected, value); }

    public bool IsPressed { get => _isPressed; set => SetProperty(ref _isPressed, value); }

    /// <summary>처음 인식했을 때의 SDL 원시 입력.</summary>
    public string Raw { get => _raw; set => SetProperty(ref _raw, value); }
}

/// <summary>스틱 방향 확인 한 개(위·아래·왼쪽·오른쪽).</summary>
public sealed class DirectionCheckItem(string name, Func<float, float, bool> test) : ObservableObject
{
    private bool _detected;
    private string _raw = "";

    public string Name { get; } = name;

    public Func<float, float, bool> Test { get; } = test;

    public bool Detected { get => _detected; set => SetProperty(ref _detected, value); }

    public string Raw { get => _raw; set => SetProperty(ref _raw, value); }
}

/// <summary>
/// 설정 창 "연결 확인" 탭. 활성 조이콘의 버튼을 하나씩 눌러 모두 인식되는지, 스틱 방향과 중앙 흔들림이 맞는지 확인한다.
/// 확인하는 동안 엔진은 InputTest 사유로 일시정지되어 마우스 출력이 나가지 않는다.
/// </summary>
public sealed class InputTestViewModel : ObservableObject
{
    /// <summary>스틱 표시 영역 크기(px).</summary>
    public const double PadSize = 140;
    private const double DotSize = 14;
    private static readonly TimeSpan RestDuration = TimeSpan.FromSeconds(3);
    private const short TriggerThreshold = 16384;

    private readonly InputLoop _loop;
    private JoyConSide? _side;
    private DeviceKey? _device;
    private EngineStatus _status = EngineStatus.Initial;
    private float _x, _y;
    private float? _jitterMax;          // 측정 전이면 null
    private float _restRunningMax;
    private DateTime? _restUntil;       // 측정 중이면 끝나는 시각
    private string _deviceText = "", _rawText = "", _unknownText = "", _progressText = "";
    private readonly SortedSet<string> _unknownRaw = [];
    private double _deadzone = 0.15;

    public InputTestViewModel(InputLoop loop)
    {
        _loop = loop;
        Directions =
        [
            new(Strings.Get("Test.Dir.Up"), (x, y) => y < -0.8f && Math.Abs(x) < 0.45f),
            new(Strings.Get("Test.Dir.Down"), (x, y) => y > 0.8f && Math.Abs(x) < 0.45f),
            new(Strings.Get("Test.Dir.Left"), (x, y) => x < -0.8f && Math.Abs(y) < 0.45f),
            new(Strings.Get("Test.Dir.Right"), (x, y) => x > 0.8f && Math.Abs(y) < 0.45f),
        ];
        ResetCommand = new RelayCommand(Reset);
        CopyCommand = new RelayCommand(CopyResult);
        MeasureRestCommand = new RelayCommand(StartRestMeasure, () => _restUntil is null);
    }

    /// <summary>스틱에서 손을 뗀 채 3초간 중앙 흔들림 최대치를 잰다.</summary>
    public RelayCommand MeasureRestCommand { get; }

    public string RestButtonText => _restUntil is { } until
        ? Strings.Format("Test.RestMeasuring", Math.Max(1, (int)Math.Ceiling((until - DateTime.UtcNow).TotalSeconds)))
        : Strings.Get("Test.RestMeasure");

    public ObservableCollection<ButtonCheckItem> Buttons { get; } = [];

    public IReadOnlyList<DirectionCheckItem> Directions { get; }

    public RelayCommand ResetCommand { get; }

    public RelayCommand CopyCommand { get; }

    public bool HasDevice => _device is not null && _status.ActiveInfo is not null;

    public string DeviceText { get => _deviceText; private set => SetProperty(ref _deviceText, value); }

    public string ProgressText { get => _progressText; private set => SetProperty(ref _progressText, value); }

    /// <summary>현재 SDL 원시 입력(매핑이 어긋났을 때 원인 확인용).</summary>
    public string RawText { get => _rawText; private set => SetProperty(ref _rawText, value); }

    /// <summary>SDL 은 눌렸다고 하는데 어떤 조이콘 버튼으로도 바뀌지 않은 입력.</summary>
    public string UnknownText { get => _unknownText; private set => SetProperty(ref _unknownText, value); }

    public string StickText => $"x {_x,6:+0.00;-0.00} · y {_y,6:+0.00;-0.00}";

    public string JitterText => _jitterMax is { } j
        ? Strings.Format("Test.Jitter", j.ToString("0.000"), _deadzone.ToString("0.00"))
        : Strings.Get("Test.JitterNone");

    public bool JitterWarning => _jitterMax >= _deadzone;

    public double DotLeft => PadSize / 2 + _x * (PadSize / 2 - DotSize / 2) - DotSize / 2;

    public double DotTop => PadSize / 2 + _y * (PadSize / 2 - DotSize / 2) - DotSize / 2;

    public double DeadzoneDiameter => _deadzone * PadSize;

    public double DeadzoneOffset => (PadSize - DeadzoneDiameter) / 2;

    /// <summary>현재 편집 중인 측면의 데드존(표시용).</summary>
    public void SetDeadzone(double deadzone)
    {
        _deadzone = deadzone;
        OnPropertyChanged(nameof(DeadzoneDiameter));
        OnPropertyChanged(nameof(DeadzoneOffset));
        OnPropertyChanged(nameof(JitterText));
        OnPropertyChanged(nameof(JitterWarning));
    }

    public void OnStatus(EngineStatus s)
    {
        _status = s;
        if (s.ActiveDevice != _device)
        {
            _device = s.ActiveDevice;
            _side = _device?.Side;
            Reset();
        }

        var info = s.ActiveInfo;
        DeviceText = info is null || _device is not { } key
            ? Strings.Get("Test.NoDevice")
            : Strings.Format("Test.Device", Strings.Side(key.Side), ShortId(key), info.Name,
                Strings.Get(info.HasSerial ? "Test.Yes" : "Test.No"), info.Connection,
                Strings.Battery(info) + (info.BatteryPercent >= 0 ? $" ({info.BatteryPercent}%)" : ""));
        OnPropertyChanged(nameof(HasDevice));

        if (s.Input is not { } input)
        {
            return;
        }

        string raw = RawNames(input);
        RawText = Strings.Format("Test.Raw", raw.Length == 0 ? "-" : raw, AxesText(input.RawAxes));

        foreach (var item in Buttons)
        {
            bool pressed = (input.Pressed & item.Button) != 0;
            item.IsPressed = pressed;
            if (pressed && !item.Detected)
            {
                item.Detected = true;
                item.Raw = raw;
            }
        }

        if (input.Pressed == JoyConButtons.None && raw.Length > 0 && _unknownRaw.Add(raw))
        {
            UnknownText = Strings.Format("Test.Unknown", string.Join(", ", _unknownRaw));
        }

        _x = input.StickX;
        _y = input.StickY;
        foreach (var d in Directions)
        {
            if (!d.Detected && d.Test(_x, _y))
            {
                d.Detected = true;
                d.Raw = AxesText(input.RawAxes);
            }
        }

        // 중앙 흔들림: 사용자가 [손 떼고 3초 측정]을 누른 동안의 최대 크기
        if (_restUntil is { } until)
        {
            _restRunningMax = Math.Max(_restRunningMax, MathF.Sqrt(_x * _x + _y * _y));
            if (DateTime.UtcNow >= until)
            {
                _restUntil = null;
                _jitterMax = _restRunningMax;
                OnPropertyChanged(nameof(JitterText));
                OnPropertyChanged(nameof(JitterWarning));
                MeasureRestCommand.NotifyCanExecuteChanged();
            }

            OnPropertyChanged(nameof(RestButtonText));
        }

        UpdateProgress();
        OnPropertyChanged(nameof(StickText));
        OnPropertyChanged(nameof(DotLeft));
        OnPropertyChanged(nameof(DotTop));
    }

    private void Reset()
    {
        Buttons.Clear();
        if (_side is { } side)
        {
            foreach (var b in JoyConButtonInfo.ButtonsOf(side))
            {
                Buttons.Add(new ButtonCheckItem(b));
            }
        }

        foreach (var d in Directions)
        {
            d.Detected = false;
            d.Raw = "";
        }

        _unknownRaw.Clear();
        UnknownText = "";
        RawText = "";
        _jitterMax = null;
        _restUntil = null;
        OnPropertyChanged(nameof(JitterText));
        OnPropertyChanged(nameof(JitterWarning));
        OnPropertyChanged(nameof(RestButtonText));
        MeasureRestCommand.NotifyCanExecuteChanged();
        UpdateProgress();
    }

    private void StartRestMeasure()
    {
        _restRunningMax = 0;
        _restUntil = DateTime.UtcNow + RestDuration;
        OnPropertyChanged(nameof(RestButtonText));
        MeasureRestCommand.NotifyCanExecuteChanged();
    }

    private void UpdateProgress()
    {
        ProgressText = Strings.Format("Test.Progress",
            Buttons.Count(b => b.Detected), Buttons.Count, Directions.Count(d => d.Detected), Directions.Count);
    }

    private void CopyResult()
    {
        string text = BuildResult();
        Log.Info("연결 확인 결과\n" + text);
        try
        {
            Clipboard.SetText(text);
        }
        catch (COMException ex)
        {
            Log.Error("클립보드 복사 실패", ex);
        }
    }

    public string BuildResult()
    {
        var sb = new StringBuilder();
        var info = _status.ActiveInfo;
        sb.AppendLine("JoyConMouse 연결 확인 결과");
        sb.AppendLine($"시각: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        sb.AppendLine($"SDL {_status.SdlVersion}, 세로 모드 힌트={(_loop.VerticalJoyCons ? 1 : 0)}");
        if (_device is not { } key || info is null)
        {
            sb.AppendLine("활성 조이콘 없음");
            return sb.ToString();
        }

        sb.AppendLine($"장치: {Strings.Side(key.Side)} id=…{ShortId(key)} 이름=\"{info.Name}\" 시리얼={(info.HasSerial ? "있음" : "없음")} "
                      + $"연결={info.Connection} 배터리={info.Battery}({(info.BatteryPercent >= 0 ? info.BatteryPercent + "%" : "퍼센트 없음")}) 전원={info.Power}");
        sb.AppendLine($"버튼 {Buttons.Count(b => b.Detected)}/{Buttons.Count}:");
        foreach (var b in Buttons)
        {
            sb.AppendLine(b.Detected ? $"  [O] {b.Button} ({b.Name}) ← SDL {b.Raw}" : $"  [ ] {b.Button} ({b.Name}) 미확인");
        }

        sb.AppendLine($"스틱 방향 {Directions.Count(d => d.Detected)}/{Directions.Count}:");
        foreach (var d in Directions)
        {
            sb.AppendLine(d.Detected ? $"  [O] {d.Name} ← SDL 축 {d.Raw}" : $"  [ ] {d.Name} 미확인");
        }

        sb.AppendLine(_jitterMax is { } j
            ? $"중앙 흔들림 최대(손 뗀 3초): {j:0.000} (데드존 {_deadzone:0.00})"
            : "중앙 흔들림: 측정 안 함");
        sb.AppendLine($"인식되지 않은 SDL 입력: {(_unknownRaw.Count == 0 ? "없음" : string.Join(", ", _unknownRaw))}");
        return sb.ToString();
    }

    private static string ShortId(DeviceKey k) => k.Id.Length > 4 ? k.Id[^4..] : k.Id;

    private static string RawNames(ActiveInput input)
    {
        var names = new List<string>();
        for (int i = 0; i < (int)SdlGamepadButton.Count; i++)
        {
            if ((input.RawButtons & (1u << i)) != 0)
            {
                names.Add(((SdlGamepadButton)i).ToString());
            }
        }

        if (input.RawAxes.Length > (int)SdlGamepadAxis.LeftTrigger && input.RawAxes[(int)SdlGamepadAxis.LeftTrigger] > TriggerThreshold)
        {
            names.Add("LeftTrigger");
        }

        if (input.RawAxes.Length > (int)SdlGamepadAxis.RightTrigger && input.RawAxes[(int)SdlGamepadAxis.RightTrigger] > TriggerThreshold)
        {
            names.Add("RightTrigger");
        }

        return string.Join("+", names);
    }

    private static string AxesText(short[] a) => a.Length < 4
        ? "-"
        : $"LX={a[0]} LY={a[1]} RX={a[2]} RY={a[3]}";
}
