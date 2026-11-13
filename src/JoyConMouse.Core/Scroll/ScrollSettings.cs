namespace JoyConMouse.Core.Scroll;

/// <summary>버튼 스크롤 설정. 범위를 벗어난 값은 <see cref="Clamped"/> 로 자른다.</summary>
public sealed record ScrollSettings
{
    public const int StepDeltaMin = 30, StepDeltaMax = 480, StepDeltaDefault = 120;
    public const int InitialDelayMin = 150, InitialDelayMax = 1000, InitialDelayDefault = 300;
    public const int RepeatIntervalMin = 30, RepeatIntervalMax = 300, RepeatIntervalDefault = 100;

    /// <summary>1회당 휠 단위(WHEEL_DELTA=120 이 한 칸).</summary>
    public int StepDelta { get; init; } = StepDeltaDefault;

    /// <summary>길게 눌렀을 때 반복을 시작하기까지의 지연(ms).</summary>
    public int InitialDelayMs { get; init; } = InitialDelayDefault;

    /// <summary>반복 간격(ms).</summary>
    public int RepeatIntervalMs { get; init; } = RepeatIntervalDefault;

    public static ScrollSettings Default { get; } = new();

    public ScrollSettings Clamped() => new()
    {
        StepDelta = Math.Clamp(StepDelta, StepDeltaMin, StepDeltaMax),
        InitialDelayMs = Math.Clamp(InitialDelayMs, InitialDelayMin, InitialDelayMax),
        RepeatIntervalMs = Math.Clamp(RepeatIntervalMs, RepeatIntervalMin, RepeatIntervalMax),
    };
}
