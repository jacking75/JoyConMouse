using System.Windows;
using System.Windows.Media;

namespace JoyConMouse.App.Views;

/// <summary>
/// 4칸 배터리 막대. 조이콘은 0·25·50·75·100% 다섯 단계만 보고하므로 25%마다 한 칸씩 채운다.
/// Percent 가 음수면 알 수 없음(빈 회색 테두리).
/// </summary>
public sealed class BatteryIndicator : FrameworkElement
{
    public static readonly DependencyProperty PercentProperty = DependencyProperty.Register(
        nameof(Percent), typeof(int), typeof(BatteryIndicator),
        new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ChargingProperty = DependencyProperty.Register(
        nameof(Charging), typeof(bool), typeof(BatteryIndicator),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly Brush OutlineBrush = Frozen(Color.FromRgb(0x6B, 0x72, 0x80));
    private static readonly Brush UnknownBrush = Frozen(Color.FromRgb(0xC4, 0xC8, 0xCE));
    private static readonly Brush GoodBrush = Frozen(Color.FromRgb(0x2E, 0xA0, 0x43));
    private static readonly Brush LowBrush = Frozen(Color.FromRgb(0xE0, 0x8A, 0x00));
    private static readonly Brush EmptyBrush = Frozen(Color.FromRgb(0xDC, 0x26, 0x26));
    private static readonly Brush BoltBrush = Frozen(Color.FromRgb(0x25, 0x63, 0xEB));
    private static readonly Geometry Bolt = Geometry.Parse("M 6,0 L 1,7 L 5,7 L 3,12 L 9,4 L 5,4 Z");

    public BatteryIndicator()
    {
        Width = 30;
        Height = 13;
        VerticalAlignment = VerticalAlignment.Center;
        SnapsToDevicePixels = true;
    }

    public int Percent
    {
        get => (int)GetValue(PercentProperty);
        set => SetValue(PercentProperty, value);
    }

    public bool Charging
    {
        get => (bool)GetValue(ChargingProperty);
        set => SetValue(ChargingProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        int p = Percent;
        var outline = new Pen(p < 0 ? UnknownBrush : p == 0 ? EmptyBrush : OutlineBrush, 1.2);
        // 몸통 22x12, 단자 2x5
        dc.DrawRoundedRectangle(null, outline, new Rect(0.6, 0.6, 22, 11.8), 2, 2);
        dc.DrawRectangle(p < 0 ? UnknownBrush : OutlineBrush, null, new Rect(23.2, 4, 2, 5));

        if (p > 0)
        {
            int bars = Math.Clamp((int)Math.Round(p / 25.0), 1, 4);
            var fill = p <= 25 ? LowBrush : GoodBrush;
            for (int i = 0; i < bars; i++)
            {
                dc.DrawRectangle(fill, null, new Rect(2.5 + i * 5, 2.5, 4, 8));
            }
        }

        if (Charging)
        {
            dc.PushTransform(new TranslateTransform(20, 0.5));
            dc.DrawGeometry(BoltBrush, new Pen(Brushes.White, 0.8), Bolt);
            dc.Pop();
        }
    }

    private static SolidColorBrush Frozen(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
