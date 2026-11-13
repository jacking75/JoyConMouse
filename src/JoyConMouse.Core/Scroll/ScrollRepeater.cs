using JoyConMouse.Core.Model;

namespace JoyConMouse.Core.Scroll;

/// <summary>
/// 스크롤 버튼의 눌림 상태로 휠 명령을 만든다. 축(세로·가로)마다 독립적으로
/// 즉시 1회 → 초기 지연 → 반복 간격으로 출력하고, 같은 축 반대 방향이 함께 눌리면 멈춘다.
/// </summary>
public sealed class ScrollRepeater(long ticksPerSecond)
{
    private AxisState _vertical, _horizontal;

    public void Reset()
    {
        _vertical = default;
        _horizontal = default;
    }

    /// <param name="up">위로 스크롤 버튼이 눌려 있음</param>
    /// <param name="down">아래로</param>
    /// <param name="left">왼쪽으로</param>
    /// <param name="right">오른쪽으로</param>
    public void Update(bool up, bool down, bool left, bool right, long now, ScrollSettings s, List<MouseCommand> output)
    {
        long initial = s.InitialDelayMs * ticksPerSecond / 1000;
        long interval = Math.Max(1, s.RepeatIntervalMs * ticksPerSecond / 1000);

        int v = _vertical.Step(up, down, now, initial, interval);
        if (v != 0)
        {
            output.Add(new MouseCommand.Wheel(WheelAxis.Vertical, v * s.StepDelta));
        }

        int h = _horizontal.Step(right, left, now, initial, interval);
        if (h != 0)
        {
            output.Add(new MouseCommand.Wheel(WheelAxis.Horizontal, h * s.StepDelta));
        }
    }

    private enum Phase { Idle, Holding, Blocked }

    private struct AxisState
    {
        private Phase _phase;
        private int _direction; // +1: 위/오른쪽, -1: 아래/왼쪽
        private long _nextFireAt;

        /// <returns>이번 틱에 낼 방향(+1, -1) 또는 0</returns>
        public int Step(bool positive, bool negative, long now, long initial, long interval)
        {
            if (positive && negative)
            {
                _phase = Phase.Blocked;
                return 0;
            }

            int held = positive ? 1 : negative ? -1 : 0;
            if (held == 0)
            {
                _phase = Phase.Idle;
                return 0;
            }

            switch (_phase)
            {
                case Phase.Idle:
                    return Start(held, now, initial, fire: true);

                case Phase.Blocked:
                    // 반대 방향을 뗀 직후에는 바로 내지 않고 초기 지연 뒤 반복한다(의도치 않은 한 칸 방지).
                    Start(held, now, initial, fire: false);
                    return 0;

                default: // Holding
                    if (held != _direction)
                    {
                        return Start(held, now, initial, fire: true);
                    }

                    if (now < _nextFireAt)
                    {
                        return 0;
                    }

                    _nextFireAt += interval;
                    if (_nextFireAt <= now)
                    {
                        _nextFireAt = now + interval; // 틱이 늦어도 몰아서 내지 않는다.
                    }

                    return _direction;
            }
        }

        private int Start(int direction, long now, long initial, bool fire)
        {
            _phase = Phase.Holding;
            _direction = direction;
            _nextFireAt = now + initial;
            return fire ? direction : 0;
        }
    }
}
