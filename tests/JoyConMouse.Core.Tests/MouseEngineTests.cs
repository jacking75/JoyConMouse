using JoyConMouse.Core.Engine;
using JoyConMouse.Core.Model;
using static JoyConMouse.Core.Tests.EngineTestHelpers;

namespace JoyConMouse.Core.Tests;

public class MouseEngineTests
{
    [Fact]
    public void 활성_장치_변경_직후_버튼이_눌린_채면_Active로_가지_않는다()
    {
        var e = new MouseEngine(1000);
        var output = new List<MouseCommand>();
        e.OnDeviceConnected(LeftA, 0);
        e.SetActiveDevice(LeftA, 0, output);

        for (long t = 0; t <= 500; t += 4)
        {
            e.Update(Press(JoyConButtons.ZL), t, output);
        }

        Assert.Equal(EngineState.WaitingNeutral, e.State);
        Assert.Empty(output);

        // 놓은 뒤 중립 유지 시간이 지나야 Active.
        e.Update(Neutral, 600, output);
        e.Update(Neutral, 600 + MouseEngine.NeutralHoldMs - 1, output);
        Assert.Equal(EngineState.WaitingNeutral, e.State);
        e.Update(Neutral, 600 + MouseEngine.NeutralHoldMs, output);
        Assert.Equal(EngineState.Active, e.State);
        Assert.Empty(output);
    }

    [Fact]
    public void 클릭을_누른_채_활성_장치를_바꾸면_Up이_정확히_한_번_나온다()
    {
        var e = new MouseEngine(1000);
        long t = Activate(e, LeftA);
        e.OnDeviceConnected(RightA, t);
        var output = new List<MouseCommand>();

        e.Update(Press(JoyConButtons.ZL), t += 4, output);
        Assert.Equal([new MouseCommand.Button(MouseButton.Left, true)], output);

        output.Clear();
        e.SetActiveDevice(RightA, t, output);
        // 새 활성 장치의 입력이 이어져도 추가 Up 은 없다.
        e.Update(Press(JoyConButtons.ZR), t += 4, output);
        e.Update(Neutral, t += 4, output);

        Assert.Equal([new MouseCommand.Button(MouseButton.Left, false)], output);
    }

    [Fact]
    public void 끊긴_뒤_다른_장치가_연결되어도_활성화되지_않는다()
    {
        var e = new MouseEngine(1000);
        long t = Activate(e, LeftA);
        var output = new List<MouseCommand>();

        e.OnDeviceDisconnected(LeftA, t, output);
        e.OnDeviceConnected(RightA, t + 10);
        e.Update(null, t + 20, output);

        Assert.Equal(EngineState.Disconnected, e.State);
        Assert.Equal(LeftA, e.ActiveDevice);

        // 같은 장치가 돌아오면 중립 대기부터.
        e.OnDeviceConnected(LeftA, t + 30);
        Assert.Equal(EngineState.WaitingNeutral, e.State);
    }

    public enum Interruption { UserPause, Disconnect, SwitchDevice, Suspend }

    [Theory]
    [InlineData(Interruption.UserPause)]
    [InlineData(Interruption.Disconnect)]
    [InlineData(Interruption.SwitchDevice)]
    [InlineData(Interruption.Suspend)]
    public void 드래그와_반복_스크롤_중_중단되면_Up_한_번_후_아무것도_나오지_않는다(Interruption what)
    {
        var e = new MouseEngine(1000);
        long t = Activate(e, LeftA);
        e.OnDeviceConnected(RightA, t);
        var output = new List<MouseCommand>();
        var held = Press(JoyConButtons.ZL | JoyConButtons.DpadUp);

        for (int i = 0; i < 100; i++)
        {
            e.Update(held, t += 4, output); // 드래그 + 반복 스크롤 진행 중
        }

        Assert.Contains(new MouseCommand.Button(MouseButton.Left, true), output);
        Assert.Contains(output, c => c is MouseCommand.Wheel);

        output.Clear();
        switch (what)
        {
            case Interruption.UserPause: e.SetPause(PauseReason.User, true, t, output); break;
            case Interruption.Disconnect: e.OnDeviceDisconnected(LeftA, t, output); break;
            case Interruption.SwitchDevice: e.SetActiveDevice(RightA, t, output); break;
            case Interruption.Suspend: e.SetPause(PauseReason.Suspend, true, t, output); break;
        }

        // 이후 버튼을 계속 누르고 있어도(끊김이면 입력 없음) 추가 명령이 없어야 한다.
        for (int i = 0; i < 500; i++)
        {
            e.Update(what == Interruption.Disconnect ? null : held, t += 4, output);
        }

        Assert.Equal([new MouseCommand.Button(MouseButton.Left, false)], output);
    }

    [Fact]
    public void DeviceSelector_규칙()
    {
        var leftB = new DeviceKey(JoyConSide.Left, "cccc");

        // 1. 저장된 장치가 연결되어 있으면 그것.
        Assert.Equal((SelectionOutcome.Selected, (DeviceKey?)RightA), DeviceSelector.Select(RightA, [LeftA, RightA]));
        // 2. 저장값이 없고 1대뿐이면 그것.
        Assert.Equal((SelectionOutcome.Selected, (DeviceKey?)LeftA), DeviceSelector.Select(null, [LeftA]));
        // 3. 저장값 없음 + 여러 대, 또는 저장값 불일치 → 사용자 선택.
        Assert.Equal((SelectionOutcome.NeedsChoice, (DeviceKey?)null), DeviceSelector.Select(null, [LeftA, RightA]));
        Assert.Equal((SelectionOutcome.NeedsChoice, (DeviceKey?)null), DeviceSelector.Select(LeftA, [leftB]));
    }
}
