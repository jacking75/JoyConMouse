namespace JoyConMouse.Core.Model;

/// <summary>측면별 물리 버튼 목록과 표시 이름.</summary>
public static class JoyConButtonInfo
{
    public static IReadOnlyList<JoyConButtons> LeftButtons { get; } =
    [
        JoyConButtons.ZL, JoyConButtons.L, JoyConButtons.Minus, JoyConButtons.Capture, JoyConButtons.LeftStick,
        JoyConButtons.DpadUp, JoyConButtons.DpadDown, JoyConButtons.DpadLeft, JoyConButtons.DpadRight,
        JoyConButtons.SL, JoyConButtons.SR,
    ];

    public static IReadOnlyList<JoyConButtons> RightButtons { get; } =
    [
        JoyConButtons.ZR, JoyConButtons.R, JoyConButtons.Plus, JoyConButtons.Home, JoyConButtons.RightStick,
        JoyConButtons.X, JoyConButtons.B, JoyConButtons.Y, JoyConButtons.A,
        JoyConButtons.SL, JoyConButtons.SR,
    ];

    public static IReadOnlyList<JoyConButtons> ButtonsOf(JoyConSide side) =>
        side == JoyConSide.Left ? LeftButtons : RightButtons;

    public static string DisplayName(JoyConButtons button) => button switch
    {
        JoyConButtons.ZL => "ZL",
        JoyConButtons.L => "L",
        JoyConButtons.Minus => "−",
        JoyConButtons.Capture => "캡처",
        JoyConButtons.LeftStick => "스틱 누르기",
        JoyConButtons.DpadUp => "↑",
        JoyConButtons.DpadDown => "↓",
        JoyConButtons.DpadLeft => "←",
        JoyConButtons.DpadRight => "→",
        JoyConButtons.ZR => "ZR",
        JoyConButtons.R => "R",
        JoyConButtons.Plus => "+",
        JoyConButtons.Home => "홈",
        JoyConButtons.RightStick => "스틱 누르기",
        JoyConButtons.X => "X",
        JoyConButtons.B => "B",
        JoyConButtons.Y => "Y",
        JoyConButtons.A => "A",
        JoyConButtons.SL => "SL",
        JoyConButtons.SR => "SR",
        _ => button.ToString(),
    };

    public static string SideName(JoyConSide side) => side == JoyConSide.Left ? "조이콘(L)" : "조이콘(R)";
}
