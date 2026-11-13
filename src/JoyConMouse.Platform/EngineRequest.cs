using JoyConMouse.Core.Engine;
using JoyConMouse.Core.Model;
using JoyConMouse.Core.Settings;

namespace JoyConMouse.Platform;

/// <summary>UI 스레드가 입력 스레드로 보내는 요청. 입력 스레드가 큐에서 꺼내 순서대로 처리한다.</summary>
public abstract record EngineRequest
{
    private EngineRequest() { }

    public sealed record SetActive(DeviceKey? Device) : EngineRequest;

    /// <summary>자동 선택 규칙에 쓸 저장된 장치(마지막 활성 장치).</summary>
    public sealed record SetSavedDevice(DeviceKey? Device) : EngineRequest;

    /// <param name="Done">처리 후(해제 명령 전송까지) 신호. 절전 진입처럼 기다려야 할 때만 넘긴다.</param>
    public sealed record SetPause(PauseReason Reason, bool On, ManualResetEventSlim? Done = null) : EngineRequest;

    /// <summary>사용자 일시정지 토글(UI·트레이 버튼).</summary>
    public sealed record ToggleUserPause : EngineRequest;

    public sealed record ApplyProfile(JoyConSide Side, JoyConProfile Profile) : EngineRequest;

    /// <summary>켜면 활성 장치의 실시간 입력(<see cref="ActiveInput"/>)을 20Hz 로 발행한다. 연결 확인 화면용.</summary>
    public sealed record SetMonitoring(bool On) : EngineRequest;

    public sealed record Shutdown(ManualResetEventSlim Done) : EngineRequest;
}
