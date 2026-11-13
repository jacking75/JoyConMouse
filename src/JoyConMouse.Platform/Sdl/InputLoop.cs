using System.Collections.Concurrent;
using System.Diagnostics;
using JoyConMouse.Core.Engine;
using JoyConMouse.Core.Model;
using JoyConMouse.Platform.Diagnostics;
using JoyConMouse.Platform.Win32;

namespace JoyConMouse.Platform.Sdl;

/// <summary>
/// 입력 전용 스레드. SDL 초기화·이벤트 폴링·상태 조회·엔진 계산·SendInput 호출을 모두 이 스레드에서 한다.
/// UI 는 <see cref="Post"/> 로 요청을 보내고 <see cref="StatusPublished"/> 로 상태를 받는다(입력 스레드에서 호출됨).
/// </summary>
public sealed class InputLoop : IDisposable
{
    private static readonly TimeSpan ActiveTick = TimeSpan.FromMilliseconds(4);
    private static readonly TimeSpan IdleTick = TimeSpan.FromMilliseconds(20);
    private static readonly long PressedPublishInterval = Stopwatch.Frequency / 20; // 20Hz
    private static readonly long HeartbeatInterval = Stopwatch.Frequency;           // 1초

    private readonly ConcurrentQueue<EngineRequest> _requests = new();
    private readonly IMouseSink _sink;
    private readonly bool _vertical;
    private Thread? _thread;

    public InputLoop(IMouseSink sink, bool verticalJoyCons = true)
    {
        _sink = sink;
        _vertical = verticalJoyCons;
    }

    /// <summary>입력 스레드에서 호출된다. 구독자는 UI 스레드로 넘겨서 처리해야 한다.</summary>
    public event Action<EngineStatus>? StatusPublished;

    /// <summary>스냅샷 생성 → SendInput 반환까지의 내부 처리 시간.</summary>
    public LatencyRecorder Latency { get; } = new();

    /// <summary>입력 스레드 틱 간격.</summary>
    public LatencyRecorder TickIntervals { get; } = new();

    public long SendFailures => (_sink as SendInputMouseSink)?.Failures ?? 0;

    public bool HighResolutionTimer { get; private set; }

    /// <summary>SDL 세로 조이콘 모드 힌트 사용 여부.</summary>
    public bool VerticalJoyCons => _vertical;

    public bool IsRunning => _thread is { IsAlive: true };

    public void Start()
    {
        if (_thread is not null)
        {
            throw new InvalidOperationException("이미 시작했습니다.");
        }

        _thread = new Thread(Run)
        {
            IsBackground = true,
            Name = "JoyConInput",
            Priority = ThreadPriority.AboveNormal,
        };
        _thread.Start();
    }

    public void Post(EngineRequest request) => _requests.Enqueue(request);

    /// <summary>눌린 입력을 해제하고 SDL 을 종료한 뒤 스레드를 멈춘다. 제한 시간 안에 끝나면 true.</summary>
    public bool Stop(TimeSpan timeout)
    {
        if (_thread is not { IsAlive: true })
        {
            return true;
        }

        using var done = new ManualResetEventSlim();
        Post(new EngineRequest.Shutdown(done));
        bool ok = done.Wait(timeout);
        if (ok)
        {
            _thread.Join(TimeSpan.FromMilliseconds(200));
        }

        return ok;
    }

    public void Dispose() => Stop(TimeSpan.FromSeconds(1));

    private void Run()
    {
        Log.Info($"입력 스레드 시작 (세로 힌트={(_vertical ? 1 : 0)})");
        SdlContext sdl;
        try
        {
            sdl = SdlContext.Initialize(_vertical);
        }
        catch (Exception ex) when (ex is SdlException or DllNotFoundException or EntryPointNotFoundException)
        {
            Log.Error("SDL 초기화 실패", ex);
            RunWithoutSdl(ex.Message);
            return;
        }

        string version = SdlNative.VersionString();
        Log.Info($"SDL {version} 초기화");
        try
        {
            RunLoop(version);
        }
        catch (Exception ex)
        {
            Log.Error("입력 스레드 오류", ex);
            StatusPublished?.Invoke(EngineStatus.Initial with { Error = "입력 처리 중 오류가 발생했습니다: " + ex.Message, SdlVersion = version });
            RunWithoutSdl(ex.Message);
        }
        finally
        {
            sdl.Dispose();
            Log.Info("입력 스레드 종료");
        }
    }

    /// <summary>SDL 을 쓸 수 없을 때: 오류 상태를 알리고 종료 요청만 기다린다.</summary>
    private void RunWithoutSdl(string error)
    {
        StatusPublished?.Invoke(EngineStatus.Initial with { Error = error });
        while (true)
        {
            while (_requests.TryDequeue(out var r))
            {
                switch (r)
                {
                    case EngineRequest.Shutdown s:
                        s.Done.Set();
                        return;
                    case EngineRequest.SetPause { Done: { } d }:
                        d.Set();
                        break;
                }
            }

            Thread.Sleep(50);
        }
    }

    private void RunLoop(string sdlVersion)
    {
        var engine = new MouseEngine(Stopwatch.Frequency);
        var output = new List<MouseCommand>(16);
        var pads = new Dictionary<uint, SdlGamepad>();
        var pendingDone = new List<ManualResetEventSlim>();
        using var timer = new TickTimer();
        HighResolutionTimer = timer.HighResolution;
        Log.Info($"고해상도 타이머: {timer.HighResolution}");

        DeviceKey? saved = null;
        bool monitoring = false;
        bool devicesChanged = true;
        bool needsChoice = false;
        long lastTick = 0, lastPublish = 0, lastDevicesBuilt = 0;
        EngineStatus? published = null;
        EngineRequest.Shutdown? shutdown = null;

        try
        {
            while (shutdown is null)
            {
                long now = Stopwatch.GetTimestamp();
                if (lastTick != 0)
                {
                    TickIntervals.Add(now - lastTick);
                }

                lastTick = now;

                // 1. SDL 이벤트: 장치 연결·해제
                while (SdlEvents.Poll(out var ev))
                {
                    if (ev.Type == SdlEventType.GamepadAdded && !pads.ContainsKey(ev.Which))
                    {
                        var pad = SdlGamepad.Open(ev.Which);
                        if (pad?.Key is { } key)
                        {
                            pads[ev.Which] = pad;
                            engine.OnDeviceConnected(key, now);
                            devicesChanged = true;
                            Log.Info($"연결: {pad.Name} 키={key.Id} 시리얼={pad.SerialFormat}");
                        }
                        else
                        {
                            pad?.Dispose(); // 조이콘이 아닌 게임패드는 쓰지 않는다.
                        }
                    }
                    else if (ev.Type == SdlEventType.GamepadRemoved && pads.Remove(ev.Which, out var gone))
                    {
                        if (gone.Key is { } key)
                        {
                            engine.OnDeviceDisconnected(key, now, output);
                            Log.Info($"연결 해제: {gone.Name} 키={key.Id}");
                        }

                        gone.Dispose();
                        devicesChanged = true;
                    }
                }

                // 2. UI 요청
                while (shutdown is null && _requests.TryDequeue(out var req))
                {
                    switch (req)
                    {
                        case EngineRequest.Shutdown s:
                            shutdown = s;
                            break;
                        case EngineRequest.SetActive a:
                            engine.SetActiveDevice(a.Device, now, output);
                            devicesChanged = true;
                            break;
                        case EngineRequest.SetSavedDevice sd:
                            saved = sd.Device;
                            devicesChanged = true;
                            break;
                        case EngineRequest.SetPause p:
                            engine.SetPause(p.Reason, p.On, now, output);
                            if (p.Done is not null)
                            {
                                pendingDone.Add(p.Done);
                            }

                            break;
                        case EngineRequest.ToggleUserPause:
                            engine.SetPause(PauseReason.User, (engine.Pause & PauseReason.User) == 0, now, output);
                            break;
                        case EngineRequest.ApplyProfile ap:
                            engine.ApplyProfile(ap.Side, ap.Profile, output);
                            break;
                        case EngineRequest.SetMonitoring m:
                            monitoring = m.On;
                            break;
                    }
                }

                if (shutdown is not null)
                {
                    break;
                }

                // 3. 활성 장치 자동 선택(선택된 장치가 없을 때만)
                if (devicesChanged)
                {
                    devicesChanged = false;
                    needsChoice = false;
                    if (engine.ActiveDevice is null)
                    {
                        var keys = pads.Values.Select(p => p.Key!.Value).Distinct().ToList();
                        var (outcome, device) = DeviceSelector.Select(saved, keys);
                        if (outcome == SelectionOutcome.Selected)
                        {
                            engine.SetActiveDevice(device, now, output);
                            Log.Info($"자동 선택: {device!.Value.Side} {device.Value.Id}");
                        }

                        needsChoice = outcome == SelectionOutcome.NeedsChoice;
                    }
                }

                // 4. 활성 장치 입력 → 엔진 → SendInput
                SdlGamepad? active = null;
                if (engine.ActiveDevice is { } ak)
                {
                    foreach (var p in pads.Values)
                    {
                        if (p.Key == ak)
                        {
                            active = p;
                            break;
                        }
                    }
                }

                JoyConSnapshot? snap = active is null ? null : JoyConNormalizer.Read(active, active.Side!.Value, _vertical);
                long tSnap = Stopwatch.GetTimestamp();
                engine.Update(snap, tSnap, output);
                if (output.Count > 0)
                {
                    _sink.Send(output);
                    Latency.Add(Stopwatch.GetTimestamp() - tSnap);
                    output.Clear();
                }

                foreach (var d in pendingDone)
                {
                    d.Set();
                }

                pendingDone.Clear();

                // 5. 상태 발행
                long t = Stopwatch.GetTimestamp();
                ActiveInput? input = monitoring && active is not null && snap is { } sn
                    ? new ActiveInput(sn.StickX, sn.StickY, sn.Pressed, active.GetGamepadButtonBits(), active.GetGamepadAxes())
                    : null;
                // 장치 정보(배터리 등)는 목록이 바뀌었거나 1초가 지났을 때만 다시 읽는다.
                IReadOnlyList<DeviceInfo> devices;
                if (published is not null && t - lastDevicesBuilt < HeartbeatInterval && SameDevices(published, pads))
                {
                    devices = published.Devices;
                }
                else
                {
                    devices = BuildDevices(pads);
                    lastDevicesBuilt = t;
                }

                var status = new EngineStatus(
                    engine.State, engine.ActiveDevice, engine.Pause, devices,
                    snap?.Pressed ?? JoyConButtons.None, needsChoice, null, sdlVersion, input);

                bool important = published is null
                                 || status.State != published.State
                                 || status.ActiveDevice != published.ActiveDevice
                                 || status.Pause != published.Pause
                                 || status.NeedsChoice != published.NeedsChoice
                                 || !ReferenceEquals(status.Devices, published.Devices);
                bool pressedChanged = published is not null && status.ActivePressed != published.ActivePressed;
                if (important
                    || ((pressedChanged || input is not null) && t - lastPublish >= PressedPublishInterval)
                    || t - lastPublish >= HeartbeatInterval)
                {
                    published = status;
                    lastPublish = t;
                    StatusPublished?.Invoke(status);
                }

                // 6. 대기
                timer.Wait(engine.State == EngineState.Active ? ActiveTick : IdleTick);
            }
        }
        finally
        {
            // 종료: 눌린 입력 해제 → 전송 → 장치 닫기
            engine.ReleaseAll(output);
            if (output.Count > 0)
            {
                _sink.Send(output);
                output.Clear();
            }

            foreach (var d in pendingDone)
            {
                d.Set();
            }

            foreach (var p in pads.Values)
            {
                p.Dispose();
            }

            pads.Clear();
            shutdown?.Done.Set();
        }
    }

    private static bool SameDevices(EngineStatus published, Dictionary<uint, SdlGamepad> pads) =>
        published.Devices.Count == pads.Count
        && pads.Values.All(p => published.Devices.Any(d => d.Key == p.Key));

    private static List<DeviceInfo> BuildDevices(Dictionary<uint, SdlGamepad> pads)
    {
        var list = new List<DeviceInfo>(pads.Count);
        foreach (var p in pads.Values.OrderBy(p => p.Side).ThenBy(p => p.InstanceId))
        {
            var (state, percent) = p.GetPower();
            var power = state switch
            {
                SdlPowerState.OnBattery => PowerSource.Battery,
                SdlPowerState.Charging => PowerSource.Charging,
                SdlPowerState.Charged => PowerSource.Charged,
                SdlPowerState.NoBattery => PowerSource.Wired,
                _ => PowerSource.Unknown,
            };
            var connection = p.GetConnectionState() switch
            {
                SdlConnectionState.Wired => ConnectionKind.Wired,
                SdlConnectionState.Wireless => ConnectionKind.Wireless,
                _ => ConnectionKind.Unknown,
            };
            list.Add(new DeviceInfo(p.Key!.Value, p.Name, DeviceInfo.LevelFromPercent(percent), percent, power,
                p.HasSerial, connection));
        }

        return list;
    }
}
