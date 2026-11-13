using JoyConMouse.Core.Model;

namespace JoyConMouse.Core.Engine;

public enum SelectionOutcome
{
    /// <summary>연결된 조이콘이 없다.</summary>
    NoDevices,

    /// <summary>자동으로 고를 수 있다.</summary>
    Selected,

    /// <summary>자동으로 고를 수 없어 사용자 선택이 필요하다.</summary>
    NeedsChoice,
}

/// <summary>앱 시작·장치 목록 변경 시 활성 장치를 자동으로 고르는 규칙.</summary>
public static class DeviceSelector
{
    public static (SelectionOutcome Outcome, DeviceKey? Device) Select(DeviceKey? saved, IReadOnlyCollection<DeviceKey> connected)
    {
        if (connected.Count == 0)
        {
            return (SelectionOutcome.NoDevices, null);
        }

        // 규칙 1: 저장된 장치가 연결되어 있으면 그것.
        if (saved is { } s && connected.Contains(s))
        {
            return (SelectionOutcome.Selected, s);
        }

        // 규칙 2: 저장값이 없고 정확히 1대만 연결되어 있으면 그것.
        if (saved is null && connected.Count == 1)
        {
            return (SelectionOutcome.Selected, connected.First());
        }

        // 규칙 3: 그 밖에는 사용자에게 묻는다.
        return (SelectionOutcome.NeedsChoice, null);
    }
}
