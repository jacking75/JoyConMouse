using JoyConMouse.Core.Engine;
using JoyConMouse.Platform;
using JoyConMouse.Platform.Diagnostics;
using JoyConMouse.Platform.Sdl;
using Microsoft.Win32;

namespace JoyConMouse.App.Services;

/// <summary>절전·세션 잠금 이벤트를 입력 스레드 일시정지 요청으로 넘긴다.</summary>
public sealed class SystemEventsBridge : IDisposable
{
    /// <summary>절전 진입 시 해제 명령 전송을 기다리는 최대 시간. 운영체제는 오래 기다려 주지 않는다.</summary>
    private static readonly TimeSpan SuspendWait = TimeSpan.FromMilliseconds(200);

    private readonly InputLoop _loop;

    public SystemEventsBridge(InputLoop loop)
    {
        _loop = loop;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.SessionSwitch += OnSessionSwitch;
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        switch (e.Mode)
        {
            case PowerModes.Suspend:
                using (var done = new ManualResetEventSlim())
                {
                    _loop.Post(new EngineRequest.SetPause(PauseReason.Suspend, true, done));
                    bool ok = done.Wait(SuspendWait);
                    Log.Info($"절전 진입: 일시정지{(ok ? "" : " (처리 대기 시간 초과)")}");
                }

                break;
            case PowerModes.Resume:
                _loop.Post(new EngineRequest.SetPause(PauseReason.Suspend, false));
                Log.Info("절전 복귀");
                break;
        }
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        bool? locked = e.Reason switch
        {
            SessionSwitchReason.SessionLock or SessionSwitchReason.ConsoleDisconnect or SessionSwitchReason.RemoteDisconnect => true,
            SessionSwitchReason.SessionUnlock or SessionSwitchReason.ConsoleConnect or SessionSwitchReason.RemoteConnect => false,
            _ => null,
        };
        if (locked is { } on)
        {
            _loop.Post(new EngineRequest.SetPause(PauseReason.SessionLock, on));
            Log.Info($"세션 {e.Reason}: 일시정지 {(on ? "켬" : "끔")}");
        }
    }

    public void Dispose()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        SystemEvents.SessionSwitch -= OnSessionSwitch;
    }
}
