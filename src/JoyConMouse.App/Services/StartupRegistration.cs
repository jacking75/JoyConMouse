using JoyConMouse.Platform.Diagnostics;
using Microsoft.Win32;

namespace JoyConMouse.App.Services;

/// <summary>HKCU\...\Run 의 시작 프로그램 등록. 상태는 파일이 아니라 레지스트리 값을 그대로 읽는다.</summary>
public static class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "JoyConMouse";

    public static string ExpectedCommand => $"\"{Environment.ProcessPath}\" --tray";

    /// <summary>값이 있고 현재 exe 경로와 일치할 때만 등록된 것으로 본다(폴더를 옮기면 해제로 표시).</summary>
    public static bool IsRegistered
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string v
                   && string.Equals(v.Trim(), ExpectedCommand, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void Set(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (enabled)
            {
                key.SetValue(ValueName, ExpectedCommand, RegistryValueKind.String);
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }

            Log.Info($"시작 프로그램 {(enabled ? "등록" : "해제")}");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or System.IO.IOException)
        {
            Log.Error("시작 프로그램 설정 실패", ex);
        }
    }
}
