using JoyConMouse.Core.Model;

namespace JoyConMouse.Core.Buttons;

/// <summary>
/// 물리 버튼의 눌림·해제 엣지로 마우스 버튼 명령을 만든다.
/// 같은 마우스 버튼에 물리 버튼 여러 개가 지정될 수 있어 마우스 버튼별 참조 카운트를 둔다.
/// </summary>
public sealed class ClickMapper
{
    private readonly int[] _refCount = new int[3];

    /// <summary>앱이 Down 을 보낸 뒤 아직 Up 을 보내지 않은 마우스 버튼.</summary>
    public bool IsDown(MouseButton b) => _refCount[(int)b] > 0;

    /// <returns>이번 프레임에 <see cref="MouseAction.TogglePause"/> 버튼이 새로 눌렸으면 true.</returns>
    public bool Update(JoyConButtons previous, JoyConButtons current, ButtonMap map, List<MouseCommand> output)
    {
        var pressed = current & ~previous;
        var released = previous & ~current;
        bool togglePause = false;

        for (uint v = (uint)pressed; v != 0; v &= v - 1)
        {
            var action = map[(JoyConButtons)(v & (~v + 1))];
            if (action == MouseAction.TogglePause)
            {
                togglePause = true;
            }
            else if (ToMouseButton(action) is { } mb && ++_refCount[(int)mb] == 1)
            {
                output.Add(new MouseCommand.Button(mb, true));
            }
        }

        for (uint v = (uint)released; v != 0; v &= v - 1)
        {
            if (ToMouseButton(map[(JoyConButtons)(v & (~v + 1))]) is { } mb && _refCount[(int)mb] > 0 && --_refCount[(int)mb] == 0)
            {
                output.Add(new MouseCommand.Button(mb, false));
            }
        }

        return togglePause;
    }

    /// <summary>앱이 누른 마우스 버튼만 Up 한다. 물리 마우스 상태는 건드리지 않는다.</summary>
    public void ReleaseAll(List<MouseCommand> output)
    {
        for (int i = 0; i < _refCount.Length; i++)
        {
            if (_refCount[i] > 0)
            {
                _refCount[i] = 0;
                output.Add(new MouseCommand.Button((MouseButton)i, false));
            }
        }
    }

    private static MouseButton? ToMouseButton(MouseAction a) => a switch
    {
        MouseAction.LeftClick => MouseButton.Left,
        MouseAction.RightClick => MouseButton.Right,
        MouseAction.MiddleClick => MouseButton.Middle,
        _ => null,
    };
}
