namespace JoyConMouse.Core.Motion;

/// <summary>스틱 커서 이동 설정. 범위를 벗어난 값은 <see cref="Clamped"/> 로 자른다.</summary>
public sealed record StickSettings
{
    public const double MaxSpeedMin = 200, MaxSpeedMax = 3000, MaxSpeedDefault = 1000;
    public const double DeadzoneMin = 0.05, DeadzoneMax = 0.35, DeadzoneDefault = 0.15;
    public const double CurveMin = 1.0, CurveMax = 3.5, CurveDefault = 2.0;

    /// <summary>이 시간보다 긴 틱 간격은 멈췄다 복귀한 것으로 보고 이동하지 않는다.</summary>
    public const double MaxGapSeconds = 0.1;

    /// <summary>스틱 감도: 최대 기울기에서 초당 상대 이동 단위.</summary>
    public double MaxSpeed { get; init; } = MaxSpeedDefault;

    /// <summary>원형 데드존 반지름(0~1).</summary>
    public double Deadzone { get; init; } = DeadzoneDefault;

    /// <summary>미세 이동 정도. 클수록 작은 기울기에서 느리다.</summary>
    public double CurveExponent { get; init; } = CurveDefault;

    public static StickSettings Default { get; } = new();

    public StickSettings Clamped() => new()
    {
        MaxSpeed = Clamp(MaxSpeed, MaxSpeedMin, MaxSpeedMax, MaxSpeedDefault),
        Deadzone = Clamp(Deadzone, DeadzoneMin, DeadzoneMax, DeadzoneDefault),
        CurveExponent = Clamp(CurveExponent, CurveMin, CurveMax, CurveDefault),
    };

    private static double Clamp(double v, double min, double max, double fallback) =>
        double.IsFinite(v) ? Math.Clamp(v, min, max) : fallback;
}
