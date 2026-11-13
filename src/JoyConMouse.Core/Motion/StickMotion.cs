namespace JoyConMouse.Core.Motion;

/// <summary>스틱 기울기를 커서 상대 이동량으로 바꾼다. 소수 잔여량을 누적해 작은 움직임이 사라지지 않게 한다.</summary>
public sealed class StickMotion
{
    private double _remX, _remY;

    public void Reset() => (_remX, _remY) = (0, 0);

    /// <param name="x">세로 기준 X(오른쪽 +), [-1, 1]</param>
    /// <param name="y">세로 기준 Y(아래 +), [-1, 1]</param>
    /// <param name="dt">직전 틱 이후 경과 초</param>
    public (int Dx, int Dy) Step(float x, float y, double dt, StickSettings s)
    {
        if (dt <= 0 || dt > StickSettings.MaxGapSeconds)
        {
            Reset(); // 오래 멈췄다 복귀할 때 튀지 않게 한다.
            return (0, 0);
        }

        double mag = Math.Sqrt((double)x * x + (double)y * y);
        if (mag <= s.Deadzone)
        {
            Reset(); // 원형 데드존. 잔여량도 버려 놓은 뒤 커서가 밀리지 않게 한다.
            return (0, 0);
        }

        double ux = x / mag, uy = y / mag;
        if (mag > 1)
        {
            mag = 1; // 대각선 과속 방지
        }

        double t = (mag - s.Deadzone) / (1 - s.Deadzone);
        double speed = s.MaxSpeed * Math.Pow(t, s.CurveExponent);
        _remX += ux * speed * dt;
        _remY += uy * speed * dt;
        int dx = (int)Math.Truncate(_remX);
        _remX -= dx;
        int dy = (int)Math.Truncate(_remY);
        _remY -= dy;
        return (dx, dy);
    }
}
