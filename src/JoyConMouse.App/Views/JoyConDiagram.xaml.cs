using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using JoyConMouse.Core.Model;

namespace JoyConMouse.App.Views;

/// <summary>세로로 쥔 조이콘 그림. 눌린 버튼을 강조한다.</summary>
public partial class JoyConDiagram : UserControl
{
    public static readonly DependencyProperty SideProperty = DependencyProperty.Register(
        nameof(Side), typeof(JoyConSide), typeof(JoyConDiagram),
        new PropertyMetadata(JoyConSide.Left, (d, _) => ((JoyConDiagram)d).Rebuild()));

    public static readonly DependencyProperty PressedProperty = DependencyProperty.Register(
        nameof(Pressed), typeof(JoyConButtons), typeof(JoyConDiagram),
        new PropertyMetadata(JoyConButtons.None, (d, _) => ((JoyConDiagram)d).UpdateHighlights()));

    private static readonly Brush BodyBrush = Frozen(Color.FromRgb(0x4B, 0x55, 0x63));
    private static readonly Brush RailBrush = Frozen(Color.FromRgb(0x9C, 0xA3, 0xAF));
    private static readonly Brush ButtonBrush = Frozen(Color.FromRgb(0xE5, 0xE7, 0xEB));
    private static readonly Brush PressedBrush = Frozen(Color.FromRgb(0xF9, 0x73, 0x16));
    private static readonly Brush LabelBrush = Frozen(Color.FromRgb(0x11, 0x18, 0x27));

    private readonly Dictionary<JoyConButtons, Shape> _shapes = [];

    public JoyConDiagram()
    {
        InitializeComponent();
        Rebuild();
    }

    public JoyConSide Side
    {
        get => (JoyConSide)GetValue(SideProperty);
        set => SetValue(SideProperty, value);
    }

    public JoyConButtons Pressed
    {
        get => (JoyConButtons)GetValue(PressedProperty);
        set => SetValue(PressedProperty, value);
    }

    private void Rebuild()
    {
        Root.Children.Clear();
        _shapes.Clear();
        bool left = Side == JoyConSide.Left;
        // 외곽(어깨·본체·레일)은 왼쪽 조이콘 좌표로 적고, 오른쪽은 좌우를 뒤집는다(레일이 반대편).
        double Mx(double x, double w) => left ? x : 140 - x - w;

        // 어깨 버튼: 뒤쪽 트리거(ZL/ZR)를 먼저, 그 앞에 L/R
        AddRect(left ? JoyConButtons.ZL : JoyConButtons.ZR, Mx(34, 66), 4, 66, 12, 6, left ? "ZL" : "ZR");
        AddRect(left ? JoyConButtons.L : JoyConButtons.R, Mx(28, 76), 18, 76, 12, 6, left ? "L" : "R");

        // 본체와 레일
        Add(new Rectangle { Width = 92, Height = 272, RadiusX = 40, RadiusY = 40, Fill = BodyBrush }, Mx(18, 92), 30);
        Add(new Rectangle { Width = 14, Height = 230, RadiusX = 4, RadiusY = 4, Fill = RailBrush }, Mx(110, 14), 50);

        // 레일 버튼: 왼쪽은 위 SL·아래 SR, 오른쪽은 위 SR·아래 SL
        AddRect(left ? JoyConButtons.SL : JoyConButtons.SR, Mx(111, 12), 78, 12, 40, 3, left ? "SL" : "SR", fontSize: 7);
        AddRect(left ? JoyConButtons.SR : JoyConButtons.SL, Mx(111, 12), 202, 12, 40, 3, left ? "SR" : "SL", fontSize: 7);

        if (left)
        {
            AddRect(JoyConButtons.Minus, 84, 44, 16, 10, 2, "−");
            AddCircle(JoyConButtons.LeftStick, 64, 96, 24, "스틱");
            AddCircle(JoyConButtons.DpadUp, 64, 152, 11, "↑");
            AddCircle(JoyConButtons.DpadDown, 64, 198, 11, "↓");
            AddCircle(JoyConButtons.DpadLeft, 41, 175, 11, "←");
            AddCircle(JoyConButtons.DpadRight, 87, 175, 11, "→");
            AddRect(JoyConButtons.Capture, 70, 232, 16, 16, 3, "●", fontSize: 7);
        }
        else
        {
            AddRect(JoyConButtons.Plus, 40, 44, 16, 10, 2, "+");
            AddCircle(JoyConButtons.X, 76, 72, 11, "X");
            AddCircle(JoyConButtons.B, 76, 118, 11, "B");
            AddCircle(JoyConButtons.Y, 53, 95, 11, "Y");
            AddCircle(JoyConButtons.A, 99, 95, 11, "A");
            AddCircle(JoyConButtons.RightStick, 76, 178, 24, "스틱");
            AddCircle(JoyConButtons.Home, 62, 240, 10, "⌂");
        }

        UpdateHighlights();
    }

    private void UpdateHighlights()
    {
        var pressed = Pressed;
        foreach (var (b, shape) in _shapes)
        {
            shape.Fill = (pressed & b) != 0 ? PressedBrush : ButtonBrush;
        }
    }

    private void AddCircle(JoyConButtons b, double cx, double cy, double r, string label)
    {
        var e = new Ellipse { Width = r * 2, Height = r * 2, Fill = ButtonBrush, ToolTip = JoyConButtonInfo.DisplayName(b) };
        Add(e, cx - r, cy - r);
        _shapes[b] = e;
        AddLabel(label, cx, cy, r >= 20 ? 10 : 9);
    }

    private void AddRect(JoyConButtons b, double x, double y, double w, double h, double radius, string label,
        double fontSize = 8, double labelDy = 0)
    {
        var rect = new Rectangle { Width = w, Height = h, RadiusX = radius, RadiusY = radius, Fill = ButtonBrush, ToolTip = JoyConButtonInfo.DisplayName(b) };
        Add(rect, x, y);
        _shapes[b] = rect;
        AddLabel(label, x + w / 2, y + h / 2 + labelDy, fontSize);
    }

    private void AddLabel(string text, double cx, double cy, double size)
    {
        var tb = new TextBlock { Text = text, FontSize = size, Foreground = LabelBrush, IsHitTestVisible = false };
        tb.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Add(tb, cx - tb.DesiredSize.Width / 2, cy - tb.DesiredSize.Height / 2);
    }

    private void Add(UIElement e, double x, double y)
    {
        Canvas.SetLeft(e, x);
        Canvas.SetTop(e, y);
        Root.Children.Add(e);
    }

    private static SolidColorBrush Frozen(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }
}
