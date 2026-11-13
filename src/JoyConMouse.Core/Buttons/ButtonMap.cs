using System.Collections;
using JoyConMouse.Core.Model;

namespace JoyConMouse.Core.Buttons;

/// <summary>물리 버튼 → 마우스 동작 배치. 불변이며, 지정되지 않은 버튼은 <see cref="MouseAction.None"/>.</summary>
public sealed class ButtonMap : IEnumerable<KeyValuePair<JoyConButtons, MouseAction>>
{
    private readonly Dictionary<JoyConButtons, MouseAction> _map;
    private readonly JoyConButtons[] _maskByAction;

    public ButtonMap(IEnumerable<KeyValuePair<JoyConButtons, MouseAction>> entries)
    {
        _map = [];
        _maskByAction = new JoyConButtons[Enum.GetValues<MouseAction>().Length];
        foreach (var (button, action) in entries)
        {
            // 단일 비트 버튼만 받는다. None 동작은 저장하지 않는다.
            if (button != JoyConButtons.None && (button & (button - 1)) == 0 && action != MouseAction.None
                && Enum.IsDefined(action))
            {
                _map[button] = action;
            }
        }

        foreach (var (b, a) in _map)
        {
            _maskByAction[(int)a] |= b;
        }
    }

    public static ButtonMap Empty { get; } = new([]);

    public MouseAction this[JoyConButtons button] => _map.GetValueOrDefault(button, MouseAction.None);

    public ButtonMap With(JoyConButtons button, MouseAction action)
    {
        var copy = new Dictionary<JoyConButtons, MouseAction>(_map) { [button] = action };
        return new ButtonMap(copy);
    }

    /// <summary>지정한 동작이 붙은 물리 버튼들의 비트 합.</summary>
    public JoyConButtons ButtonsFor(MouseAction action) =>
        action == MouseAction.None || !Enum.IsDefined(action) ? JoyConButtons.None : _maskByAction[(int)action];

    public bool SameAs(ButtonMap other) =>
        _map.Count == other._map.Count && _map.All(kv => other[kv.Key] == kv.Value);

    public IEnumerator<KeyValuePair<JoyConButtons, MouseAction>> GetEnumerator() => _map.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
