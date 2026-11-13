using System.ComponentModel;
using System.Windows;

namespace JoyConMouse.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // 연결 확인 모드(마우스 출력 정지)는 창이 보일 때만 켠다. 숨기거나 최소화하면 자동으로 풀린다.
        IsVisibleChanged += (_, _) => ReportShown();
        StateChanged += (_, _) => ReportShown();
    }

    private void ReportShown() =>
        (DataContext as ViewModels.MainViewModel)?.SetWindowShown(IsVisible && WindowState != WindowState.Minimized);

    /// <summary>true 면 닫기(X)가 실제로 창을 닫는다. 앱 종료 시에만 켠다.</summary>
    public bool AllowClose { get; set; }

    /// <summary>닫기(X)로 트레이에 숨겼을 때.</summary>
    public event Action? HiddenToTray;

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            Hide();
            HiddenToTray?.Invoke();
        }

        base.OnClosing(e);
    }

    /// <summary>숨김·최소화 상태에서도 창을 앞으로 가져온다.</summary>
    public void BringToFront()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Topmost = true;  // 다른 앱 뒤에 있을 때 앞으로 올리기 위한 관용 방법
        Topmost = false;
        Focus();
    }
}
