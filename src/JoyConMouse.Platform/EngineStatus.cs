using JoyConMouse.Core.Engine;
using JoyConMouse.Core.Model;

namespace JoyConMouse.Platform;

public enum BatteryLevel { Unknown, Critical, Low, Medium, High }

public enum PowerSource { Unknown, Battery, Charging, Charged, Wired }

public enum ConnectionKind { Unknown, Wired, Wireless }

/// <summary>연결된 조이콘 한 대의 표시 정보. 원본 시리얼·경로는 담지 않는다.</summary>
public sealed record DeviceInfo(
    DeviceKey Key,
    string Name,
    BatteryLevel Battery,
    int BatteryPercent,
    PowerSource Power,
    bool HasSerial,
    ConnectionKind Connection)
{
    public static BatteryLevel LevelFromPercent(int percent) => percent switch
    {
        < 0 => BatteryLevel.Unknown,
        >= 70 => BatteryLevel.High,
        >= 40 => BatteryLevel.Medium,
        >= 15 => BatteryLevel.Low,
        _ => BatteryLevel.Critical,
    };
}

/// <summary>
/// 연결 확인 화면용 활성 장치 실시간 입력. 정규화 값과 함께 SDL 원시 값(게임패드 버튼 비트, 축 6개)을 담아
/// 매핑이 어긋났을 때 원인을 볼 수 있게 한다.
/// </summary>
public sealed record ActiveInput(float StickX, float StickY, JoyConButtons Pressed, uint RawButtons, short[] RawAxes);

/// <summary>입력 스레드가 발행하는 불변 상태 스냅샷.</summary>
public sealed record EngineStatus(
    EngineState State,
    DeviceKey? ActiveDevice,
    PauseReason Pause,
    IReadOnlyList<DeviceInfo> Devices,
    JoyConButtons ActivePressed,
    bool NeedsChoice,
    string? Error,
    string SdlVersion,
    ActiveInput? Input = null)
{
    public static EngineStatus Initial { get; } =
        new(EngineState.NoDevice, null, PauseReason.None, [], JoyConButtons.None, false, null, "");

    public DeviceInfo? ActiveInfo => ActiveDevice is { } k ? Devices.FirstOrDefault(d => d.Key == k) : null;
}
