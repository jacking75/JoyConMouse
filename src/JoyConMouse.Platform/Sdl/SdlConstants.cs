namespace JoyConMouse.Platform.Sdl;

// 모든 값은 SDL 3.4.20 헤더(include/SDL3/SDL_init.h, SDL_events.h, SDL_gamepad.h, SDL_hints.h,
// SDL_power.h, SDL_joystick.h)에서 옮겼다. SDL 버전을 바꾸면 다시 대조한다.

public static class SdlInit
{
    public const uint Joystick = 0x00000200u; // SDL_INIT_JOYSTICK
    public const uint Gamepad = 0x00002000u;  // SDL_INIT_GAMEPAD (JOYSTICK 포함)
    public const uint Events = 0x00004000u;   // SDL_INIT_EVENTS
}

public static class SdlEventType
{
    public const uint Quit = 0x100;

    public const uint JoystickAxisMotion = 0x600;
    public const uint JoystickButtonDown = 0x603;
    public const uint JoystickButtonUp = 0x604;
    public const uint JoystickAdded = 0x605;
    public const uint JoystickRemoved = 0x606;
    public const uint JoystickBatteryUpdated = 0x607;

    public const uint GamepadAxisMotion = 0x650;
    public const uint GamepadButtonDown = 0x651;
    public const uint GamepadButtonUp = 0x652;
    public const uint GamepadAdded = 0x653;
    public const uint GamepadRemoved = 0x654;
    public const uint GamepadRemapped = 0x655;
}

public static class SdlHints
{
    public const string JoystickHidapiJoyCons = "SDL_JOYSTICK_HIDAPI_JOY_CONS";
    public const string JoystickHidapiCombineJoyCons = "SDL_JOYSTICK_HIDAPI_COMBINE_JOY_CONS";
    public const string JoystickAllowBackgroundEvents = "SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS";
    public const string JoystickHidapiVerticalJoyCons = "SDL_JOYSTICK_HIDAPI_VERTICAL_JOY_CONS"; // SDL 3.2.0+
}

/// <summary>SDL_GamepadType</summary>
public enum SdlGamepadType
{
    Unknown = 0,
    Standard,
    Xbox360,
    XboxOne,
    Ps3,
    Ps4,
    Ps5,
    NintendoSwitchPro,
    NintendoSwitchJoyConLeft,
    NintendoSwitchJoyConRight,
    NintendoSwitchJoyConPair,
    GameCube,
    Steam,
}

/// <summary>SDL_GamepadButton</summary>
public enum SdlGamepadButton
{
    Invalid = -1,
    South = 0,
    East,
    West,
    North,
    Back,
    Guide,
    Start,
    LeftStick,
    RightStick,
    LeftShoulder,
    RightShoulder,
    DpadUp,
    DpadDown,
    DpadLeft,
    DpadRight,
    Misc1,
    RightPaddle1,
    LeftPaddle1,
    RightPaddle2,
    LeftPaddle2,
    Touchpad,
    Misc2,
    Misc3,
    Misc4,
    Misc5,
    Misc6,
    Count,
}

/// <summary>SDL_GamepadAxis</summary>
public enum SdlGamepadAxis
{
    Invalid = -1,
    LeftX = 0,
    LeftY,
    RightX,
    RightY,
    LeftTrigger,
    RightTrigger,
    Count,
}

/// <summary>SDL_PowerState</summary>
public enum SdlPowerState
{
    Error = -1,
    Unknown = 0,
    OnBattery,
    NoBattery,
    Charging,
    Charged,
}

/// <summary>SDL_JoystickConnectionState</summary>
public enum SdlConnectionState
{
    Invalid = -1,
    Unknown = 0,
    Wired,
    Wireless,
}
