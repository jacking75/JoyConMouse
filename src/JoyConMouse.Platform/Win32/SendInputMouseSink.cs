using System.Runtime.InteropServices;
using JoyConMouse.Core.Model;
using JoyConMouse.Platform.Diagnostics;
using static JoyConMouse.Platform.Win32.NativeMethods;

namespace JoyConMouse.Platform.Win32;

/// <summary>마우스 명령을 Win32 SendInput 으로 보낸다. 한 틱의 명령은 한 번의 호출로 묶는다.</summary>
public sealed unsafe class SendInputMouseSink : IMouseSink
{
    private const int MaxBatch = 64;
    private static readonly int InputSize = Marshal.SizeOf<INPUT>();

    private readonly Dictionary<int, DateTime> _lastLogged = [];
    private long _failures;

    static SendInputMouseSink()
    {
        if (Environment.Is64BitProcess && InputSize != 40)
        {
            throw new InvalidOperationException($"INPUT 구조체 크기가 40이 아닙니다: {InputSize}");
        }
    }

    /// <summary>SendInput 이 요청보다 적게 넣은 횟수(UIPI 차단 등).</summary>
    public long Failures => Interlocked.Read(ref _failures);

    public void Send(IReadOnlyList<MouseCommand> commands)
    {
        if (commands.Count == 0)
        {
            return;
        }

        int count = Math.Min(commands.Count, MaxBatch);
        INPUT* inputs = stackalloc INPUT[count];
        for (int i = 0; i < count; i++)
        {
            inputs[i] = ToInput(commands[i]);
        }

        uint sent = SendInput((uint)count, inputs, InputSize);
        if (sent < count)
        {
            int err = Marshal.GetLastPInvokeError();
            Interlocked.Increment(ref _failures);
            var now = DateTime.UtcNow;
            if (!_lastLogged.TryGetValue(err, out var last) || now - last > TimeSpan.FromSeconds(10))
            {
                _lastLogged[err] = now;
                Log.Warn($"SendInput 이 {count}개 중 {sent}개만 넣음. GetLastError={err} (관리자 권한 창 등 UIPI 차단일 수 있음)");
            }
        }
    }

    private static INPUT ToInput(MouseCommand c)
    {
        var mi = new MOUSEINPUT();
        switch (c)
        {
            case MouseCommand.Move m:
                mi.dx = m.Dx;
                mi.dy = m.Dy;
                mi.dwFlags = MOUSEEVENTF_MOVE;
                break;
            case MouseCommand.Button b:
                mi.dwFlags = b.Which switch
                {
                    MouseButton.Left => b.Down ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP,
                    MouseButton.Right => b.Down ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP,
                    _ => b.Down ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP,
                };
                break;
            case MouseCommand.Wheel w:
                mi.mouseData = unchecked((uint)w.Delta);
                mi.dwFlags = w.Axis == WheelAxis.Vertical ? MOUSEEVENTF_WHEEL : MOUSEEVENTF_HWHEEL;
                break;
        }

        return new INPUT { type = INPUT_MOUSE, mi = mi };
    }
}
