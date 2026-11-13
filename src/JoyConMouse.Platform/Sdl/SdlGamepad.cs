using JoyConMouse.Core.Model;

namespace JoyConMouse.Platform.Sdl;

/// <summary>열린 SDL 게임패드 하나. SDL 스레드에서만 사용한다.</summary>
public sealed unsafe class SdlGamepad : IDisposable
{
    private nint _handle;

    private SdlGamepad(nint handle, uint instanceId)
    {
        _handle = handle;
        InstanceId = instanceId;
        Type = SdlNative.SDL_GetGamepadType(handle);
        Name = SdlNative.PtrToString(SdlNative.SDL_GetGamepadName(handle)) ?? string.Empty;
        string? serial = SdlNative.PtrToString(SdlNative.SDL_GetGamepadSerial(handle));
        string? path = SdlNative.PtrToString(SdlNative.SDL_GetGamepadPath(handle));
        HasSerial = !string.IsNullOrEmpty(serial);
        HasPath = !string.IsNullOrEmpty(path);
        SerialFormat = DescribeSerial(serial);
        Side = Type switch
        {
            SdlGamepadType.NintendoSwitchJoyConLeft => JoyConSide.Left,
            SdlGamepadType.NintendoSwitchJoyConRight => JoyConSide.Right,
            _ => null,
        };
        if (Side is { } side)
        {
            Key = DeviceIdentity.CreateKey(side, serial, path);
        }
    }

    public uint InstanceId { get; }
    public SdlGamepadType Type { get; }
    public string Name { get; }
    public bool HasSerial { get; }
    public bool HasPath { get; }

    /// <summary>로그용 시리얼 형식 설명(값은 담지 않음). 예: "MAC(구분자 있음)", "기타 16자", "없음".</summary>
    public string SerialFormat { get; }

    private static string DescribeSerial(string? serial)
    {
        if (string.IsNullOrEmpty(serial))
        {
            return "없음";
        }

        string normalized = DeviceIdentity.NormalizeSerial(serial);
        if (normalized == serial)
        {
            return normalized.Length == 17 ? "MAC(구분자 있음)" : $"기타 {serial.Length}자";
        }

        return serial.Contains('-') || serial.Contains(':') ? "MAC(구분자 있음, 대문자)" : "MAC(구분자 없음)";
    }

    /// <summary>단독 조이콘이면 측면, 아니면 null.</summary>
    public JoyConSide? Side { get; }

    public DeviceKey? Key { get; }

    public nint Handle => _handle;

    public static SdlGamepad? Open(uint instanceId)
    {
        nint h = SdlNative.SDL_OpenGamepad(instanceId);
        return h == 0 ? null : new SdlGamepad(h, instanceId);
    }

    public bool GetButton(SdlGamepadButton b) => SdlNative.SDL_GetGamepadButton(_handle, b);

    public short GetAxis(SdlGamepadAxis a) => SdlNative.SDL_GetGamepadAxis(_handle, a);

    public (SdlPowerState State, int Percent) GetPower()
    {
        int percent = -1;
        var state = SdlNative.SDL_GetGamepadPowerInfo(_handle, &percent);
        return (state, percent);
    }

    public SdlConnectionState GetConnectionState() => SdlNative.SDL_GetGamepadConnectionState(_handle);

    /// <summary>마지막으로 버튼·스틱 입력이 바뀐 시각(Stopwatch tick). 연결 유지 판단에 쓴다.</summary>
    public long LastActivity { get; private set; }

    /// <summary>마지막으로 연결 유지 신호를 보낸 시각(Stopwatch tick).</summary>
    public long LastKeepAlive { get; set; }

    private uint _lastButtons;
    private short[] _lastAxes = [];

    /// <summary>버튼·축 상태를 이전 표본과 비교해 바뀌었으면 <see cref="LastActivity"/> 를 갱신한다.</summary>
    public void SampleActivity(long now)
    {
        uint buttons = GetGamepadButtonBits();
        short[] axes = GetGamepadAxes();
        bool changed = LastActivity == 0 || buttons != _lastButtons || axes.Length != _lastAxes.Length;
        for (int i = 0; !changed && i < axes.Length; i++)
        {
            changed = Math.Abs(axes[i] - _lastAxes[i]) > 2000; // 미세한 축 흔들림은 무시
        }

        if (changed)
        {
            LastActivity = now;
            _lastButtons = buttons;
            _lastAxes = axes;
        }
    }

    /// <summary>
    /// 조이콘에 연결 유지 신호를 보낸다. 플레이어 번호를 p ↔ p+8 로 바꿔 SDL 이 플레이어 LED 명령을 보내게 한다
    /// (LED 패턴은 8개 주기라 눈에 보이는 변화는 없다. 같은 번호를 다시 지정하면 SDL 이 아무것도 보내지 않는다).
    /// 명령 응답을 기다리느라 보통 수십 ms 동안 블로킹되므로 사용하지 않을 때만 호출한다.
    /// </summary>
    public bool SendKeepAlive()
    {
        int p = SdlNative.SDL_GetGamepadPlayerIndex(_handle);
        if (p < 0)
        {
            p = 0;
        }

        return SdlNative.SDL_SetGamepadPlayerIndex(_handle, p >= 8 ? p - 8 : p + 8);
    }

    /// <summary>게임패드 매핑을 거친 버튼 비트(SdlGamepadButton 순서).</summary>
    public uint GetGamepadButtonBits()
    {
        uint bits = 0;
        for (int i = 0; i < (int)SdlGamepadButton.Count; i++)
        {
            if (SdlNative.SDL_GetGamepadButton(_handle, (SdlGamepadButton)i))
            {
                bits |= 1u << i;
            }
        }

        return bits;
    }

    public short[] GetGamepadAxes()
    {
        var axes = new short[(int)SdlGamepadAxis.Count];
        for (int i = 0; i < axes.Length; i++)
        {
            axes[i] = SdlNative.SDL_GetGamepadAxis(_handle, (SdlGamepadAxis)i);
        }

        return axes;
    }

    public void Dispose()
    {
        if (_handle != 0)
        {
            SdlNative.SDL_CloseGamepad(_handle);
            _handle = 0;
        }
    }
}
