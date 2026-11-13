using JoyConMouse.Core.Buttons;
using JoyConMouse.Core.Model;

namespace JoyConMouse.Core.Tests;

public class ClickMapperTests
{
    [Fact]
    public void 같은_클릭에_지정된_두_버튼은_둘_다_떼야_Up이_나온다()
    {
        var map = new ButtonMap(new Dictionary<JoyConButtons, MouseAction>
        {
            [JoyConButtons.ZL] = MouseAction.LeftClick,
            [JoyConButtons.SL] = MouseAction.LeftClick,
        });
        var c = new ClickMapper();
        var output = new List<MouseCommand>();

        c.Update(JoyConButtons.None, JoyConButtons.ZL, map, output);
        c.Update(JoyConButtons.ZL, JoyConButtons.ZL | JoyConButtons.SL, map, output);
        c.Update(JoyConButtons.ZL | JoyConButtons.SL, JoyConButtons.SL, map, output);
        Assert.Equal([new MouseCommand.Button(MouseButton.Left, true)], output);

        c.Update(JoyConButtons.SL, JoyConButtons.None, map, output);
        Assert.Equal(
            [new MouseCommand.Button(MouseButton.Left, true), new MouseCommand.Button(MouseButton.Left, false)],
            output);
    }

    [Fact]
    public void ReleaseAll은_앱이_누른_버튼만_Up한다()
    {
        var c = new ClickMapper();
        var output = new List<MouseCommand>();
        c.Update(JoyConButtons.None, JoyConButtons.ZL, DefaultLayouts.Left, output);

        output.Clear();
        c.ReleaseAll(output);
        Assert.Equal([new MouseCommand.Button(MouseButton.Left, false)], output);

        // 두 번째 호출은 아무것도 내지 않는다.
        output.Clear();
        c.ReleaseAll(output);
        Assert.Empty(output);
    }
}
