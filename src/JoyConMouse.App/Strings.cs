using System.Windows;
using JoyConMouse.Core.Model;
using JoyConMouse.Platform;

namespace JoyConMouse.App;

/// <summary>Resources/Strings.ko.xaml 의 문자열을 코드에서 읽는다.</summary>
public static class Strings
{
    public static string Get(string key) =>
        Application.Current?.TryFindResource(key) as string ?? key;

    public static string Format(string key, params object?[] args) => string.Format(Get(key), args);

    public static string Side(JoyConSide side) => Get(side == JoyConSide.Left ? "Side.LeftDevice" : "Side.RightDevice");

    public static string Action(MouseAction a) => Get("Action." + a);

    /// <summary>배터리 표시 문구. 예: "75%", "75% (충전 중)", "알 수 없음". 조이콘은 25% 단위로만 보고한다.</summary>
    public static string Battery(DeviceInfo? d)
    {
        if (d is null || d.BatteryPercent < 0)
        {
            return Get("Battery.Unknown");
        }

        string value = $"{d.BatteryPercent}%";
        return d.Power switch
        {
            PowerSource.Charging => Format("Battery.Charging", value),
            PowerSource.Charged => Format("Battery.Charged", value),
            _ => value,
        };
    }
}
