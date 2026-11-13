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

    public static string Battery(DeviceInfo? d)
    {
        if (d is null)
        {
            return Get("Battery.Unknown");
        }

        string level = Get("Battery." + d.Battery);
        return d.Power == PowerSource.Charging ? Format("Battery.Charging", level) : level;
    }
}
