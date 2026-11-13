using static JoyConMouse.Platform.Win32.NativeMethods;

namespace JoyConMouse.Platform.Win32;

/// <summary>
/// 입력 스레드 대기용 타이머. 고해상도 waitable timer 를 쓰고, 지원하지 않으면 timeBeginPeriod(1) + Thread.Sleep 으로 대신한다.
/// </summary>
public sealed unsafe class TickTimer : IDisposable
{
    private nint _timer;
    private bool _periodRaised;

    public TickTimer()
    {
        _timer = CreateWaitableTimerEx(0, null, CREATE_WAITABLE_TIMER_HIGH_RESOLUTION, TIMER_ALL_ACCESS);
        if (_timer == 0)
        {
            _periodRaised = timeBeginPeriod(1) == 0;
        }
    }

    public bool HighResolution => _timer != 0;

    public void Wait(TimeSpan duration)
    {
        if (_timer != 0)
        {
            long due = -duration.Ticks; // 음수 = 상대 시간(100ns 단위)
            if (SetWaitableTimer(_timer, &due, 0, 0, 0, false))
            {
                WaitForSingleObject(_timer, INFINITE);
                return;
            }
        }

        Thread.Sleep(Math.Max(1, (int)duration.TotalMilliseconds));
    }

    public void Dispose()
    {
        if (_timer != 0)
        {
            CloseHandle(_timer);
            _timer = 0;
        }

        if (_periodRaised)
        {
            timeEndPeriod(1);
            _periodRaised = false;
        }
    }
}
