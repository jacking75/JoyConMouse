using System.Runtime.InteropServices;

namespace JoyConMouse.Platform.Sdl;

/// <summary>
/// SDL3 최소 P/Invoke. 시그니처는 SDL 3.4.20 헤더 기준이다.
/// SDL3 는 성공 여부를 C bool(1바이트)로 돌려준다. SDL 이 소유한 문자열(const char*)은 해제하지 않는다.
/// </summary>
public static unsafe partial class SdlNative
{
    private const string Lib = "SDL3";

    /// <summary>SDL_Event 공용체 크기(padding[128]).</summary>
    public const int EventSize = 128;

    /// <summary>SDL_GamepadDeviceEvent·SDL_JoyDeviceEvent 의 which 오프셋: type(4) + reserved(4) + timestamp(8).</summary>
    public const int EventWhichOffset = 16;

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_SetHint(string name, string value);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_Init(uint flags);

    [LibraryImport(Lib)]
    public static partial void SDL_Quit();

    [LibraryImport(Lib)]
    public static partial nint SDL_GetError();

    [LibraryImport(Lib)]
    public static partial void SDL_free(void* mem);

    [LibraryImport(Lib)]
    public static partial int SDL_GetVersion();

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_PollEvent(byte* ev);

    [LibraryImport(Lib)]
    public static partial uint* SDL_GetGamepads(int* count);

    [LibraryImport(Lib)]
    public static partial nint SDL_OpenGamepad(uint instanceId);

    [LibraryImport(Lib)]
    public static partial void SDL_CloseGamepad(nint gamepad);

    [LibraryImport(Lib)]
    public static partial uint SDL_GetGamepadID(nint gamepad);

    [LibraryImport(Lib)]
    public static partial SdlGamepadType SDL_GetGamepadType(nint gamepad);

    [LibraryImport(Lib)]
    public static partial nint SDL_GetGamepadName(nint gamepad);

    [LibraryImport(Lib)]
    public static partial nint SDL_GetGamepadSerial(nint gamepad);

    [LibraryImport(Lib)]
    public static partial nint SDL_GetGamepadPath(nint gamepad);

    [LibraryImport(Lib)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool SDL_GetGamepadButton(nint gamepad, SdlGamepadButton button);

    [LibraryImport(Lib)]
    public static partial short SDL_GetGamepadAxis(nint gamepad, SdlGamepadAxis axis);

    [LibraryImport(Lib)]
    public static partial SdlPowerState SDL_GetGamepadPowerInfo(nint gamepad, int* percent);

    [LibraryImport(Lib)]
    public static partial SdlConnectionState SDL_GetGamepadConnectionState(nint gamepad);

    /// <summary>SDL 이 소유한 UTF-8 문자열을 복사한다. NULL 이면 null.</summary>
    public static string? PtrToString(nint p) => p == 0 ? null : Marshal.PtrToStringUTF8(p);

    public static string GetError() => PtrToString(SDL_GetError()) ?? string.Empty;

    /// <summary>SDL_GetGamepads 결과를 배열로 복사하고 SDL 메모리를 해제한다.</summary>
    public static uint[] GetGamepadIds()
    {
        int count = 0;
        uint* ids = SDL_GetGamepads(&count);
        if (ids == null)
        {
            return [];
        }

        try
        {
            return new ReadOnlySpan<uint>(ids, count).ToArray();
        }
        finally
        {
            SDL_free(ids);
        }
    }

    /// <summary>SDL_GetVersion 값(major*1000000 + minor*1000 + micro)을 "3.4.20" 형태로.</summary>
    public static string VersionString()
    {
        int v = SDL_GetVersion();
        return $"{v / 1000000}.{v / 1000 % 1000}.{v % 1000}";
    }
}
