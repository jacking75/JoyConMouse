using CommunityToolkit.Mvvm.ComponentModel;
using JoyConMouse.Core.Model;
using JoyConMouse.Platform;

namespace JoyConMouse.App.ViewModels;

/// <summary>장치 목록의 한 줄. 측면 + 식별자 끝 4자리.</summary>
public sealed class DeviceItemViewModel : ObservableObject
{
    private bool _isActive;
    private bool _emphasizeId;
    private string _battery = "";

    public DeviceItemViewModel(DeviceKey key)
    {
        Key = key;
    }

    public DeviceKey Key { get; }

    public string SideName => Strings.Side(Key.Side);

    public string ShortId => "…" + (Key.Id.Length > 4 ? Key.Id[^4..] : Key.Id);

    public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

    /// <summary>같은 측면 장치가 둘 이상이면 식별자를 강조한다.</summary>
    public bool EmphasizeId { get => _emphasizeId; set => SetProperty(ref _emphasizeId, value); }

    public string Battery { get => _battery; set => SetProperty(ref _battery, value); }

    public void Update(DeviceInfo info, bool active, bool emphasize)
    {
        IsActive = active;
        EmphasizeId = emphasize;
        Battery = Strings.Format("Battery.Label", Strings.Battery(info));
    }
}
