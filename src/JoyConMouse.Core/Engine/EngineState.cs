namespace JoyConMouse.Core.Engine;

public enum EngineState
{
    /// <summary>활성 장치 미선택.</summary>
    NoDevice,

    /// <summary>선택한 장치가 끊김.</summary>
    Disconnected,

    /// <summary>선택·재연결·재개 직후 중립(스틱·버튼을 놓음) 확인 중.</summary>
    WaitingNeutral,

    /// <summary>정상 동작.</summary>
    Active,

    /// <summary>일시정지 사유가 하나 이상. 일시정지 토글 버튼만 처리한다.</summary>
    Paused,
}

[Flags]
public enum PauseReason
{
    None = 0,
    User = 1,
    Suspend = 2,
    SessionLock = 4,

    /// <summary>설정 창의 연결 확인 중. 버튼을 눌러도 마우스 출력이 나가지 않게 한다.</summary>
    InputTest = 8,
}
