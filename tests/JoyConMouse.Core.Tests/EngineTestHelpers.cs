using JoyConMouse.Core.Engine;
using JoyConMouse.Core.Model;

namespace JoyConMouse.Core.Tests;

/// <summary>시간 단위는 ms(ticksPerSecond = 1000).</summary>
internal static class EngineTestHelpers
{
    public static readonly DeviceKey LeftA = new(JoyConSide.Left, "aaaa");
    public static readonly DeviceKey RightA = new(JoyConSide.Right, "bbbb");

    public static readonly JoyConSnapshot Neutral = new(0, 0, JoyConButtons.None);

    public static JoyConSnapshot Press(JoyConButtons b) => new(0, 0, b);

    /// <summary>장치를 연결·선택하고 중립을 유지해 Active 로 만든다. 끝난 시각을 돌려준다.</summary>
    public static long Activate(MouseEngine e, DeviceKey key, long now = 0)
    {
        var sink = new List<MouseCommand>();
        e.OnDeviceConnected(key, now);
        e.SetActiveDevice(key, now, sink);
        e.Update(Neutral, now, sink);
        now += MouseEngine.NeutralHoldMs;
        e.Update(Neutral, now, sink);
        Assert.Equal(EngineState.Active, e.State);
        return now;
    }
}
