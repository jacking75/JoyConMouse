using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using JoyConMouse.Core.Buttons;
using JoyConMouse.Core.Model;
using JoyConMouse.Core.Motion;
using JoyConMouse.Core.Scroll;
using JoyConMouse.Core.Settings;

namespace JoyConMouse.App.ViewModels;

/// <summary>한 측면의 프로필 편집 화면. 값이 바뀔 때마다 <see cref="Changed"/> 로 새 프로필을 알린다.</summary>
public sealed class ProfileViewModel : ObservableObject
{
    private double _maxSpeed, _curve, _deadzonePercent, _stepDelta, _initialDelay, _interval;
    private bool _suppress;

    public ProfileViewModel(JoyConSide side, JoyConProfile profile)
    {
        Side = side;
        Buttons = new ObservableCollection<ButtonBindingViewModel>(
            JoyConButtonInfo.ButtonsOf(side).Select(b => new ButtonBindingViewModel(b, profile.Buttons[b], RaiseChanged)));
        Load(profile);
    }

    public event Action<JoyConSide, JoyConProfile>? Changed;

    public JoyConSide Side { get; }

    public ObservableCollection<ButtonBindingViewModel> Buttons { get; }

    public double MaxSpeedMin => StickSettings.MaxSpeedMin;
    public double MaxSpeedMax => StickSettings.MaxSpeedMax;
    public double CurveMin => StickSettings.CurveMin;
    public double CurveMax => StickSettings.CurveMax;
    public double DeadzoneMinPercent => StickSettings.DeadzoneMin * 100;
    public double DeadzoneMaxPercent => StickSettings.DeadzoneMax * 100;
    public double StepDeltaMin => ScrollSettings.StepDeltaMin;
    public double StepDeltaMax => ScrollSettings.StepDeltaMax;
    public double InitialDelayMin => ScrollSettings.InitialDelayMin;
    public double InitialDelayMax => ScrollSettings.InitialDelayMax;
    public double IntervalMin => ScrollSettings.RepeatIntervalMin;
    public double IntervalMax => ScrollSettings.RepeatIntervalMax;

    public double MaxSpeed { get => _maxSpeed; set => SetValue(ref _maxSpeed, Math.Round(value / 50) * 50); }

    public double CurveExponent { get => _curve; set => SetValue(ref _curve, Math.Round(value, 1)); }

    public double DeadzonePercent { get => _deadzonePercent; set => SetValue(ref _deadzonePercent, Math.Round(value)); }

    public double StepDelta { get => _stepDelta; set => SetValue(ref _stepDelta, Math.Round(value / 10) * 10); }

    public double InitialDelayMs { get => _initialDelay; set => SetValue(ref _initialDelay, Math.Round(value / 10) * 10); }

    public double RepeatIntervalMs { get => _interval; set => SetValue(ref _interval, Math.Round(value / 5) * 5); }

    public bool HasTogglePause => Buttons.Any(b => b.Selected.Action == MouseAction.TogglePause);

    public JoyConProfile ToProfile() => new JoyConProfile(
        new StickSettings { MaxSpeed = MaxSpeed, CurveExponent = CurveExponent, Deadzone = DeadzonePercent / 100 },
        new ScrollSettings { StepDelta = (int)StepDelta, InitialDelayMs = (int)InitialDelayMs, RepeatIntervalMs = (int)RepeatIntervalMs },
        new ButtonMap(Buttons.Select(b => KeyValuePair.Create(b.Button, b.Selected.Action)))).Clamped();

    /// <summary>값을 통째로 바꾼다(기본값 복원·미러). 바뀐 뒤 한 번만 알린다.</summary>
    public void Load(JoyConProfile p, bool notify = false)
    {
        _suppress = true;
        try
        {
            MaxSpeed = p.Stick.MaxSpeed;
            CurveExponent = p.Stick.CurveExponent;
            DeadzonePercent = p.Stick.Deadzone * 100;
            StepDelta = p.Scroll.StepDelta;
            InitialDelayMs = p.Scroll.InitialDelayMs;
            RepeatIntervalMs = p.Scroll.RepeatIntervalMs;
            foreach (var b in Buttons)
            {
                b.Selected = ActionOption.Of(p.Buttons[b.Button]);
            }
        }
        finally
        {
            _suppress = false;
        }

        OnPropertyChanged(nameof(HasTogglePause));
        if (notify)
        {
            Changed?.Invoke(Side, ToProfile());
        }
    }

    public void SetPressed(JoyConButtons pressed)
    {
        foreach (var b in Buttons)
        {
            b.IsPressed = (pressed & b.Button) != 0;
        }
    }

    private void SetValue(ref double field, double value, [CallerMemberName] string? propertyName = null)
    {
        if (SetProperty(ref field, value, propertyName))
        {
            RaiseChanged();
        }
    }

    private void RaiseChanged()
    {
        if (_suppress)
        {
            return;
        }

        OnPropertyChanged(nameof(HasTogglePause));
        Changed?.Invoke(Side, ToProfile());
    }
}
