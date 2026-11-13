using JoyConMouse.Core.Buttons;
using JoyConMouse.Core.Model;
using JoyConMouse.Core.Motion;
using JoyConMouse.Core.Scroll;

namespace JoyConMouse.Core.Settings;

/// <summary>측면(왼쪽·오른쪽)별 설정. 장치가 아니라 측면 단위로 적용된다.</summary>
public sealed record JoyConProfile(StickSettings Stick, ScrollSettings Scroll, ButtonMap Buttons)
{
    public static JoyConProfile DefaultFor(JoyConSide side) =>
        new(StickSettings.Default, ScrollSettings.Default, DefaultLayouts.For(side));

    public JoyConProfile Clamped() => this with { Stick = Stick.Clamped(), Scroll = Scroll.Clamped() };
}
