using JoyConMouse.Core.Model;
using JoyConMouse.Core.Motion;
using JoyConMouse.Core.Settings;

namespace JoyConMouse.Core.Tests;

public class SettingsSerializerTests
{
    [Fact]
    public void 왼쪽만_바꿔_저장해도_오른쪽은_기본값_그대로다()
    {
        var left = JoyConProfile.DefaultFor(JoyConSide.Left);
        left = left with
        {
            Stick = left.Stick with { MaxSpeed = 2000 },
            Buttons = left.Buttons.With(JoyConButtons.SL, MouseAction.ScrollUp),
        };
        var saved = AppSettings.Default with { Left = left, LastActiveDevice = new DeviceKey(JoyConSide.Left, "3f9a0c12e4b7d655") };

        var loaded = SettingsSerializer.Deserialize(SettingsSerializer.Serialize(saved));

        Assert.Equal(SettingsLoadStatus.Ok, loaded.Status);
        Assert.Equal(2000, loaded.Settings.Left.Stick.MaxSpeed);
        Assert.Equal(MouseAction.ScrollUp, loaded.Settings.Left.Buttons[JoyConButtons.SL]);
        Assert.Equal(saved.LastActiveDevice, loaded.Settings.LastActiveDevice);

        var defRight = JoyConProfile.DefaultFor(JoyConSide.Right);
        Assert.Equal(defRight.Stick, loaded.Settings.Right.Stick);
        Assert.Equal(defRight.Scroll, loaded.Settings.Right.Scroll);
        Assert.True(defRight.Buttons.SameAs(loaded.Settings.Right.Buttons));
    }

    [Fact]
    public void 깨진_JSON은_기본값과_손상_표시를_돌려준다()
    {
        var r = SettingsSerializer.Deserialize("{ \"version\": 1, \"profiles\": { ");
        Assert.Equal(SettingsLoadStatus.Corrupt, r.Status);
        Assert.Same(AppSettings.Default, r.Settings);
    }

    [Fact]
    public void 범위_밖_값은_범위로_잘린다()
    {
        const string json = """
            { "version": 1, "profiles": { "Left": {
                "stick": { "maxSpeed": 99999, "deadzone": 0.0, "curveExponent": 10 },
                "scroll": { "stepDelta": 1, "initialDelayMs": 5000, "repeatIntervalMs": 1 } } } }
            """;
        var p = SettingsSerializer.Deserialize(json).Settings.Left;

        Assert.Equal(StickSettings.MaxSpeedMax, p.Stick.MaxSpeed);
        Assert.Equal(StickSettings.DeadzoneMin, p.Stick.Deadzone);
        Assert.Equal(StickSettings.CurveMax, p.Stick.CurveExponent);
        Assert.Equal(30, p.Scroll.StepDelta);
        Assert.Equal(1000, p.Scroll.InitialDelayMs);
        Assert.Equal(30, p.Scroll.RepeatIntervalMs);
    }
}
