using JoyConMouse.Core.Model;

namespace JoyConMouse.Core.Settings;

/// <summary>앱 설정 전체. 시작 프로그램 등록 여부는 레지스트리 상태를 그대로 읽으므로 여기에 두지 않는다.</summary>
public sealed record AppSettings
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public DeviceKey? LastActiveDevice { get; init; }

    public bool StartPaused { get; init; }

    /// <summary>쓰지 않는 조이콘에 주기적으로 신호를 보내 연결이 끊기지 않게 할지.</summary>
    public bool KeepConnected { get; init; } = true;

    /// <summary>창을 닫을 때 "트레이에서 계속 실행" 안내를 이미 보여 주었는지.</summary>
    public bool TrayNoticeShown { get; init; }

    public JoyConProfile Left { get; init; } = JoyConProfile.DefaultFor(JoyConSide.Left);

    public JoyConProfile Right { get; init; } = JoyConProfile.DefaultFor(JoyConSide.Right);

    public static AppSettings Default { get; } = new();

    public JoyConProfile ProfileFor(JoyConSide side) => side == JoyConSide.Left ? Left : Right;

    public AppSettings WithProfile(JoyConSide side, JoyConProfile profile) =>
        side == JoyConSide.Left ? this with { Left = profile } : this with { Right = profile };
}
