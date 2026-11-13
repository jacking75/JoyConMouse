namespace JoyConMouse.Platform.Sdl;

/// <summary>
/// SDL 힌트 설정과 초기화·종료. SDL 을 사용하는 스레드에서 만들고 같은 스레드에서 Dispose 한다.
/// </summary>
public sealed class SdlContext : IDisposable
{
    private bool _initialized;

    private SdlContext() { }

    public bool VerticalJoyCons { get; private init; }

    /// <summary>힌트를 설정한 뒤 SDL_Init(GAMEPAD) 를 호출한다. 실패하면 SDL_GetError 내용으로 예외.</summary>
    public static SdlContext Initialize(bool verticalJoyCons = true)
    {
        // 힌트는 반드시 SDL_Init 전에 설정한다.
        SdlNative.SDL_SetHint(SdlHints.JoystickHidapiJoyCons, "1");
        SdlNative.SDL_SetHint(SdlHints.JoystickHidapiCombineJoyCons, "0");
        SdlNative.SDL_SetHint(SdlHints.JoystickAllowBackgroundEvents, "1");
        SdlNative.SDL_SetHint(SdlHints.JoystickHidapiVerticalJoyCons, verticalJoyCons ? "1" : "0");
        // 센서는 SDL_SetGamepadSensorEnabled 를 호출하지 않으므로 꺼진 상태로 남는다.

        if (!SdlNative.SDL_Init(SdlInit.Gamepad))
        {
            throw new SdlException("SDL_Init 실패: " + SdlNative.GetError());
        }

        return new SdlContext { _initialized = true, VerticalJoyCons = verticalJoyCons };
    }

    public void Dispose()
    {
        if (_initialized)
        {
            _initialized = false;
            SdlNative.SDL_Quit();
        }
    }
}

public sealed class SdlException(string message) : Exception(message);
