namespace JoyConMouse.App.Services;

/// <summary>
/// 단일 인스턴스 보장. 두 번째 실행은 첫 인스턴스에 "창 열기" 신호만 보내고 종료한다.
/// </summary>
public sealed class SingleInstance : IDisposable
{
    private const string MutexName = @"Local\JoyConMouse.SingleInstance";
    private const string EventName = @"Local\JoyConMouse.Activate";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _event;
    private RegisteredWaitHandle? _wait;

    private SingleInstance(Mutex mutex, EventWaitHandle ev)
    {
        _mutex = mutex;
        _event = ev;
    }

    /// <summary>첫 인스턴스면 객체를, 이미 실행 중이면 신호를 보낸 뒤 null 을 돌려준다.</summary>
    public static SingleInstance? TryAcquire()
    {
        var mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            try
            {
                using var existing = EventWaitHandle.OpenExisting(EventName);
                existing.Set();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                // 첫 인스턴스가 아직 이벤트를 만들기 전이면 신호 없이 종료한다.
            }

            return null;
        }

        var ev = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
        return new SingleInstance(mutex, ev);
    }

    /// <summary>다른 인스턴스가 신호를 보내면 스레드 풀에서 <paramref name="onActivate"/> 를 호출한다.</summary>
    public void ListenForActivation(Action onActivate)
    {
        _wait = ThreadPool.RegisterWaitForSingleObject(_event, (_, _) => onActivate(), null, Timeout.Infinite, executeOnlyOnce: false);
    }

    public void Dispose()
    {
        _wait?.Unregister(null);
        _event.Dispose();
        try
        {
            _mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
        }

        _mutex.Dispose();
    }
}
