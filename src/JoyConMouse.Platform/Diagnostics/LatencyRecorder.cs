using System.Diagnostics;

namespace JoyConMouse.Platform.Diagnostics;

public readonly record struct LatencyStats(int Count, double P50Ms, double P95Ms, double P99Ms, double MaxMs);

/// <summary>최근 N개의 시간 간격(Stopwatch tick)을 보관하고 백분위수를 계산한다.</summary>
public sealed class LatencyRecorder(int capacity = 2000)
{
    private readonly long[] _buffer = new long[capacity];
    private readonly Lock _gate = new();
    private int _count;
    private int _next;

    public void Add(long ticks)
    {
        lock (_gate)
        {
            _buffer[_next] = ticks;
            _next = (_next + 1) % _buffer.Length;
            if (_count < _buffer.Length)
            {
                _count++;
            }
        }
    }

    public LatencyStats Snapshot()
    {
        long[] copy;
        lock (_gate)
        {
            copy = _buffer.AsSpan(0, _count).ToArray();
        }

        if (copy.Length == 0)
        {
            return default;
        }

        Array.Sort(copy);
        double ToMs(long t) => t * 1000.0 / Stopwatch.Frequency;
        double P(double q) => ToMs(copy[Math.Min(copy.Length - 1, (int)Math.Ceiling(q * copy.Length) - 1)]);
        return new LatencyStats(copy.Length, P(0.50), P(0.95), P(0.99), ToMs(copy[^1]));
    }
}
