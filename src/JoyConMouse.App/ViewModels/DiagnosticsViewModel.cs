using CommunityToolkit.Mvvm.ComponentModel;
using JoyConMouse.Platform.Diagnostics;
using JoyConMouse.Platform.Sdl;

namespace JoyConMouse.App.ViewModels;

/// <summary>진단 창: 내부 지연·틱 간격·SendInput 실패 수. 1초마다 갱신한다.</summary>
public sealed class DiagnosticsViewModel(InputLoop loop) : ObservableObject
{
    private string _latency = "", _tick = "", _failures = "", _timer = "";

    public string Latency { get => _latency; private set => SetProperty(ref _latency, value); }

    public string Tick { get => _tick; private set => SetProperty(ref _tick, value); }

    public string Failures { get => _failures; private set => SetProperty(ref _failures, value); }

    public string Timer { get => _timer; private set => SetProperty(ref _timer, value); }

    public void Refresh()
    {
        Latency = Format(loop.Latency.Snapshot(), "F3");
        Tick = Format(loop.TickIntervals.Snapshot(), "F2");
        Failures = loop.SendFailures.ToString();
        Timer = loop.HighResolutionTimer ? "사용" : "사용 안 함(대체 방식)";
    }

    private static string Format(LatencyStats s, string f) => s.Count == 0
        ? "기록 없음"
        : $"p50 {s.P50Ms.ToString(f)} ms · p95 {s.P95Ms.ToString(f)} ms · p99 {s.P99Ms.ToString(f)} ms · 최대 {s.MaxMs.ToString(f)} ms (최근 {s.Count}개)";
}
