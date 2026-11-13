using JoyConMouse.Core.Buttons;
using JoyConMouse.Core.Model;
using JoyConMouse.Core.Motion;
using JoyConMouse.Core.Scroll;
using JoyConMouse.Core.Settings;

namespace JoyConMouse.Core.Engine;

/// <summary>
/// 선택한 조이콘 한 대의 입력을 마우스 명령으로 바꾸는 상태 기계.
/// 입력 스레드 하나가 소유하며 잠금 없이 쓴다. 시간은 외부에서 tick 단위로 받는다.
/// </summary>
public sealed class MouseEngine
{
    /// <summary>중립이 이 시간 동안 유지되어야 Active 로 간다.</summary>
    public const int NeutralHoldMs = 50;

    private readonly long _ticksPerSecond;
    private readonly HashSet<DeviceKey> _connected = [];
    private readonly StickMotion _stick = new();
    private readonly ClickMapper _click = new();
    private readonly ScrollRepeater _scroll;

    private JoyConProfile _leftProfile = JoyConProfile.DefaultFor(JoyConSide.Left);
    private JoyConProfile _rightProfile = JoyConProfile.DefaultFor(JoyConSide.Right);
    private PauseReason _pause;
    private bool _neutralConfirmed;
    private long? _neutralSince;
    private long? _lastUpdate;
    private JoyConButtons _previous;

    public MouseEngine(long ticksPerSecond)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(ticksPerSecond, 0);
        _ticksPerSecond = ticksPerSecond;
        _scroll = new ScrollRepeater(ticksPerSecond);
    }

    public DeviceKey? ActiveDevice { get; private set; }

    public PauseReason Pause => _pause;

    public EngineState State =>
        ActiveDevice is not { } key ? EngineState.NoDevice
        : !_connected.Contains(key) ? EngineState.Disconnected
        : _pause != PauseReason.None ? EngineState.Paused
        : _neutralConfirmed ? EngineState.Active
        : EngineState.WaitingNeutral;

    public JoyConProfile ProfileFor(JoyConSide side) => side == JoyConSide.Left ? _leftProfile : _rightProfile;

    private JoyConProfile ActiveProfile => ProfileFor(ActiveDevice?.Side ?? JoyConSide.Left);

    /// <summary>
    /// 측면 프로필을 바꾼다. 활성 측면의 버튼 배치가 바뀌면 눌린 입력을 풀고 중립 대기로 간다
    /// (해제 엣지를 새 배치로 해석해 버튼이 눌린 채 남는 것을 막기 위함).
    /// </summary>
    public void ApplyProfile(JoyConSide side, JoyConProfile profile, List<MouseCommand> output)
    {
        profile = profile.Clamped();
        var old = ProfileFor(side);
        if (side == JoyConSide.Left)
        {
            _leftProfile = profile;
        }
        else
        {
            _rightProfile = profile;
        }

        if (ActiveDevice?.Side == side && !old.Buttons.SameAs(profile.Buttons))
        {
            ReleaseAll(output);
            _neutralConfirmed = false;
        }
    }

    public void SetActiveDevice(DeviceKey? key, long now, List<MouseCommand> output)
    {
        if (ActiveDevice == key)
        {
            return;
        }

        ReleaseAll(output);
        ActiveDevice = key;
        _neutralConfirmed = false;
    }

    public void OnDeviceConnected(DeviceKey key, long now)
    {
        _connected.Add(key);
        if (key == ActiveDevice)
        {
            _neutralConfirmed = false; // 재연결 시 중립 확인부터.
        }
    }

    public void OnDeviceDisconnected(DeviceKey key, long now, List<MouseCommand> output)
    {
        _connected.Remove(key);
        if (key == ActiveDevice)
        {
            // 다른 장치를 자동 활성화하지 않는다. 같은 장치가 돌아오면 중립 대기로 간다.
            ReleaseAll(output);
            _neutralConfirmed = false;
        }
    }

    public void SetPause(PauseReason reason, bool on, long now, List<MouseCommand> output)
    {
        var before = _pause;
        _pause = on ? _pause | reason : _pause & ~reason;
        if (before == PauseReason.None && _pause != PauseReason.None)
        {
            ReleaseAll(output);
        }
        else if (before != PauseReason.None && _pause == PauseReason.None)
        {
            _neutralConfirmed = false; // 재개 시 버튼·스틱을 놓아야 시작한다.
        }
    }

    /// <param name="activeSnapshot">활성 장치의 현재 입력. 활성 장치가 열려 있지 않으면 null.</param>
    public void Update(JoyConSnapshot? activeSnapshot, long now, List<MouseCommand> output)
    {
        if (activeSnapshot is not { } snap)
        {
            _lastUpdate = null;
            return;
        }

        switch (State)
        {
            case EngineState.Paused:
                UpdatePaused(snap, now, output);
                return;

            case EngineState.WaitingNeutral:
                UpdateWaiting(snap, now);
                return;

            case EngineState.Active:
                UpdateActive(snap, now, output);
                return;

            default:
                _lastUpdate = null;
                return;
        }
    }

    /// <summary>앱이 누른 마우스 버튼을 Up 하고 이동 잔여량·스크롤 반복을 비운다.</summary>
    public void ReleaseAll(List<MouseCommand> output)
    {
        _click.ReleaseAll(output);
        _scroll.Reset();
        _stick.Reset();
        _lastUpdate = null;
        _neutralSince = null;
    }

    private void UpdatePaused(JoyConSnapshot snap, long now, List<MouseCommand> output)
    {
        _lastUpdate = null;
        var toggle = ActiveProfile.Buttons.ButtonsFor(MouseAction.TogglePause);
        bool toggleEdge = (snap.Pressed & ~_previous & toggle) != 0;
        _previous = snap.Pressed;
        if (toggleEdge && (_pause & PauseReason.User) != 0)
        {
            SetPause(PauseReason.User, false, now, output);
        }
    }

    private void UpdateWaiting(JoyConSnapshot snap, long now)
    {
        _lastUpdate = null;
        _previous = snap.Pressed;
        double mag = Math.Sqrt((double)snap.StickX * snap.StickX + (double)snap.StickY * snap.StickY);
        bool neutral = snap.Pressed == JoyConButtons.None && mag < ActiveProfile.Stick.Deadzone;
        if (!neutral)
        {
            _neutralSince = null;
            return;
        }

        _neutralSince ??= now;
        if ((now - _neutralSince.Value) * 1000 >= NeutralHoldMs * _ticksPerSecond)
        {
            _neutralConfirmed = true;
            _neutralSince = null;
            _previous = JoyConButtons.None;
        }
    }

    private void UpdateActive(JoyConSnapshot snap, long now, List<MouseCommand> output)
    {
        var profile = ActiveProfile;
        double dt = _lastUpdate is { } last ? (double)(now - last) / _ticksPerSecond : 0;
        _lastUpdate = now;

        bool togglePause = _click.Update(_previous, snap.Pressed, profile.Buttons, output);
        _previous = snap.Pressed;

        var (dx, dy) = _stick.Step(snap.StickX, snap.StickY, dt, profile.Stick);
        if (dx != 0 || dy != 0)
        {
            output.Add(new MouseCommand.Move(dx, dy));
        }

        var map = profile.Buttons;
        var p = snap.Pressed;
        _scroll.Update(
            up: (p & map.ButtonsFor(MouseAction.ScrollUp)) != 0,
            down: (p & map.ButtonsFor(MouseAction.ScrollDown)) != 0,
            left: (p & map.ButtonsFor(MouseAction.ScrollLeft)) != 0,
            right: (p & map.ButtonsFor(MouseAction.ScrollRight)) != 0,
            now, profile.Scroll, output);

        if (togglePause)
        {
            SetPause(PauseReason.User, true, now, output);
        }
    }
}
