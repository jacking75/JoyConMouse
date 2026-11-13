using CommunityToolkit.Mvvm.ComponentModel;
using JoyConMouse.Core.Model;

namespace JoyConMouse.App.ViewModels;

public sealed record ActionOption(MouseAction Action, string Name)
{
    public static IReadOnlyList<ActionOption> All { get; } =
        Enum.GetValues<MouseAction>().Select(a => new ActionOption(a, Strings.Action(a))).ToList();

    public static ActionOption Of(MouseAction a) => All.First(o => o.Action == a);
}

/// <summary>버튼 배치 탭의 한 줄: 물리 버튼 하나와 지정된 기능.</summary>
public sealed class ButtonBindingViewModel : ObservableObject
{
    private ActionOption _selected;
    private bool _isPressed;

    public ButtonBindingViewModel(JoyConButtons button, MouseAction action, Action onChanged)
    {
        Button = button;
        Name = JoyConButtonInfo.DisplayName(button);
        _selected = ActionOption.Of(action);
        OnChanged = onChanged;
    }

    public JoyConButtons Button { get; }

    public string Name { get; }

    public IReadOnlyList<ActionOption> Options => ActionOption.All;

    private Action OnChanged { get; }

    public ActionOption Selected
    {
        get => _selected;
        set
        {
            if (value is not null && SetProperty(ref _selected, value))
            {
                OnChanged();
            }
        }
    }

    /// <summary>활성 조이콘에서 지금 눌린 버튼이면 true(강조 표시).</summary>
    public bool IsPressed
    {
        get => _isPressed;
        set => SetProperty(ref _isPressed, value);
    }
}
