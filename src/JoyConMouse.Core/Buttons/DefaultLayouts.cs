using JoyConMouse.Core.Model;

namespace JoyConMouse.Core.Buttons;

/// <summary>기획 4장 기본 배치와 좌우 미러 표.</summary>
public static class DefaultLayouts
{
    public static ButtonMap Left { get; } = new(new Dictionary<JoyConButtons, MouseAction>
    {
        [JoyConButtons.ZL] = MouseAction.LeftClick,
        [JoyConButtons.L] = MouseAction.RightClick,
        [JoyConButtons.DpadUp] = MouseAction.ScrollUp,
        [JoyConButtons.DpadDown] = MouseAction.ScrollDown,
        [JoyConButtons.DpadLeft] = MouseAction.ScrollLeft,
        [JoyConButtons.DpadRight] = MouseAction.ScrollRight,
        [JoyConButtons.LeftStick] = MouseAction.MiddleClick,
        [JoyConButtons.Minus] = MouseAction.TogglePause,
    });

    public static ButtonMap Right { get; } = new(new Dictionary<JoyConButtons, MouseAction>
    {
        [JoyConButtons.ZR] = MouseAction.LeftClick,
        [JoyConButtons.R] = MouseAction.RightClick,
        [JoyConButtons.X] = MouseAction.ScrollUp,
        [JoyConButtons.B] = MouseAction.ScrollDown,
        [JoyConButtons.Y] = MouseAction.ScrollLeft,
        [JoyConButtons.A] = MouseAction.ScrollRight,
        [JoyConButtons.RightStick] = MouseAction.MiddleClick,
        [JoyConButtons.Plus] = MouseAction.TogglePause,
    });

    public static ButtonMap For(JoyConSide side) => side == JoyConSide.Left ? Left : Right;

    private static readonly (JoyConButtons Left, JoyConButtons Right)[] MirrorPairs =
    [
        (JoyConButtons.ZL, JoyConButtons.ZR),
        (JoyConButtons.L, JoyConButtons.R),
        (JoyConButtons.DpadUp, JoyConButtons.X),
        (JoyConButtons.DpadDown, JoyConButtons.B),
        (JoyConButtons.DpadLeft, JoyConButtons.Y),
        (JoyConButtons.DpadRight, JoyConButtons.A),
        (JoyConButtons.Minus, JoyConButtons.Plus),
        (JoyConButtons.LeftStick, JoyConButtons.RightStick),
        (JoyConButtons.Capture, JoyConButtons.Home),
        (JoyConButtons.SL, JoyConButtons.SL),
        (JoyConButtons.SR, JoyConButtons.SR),
    ];

    /// <summary>반대쪽 배치를 미러 표로 옮겨 <paramref name="target"/> 측면용 배치를 만든다.</summary>
    public static ButtonMap MirrorFrom(ButtonMap source, JoyConSide target)
    {
        var result = new Dictionary<JoyConButtons, MouseAction>();
        foreach (var (l, r) in MirrorPairs)
        {
            var (from, to) = target == JoyConSide.Left ? (r, l) : (l, r);
            result[to] = source[from];
        }

        return new ButtonMap(result);
    }
}
