using JoyConMouse.Core.Motion;

namespace JoyConMouse.Core.Tests;

public class StickMotionTests
{
    private static (int X, int Y) Run(StickMotion m, float x, float y, double dt, int frames, StickSettings s)
    {
        int sx = 0, sy = 0;
        for (int i = 0; i < frames; i++)
        {
            var (dx, dy) = m.Step(x, y, dt, s);
            sx += dx;
            sy += dy;
        }

        return (sx, sy);
    }

    [Fact]
    public void 데드존_안_입력은_오래_누적해도_움직이지_않는다()
    {
        var s = StickSettings.Default;
        var total = Run(new StickMotion(), (float)(s.Deadzone * 0.9), 0, 0.004, 2500, s);
        Assert.Equal((0, 0), total);
    }

    [Fact]
    public void 대각선과_수평의_이동_거리가_같다()
    {
        var s = StickSettings.Default;
        const float mag = 0.8f;
        float d = mag / MathF.Sqrt(2);
        var h = Run(new StickMotion(), mag, 0, 0.004, 250, s);
        var diag = Run(new StickMotion(), d, d, 0.004, 250, s);

        double lenH = Math.Sqrt(h.X * h.X + h.Y * h.Y);
        double lenD = Math.Sqrt(diag.X * diag.X + diag.Y * diag.Y);
        Assert.True(lenH > 50);
        Assert.InRange(lenD, lenH - 1.5, lenH + 1.5); // 각 축 절삭 오차 1씩
    }

    [Fact]
    public void 프레임당_1보다_작은_움직임도_누적되어_사라지지_않는다()
    {
        // 최대 기울기·곡선 1 → 초당 250, dt 1ms → 프레임당 0.25
        var s = new StickSettings { MaxSpeed = 250, CurveExponent = 1, Deadzone = 0.15 };
        var total = Run(new StickMotion(), 1, 0, 0.001, 12, s);
        Assert.Equal((3, 0), total);
    }

    [Fact]
    public void 긴_틱_간격_뒤에는_움직이지_않고_이전_잔여량도_버린다()
    {
        var s = new StickSettings { MaxSpeed = 250, CurveExponent = 1, Deadzone = 0.15 };
        var m = new StickMotion();
        Assert.Equal((0, 0), Run(m, 1, 0, 0.001, 3, s)); // 잔여 0.75

        Assert.Equal((0, 0), m.Step(1, 0, StickSettings.MaxGapSeconds + 0.05, s));
        // 잔여가 남아 있었다면 0.75 + 0.25 = 1 로 1 이 나온다.
        Assert.Equal((0, 0), m.Step(1, 0, 0.001, s));
    }
}
