namespace JoyConMouse.Core.Model;

public enum JoyConSide { Left, Right }

/// <summary>조이콘 물리 버튼. 좌우 장치는 서로 다른 비트를 쓰고, 레일 버튼(SL·SR)만 공통이다.</summary>
[Flags]
public enum JoyConButtons : uint
{
    None = 0,
    // 왼쪽
    ZL = 1 << 0, L = 1 << 1, Minus = 1 << 2, Capture = 1 << 3, LeftStick = 1 << 4,
    DpadUp = 1 << 5, DpadDown = 1 << 6, DpadLeft = 1 << 7, DpadRight = 1 << 8,
    // 오른쪽
    ZR = 1 << 9, R = 1 << 10, Plus = 1 << 11, Home = 1 << 12, RightStick = 1 << 13,
    X = 1 << 14, B = 1 << 15, Y = 1 << 16, A = 1 << 17,
    // 공통(레일 버튼)
    SL = 1 << 18, SR = 1 << 19,
}

/// <summary>
/// 정규화된 조이콘 입력 한 프레임. 세로로 쥔 기준이며
/// X는 오른쪽 +, Y는 아래쪽 +(화면 좌표와 같은 방향), 범위 [-1, 1].
/// </summary>
public readonly record struct JoyConSnapshot(float StickX, float StickY, JoyConButtons Pressed);

/// <summary>장치 식별자. Id는 시리얼(없으면 경로)의 해시이며 원본 값은 담지 않는다.</summary>
public readonly record struct DeviceKey(JoyConSide Side, string Id);

public enum MouseButton { Left, Right, Middle }

public enum WheelAxis { Vertical, Horizontal }

public enum MouseAction
{
    None, LeftClick, RightClick, MiddleClick,
    ScrollUp, ScrollDown, ScrollLeft, ScrollRight,
    TogglePause,
}

public abstract record MouseCommand
{
    private MouseCommand() { }

    public sealed record Move(int Dx, int Dy) : MouseCommand;

    public sealed record Button(MouseButton Which, bool Down) : MouseCommand;

    /// <summary>세로: 위 +, 가로: 오른쪽 +.</summary>
    public sealed record Wheel(WheelAxis Axis, int Delta) : MouseCommand;
}

public interface IMouseSink
{
    void Send(IReadOnlyList<MouseCommand> commands);
}
