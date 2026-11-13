namespace JoyConMouse.Platform.Sdl;

/// <summary>SDL_Event 에서 필요한 필드만 꺼낸 값. Which 는 장치 이벤트(ADDED/REMOVED 등)에서만 의미가 있다.</summary>
public readonly record struct SdlEventInfo(uint Type, uint Which);

public static unsafe class SdlEvents
{
    /// <summary>SDL_PollEvent 한 번. 이벤트가 없으면 false.</summary>
    public static bool Poll(out SdlEventInfo info)
    {
        byte* buf = stackalloc byte[SdlNative.EventSize];
        if (!SdlNative.SDL_PollEvent(buf))
        {
            info = default;
            return false;
        }

        uint type = *(uint*)buf;
        uint which = *(uint*)(buf + SdlNative.EventWhichOffset);
        info = new SdlEventInfo(type, which);
        return true;
    }
}
