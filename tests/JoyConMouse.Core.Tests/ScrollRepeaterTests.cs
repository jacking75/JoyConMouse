using JoyConMouse.Core.Model;
using JoyConMouse.Core.Scroll;

namespace JoyConMouse.Core.Tests;

public class ScrollRepeaterTests
{
    [Fact]
    public void 길게_누르면_즉시_1회_후_초기_지연_뒤_반복한다()
    {
        var r = new ScrollRepeater(1000);
        var s = ScrollSettings.Default; // 지연 300ms, 간격 100ms
        var fired = new List<long>();
        var output = new List<MouseCommand>();

        for (long t = 0; t <= 550; t++)
        {
            output.Clear();
            r.Update(up: true, down: false, left: false, right: false, t, s, output);
            if (output.Count > 0)
            {
                Assert.Equal([new MouseCommand.Wheel(WheelAxis.Vertical, s.StepDelta)], output);
                fired.Add(t);
            }
        }

        Assert.Equal([0L, 300L, 400L, 500L], fired);
    }

    [Fact]
    public void 같은_축_반대_방향은_멈추고_다른_축은_독립적이다()
    {
        var r = new ScrollRepeater(1000);
        var s = ScrollSettings.Default;
        var output = new List<MouseCommand>();

        for (long t = 0; t <= 1000; t += 4)
        {
            r.Update(up: true, down: true, left: false, right: false, t, s, output);
        }

        Assert.Empty(output);

        var r2 = new ScrollRepeater(1000);
        r2.Update(up: false, down: true, left: true, right: false, 0, s, output);
        Assert.Equal(
            [new MouseCommand.Wheel(WheelAxis.Vertical, -s.StepDelta), new MouseCommand.Wheel(WheelAxis.Horizontal, -s.StepDelta)],
            output);
    }
}
