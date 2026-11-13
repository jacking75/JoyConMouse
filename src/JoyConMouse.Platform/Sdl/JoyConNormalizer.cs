using JoyConMouse.Core.Model;

namespace JoyConMouse.Platform.Sdl;

/// <summary>
/// SDL 게임패드 상태를 세로 기준 <see cref="JoyConSnapshot"/> 으로 바꾼다.
/// 표의 값은 SDL 3.4.20 소스(src/joystick/hidapi/SDL_hidapi_switch.c, src/joystick/SDL_gamepad.c)의
/// 단독 조이콘 매핑을 옮긴 것이다. 세로 모드 표는 실기기로 확정했다(문서 4번, 2026-11-13: 좌우 버튼 11/11, 스틱 4방향 일치).
/// 가로 모드 표는 실측하지 않은 대체용이다.
/// </summary>
public static class JoyConNormalizer
{
    /// <summary>트리거 축(ZL·ZR)이 이 값을 넘으면 눌림. SDL 은 트리거를 0~32767 로 준다.</summary>
    private const short TriggerThreshold = 16384;

    private readonly record struct Entry(SdlGamepadButton Button, SdlGamepadAxis TriggerAxis, JoyConButtons Physical)
    {
        public static Entry Btn(SdlGamepadButton b, JoyConButtons p) => new(b, SdlGamepadAxis.Invalid, p);
        public static Entry Trig(SdlGamepadAxis a, JoyConButtons p) => new(SdlGamepadButton.Invalid, a, p);
    }

    // 세로 모드(SDL_JOYSTICK_HIDAPI_VERTICAL_JOY_CONS=1). 조합 모드와 같은 배치를 쓴다.
    private static readonly Entry[] LeftVertical =
    [
        Entry.Trig(SdlGamepadAxis.LeftTrigger, JoyConButtons.ZL),
        Entry.Btn(SdlGamepadButton.LeftShoulder, JoyConButtons.L),
        Entry.Btn(SdlGamepadButton.Back, JoyConButtons.Minus),
        Entry.Btn(SdlGamepadButton.Misc1, JoyConButtons.Capture),
        Entry.Btn(SdlGamepadButton.LeftStick, JoyConButtons.LeftStick),
        Entry.Btn(SdlGamepadButton.DpadUp, JoyConButtons.DpadUp),
        Entry.Btn(SdlGamepadButton.DpadDown, JoyConButtons.DpadDown),
        Entry.Btn(SdlGamepadButton.DpadLeft, JoyConButtons.DpadLeft),
        Entry.Btn(SdlGamepadButton.DpadRight, JoyConButtons.DpadRight),
        Entry.Btn(SdlGamepadButton.LeftPaddle1, JoyConButtons.SL),
        Entry.Btn(SdlGamepadButton.LeftPaddle2, JoyConButtons.SR),
    ];

    private static readonly Entry[] RightVertical =
    [
        Entry.Trig(SdlGamepadAxis.RightTrigger, JoyConButtons.ZR),
        Entry.Btn(SdlGamepadButton.RightShoulder, JoyConButtons.R),
        Entry.Btn(SdlGamepadButton.Start, JoyConButtons.Plus),
        Entry.Btn(SdlGamepadButton.Guide, JoyConButtons.Home),
        Entry.Btn(SdlGamepadButton.RightStick, JoyConButtons.RightStick),
        Entry.Btn(SdlGamepadButton.North, JoyConButtons.X),
        Entry.Btn(SdlGamepadButton.South, JoyConButtons.B),
        Entry.Btn(SdlGamepadButton.West, JoyConButtons.Y),
        Entry.Btn(SdlGamepadButton.East, JoyConButtons.A),
        Entry.Btn(SdlGamepadButton.RightPaddle2, JoyConButtons.SL),
        Entry.Btn(SdlGamepadButton.RightPaddle1, JoyConButtons.SR),
    ];

    // 가로 미니 게임패드 모드(힌트 0). 얼굴 버튼이 90° 회전되어 있다.
    private static readonly Entry[] LeftHorizontal =
    [
        Entry.Btn(SdlGamepadButton.LeftPaddle2, JoyConButtons.ZL),
        Entry.Btn(SdlGamepadButton.LeftPaddle1, JoyConButtons.L),
        Entry.Btn(SdlGamepadButton.Start, JoyConButtons.Minus),
        Entry.Btn(SdlGamepadButton.Guide, JoyConButtons.Capture),
        Entry.Btn(SdlGamepadButton.LeftStick, JoyConButtons.LeftStick),
        Entry.Btn(SdlGamepadButton.West, JoyConButtons.DpadUp),
        Entry.Btn(SdlGamepadButton.East, JoyConButtons.DpadDown),
        Entry.Btn(SdlGamepadButton.South, JoyConButtons.DpadLeft),
        Entry.Btn(SdlGamepadButton.North, JoyConButtons.DpadRight),
        Entry.Btn(SdlGamepadButton.LeftShoulder, JoyConButtons.SL),
        Entry.Btn(SdlGamepadButton.RightShoulder, JoyConButtons.SR),
    ];

    private static readonly Entry[] RightHorizontal =
    [
        Entry.Btn(SdlGamepadButton.RightPaddle2, JoyConButtons.ZR),
        Entry.Btn(SdlGamepadButton.RightPaddle1, JoyConButtons.R),
        Entry.Btn(SdlGamepadButton.Start, JoyConButtons.Plus),
        Entry.Btn(SdlGamepadButton.Guide, JoyConButtons.Home),
        Entry.Btn(SdlGamepadButton.LeftStick, JoyConButtons.RightStick),
        Entry.Btn(SdlGamepadButton.East, JoyConButtons.X),
        Entry.Btn(SdlGamepadButton.West, JoyConButtons.B),
        Entry.Btn(SdlGamepadButton.North, JoyConButtons.Y),
        Entry.Btn(SdlGamepadButton.South, JoyConButtons.A),
        Entry.Btn(SdlGamepadButton.LeftShoulder, JoyConButtons.SL),
        Entry.Btn(SdlGamepadButton.RightShoulder, JoyConButtons.SR),
    ];

    public static JoyConSnapshot Read(SdlGamepad pad, JoyConSide side, bool vertical)
    {
        Entry[] map = side == JoyConSide.Left
            ? (vertical ? LeftVertical : LeftHorizontal)
            : (vertical ? RightVertical : RightHorizontal);

        JoyConButtons pressed = JoyConButtons.None;
        foreach (var e in map)
        {
            bool down = e.Button != SdlGamepadButton.Invalid
                ? pad.GetButton(e.Button)
                : pad.GetAxis(e.TriggerAxis) > TriggerThreshold;
            if (down)
            {
                pressed |= e.Physical;
            }
        }

        float x, y;
        if (vertical)
        {
            // 세로 모드: 왼쪽은 LEFT 스틱, 오른쪽은 RIGHT 스틱. SDL 축은 오른쪽 +, 아래 + 이다.
            (x, y) = side == JoyConSide.Left
                ? (Axis(pad.GetAxis(SdlGamepadAxis.LeftX)), Axis(pad.GetAxis(SdlGamepadAxis.LeftY)))
                : (Axis(pad.GetAxis(SdlGamepadAxis.RightX)), Axis(pad.GetAxis(SdlGamepadAxis.RightY)));
        }
        else
        {
            // 가로 모드: 미니 게임패드의 LEFT 스틱이 90° 회전되어 있다.
            float hx = Axis(pad.GetAxis(SdlGamepadAxis.LeftX));
            float hy = Axis(pad.GetAxis(SdlGamepadAxis.LeftY));
            (x, y) = side == JoyConSide.Left ? (-hy, hx) : (hy, -hx);
        }

        return new JoyConSnapshot(x, y, pressed);
    }

    /// <summary>원시 short 값을 [-1, 1] 로.</summary>
    public static float Axis(short raw) => Math.Clamp(raw / 32767f, -1f, 1f);
}
