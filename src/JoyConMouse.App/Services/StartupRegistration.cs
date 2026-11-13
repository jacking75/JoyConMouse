using JoyConMouse.Platform.Diagnostics;
#if STORE
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
#else
using Microsoft.Win32;
#endif

namespace JoyConMouse.App.Services;

#if STORE
/// <summary>
/// Microsoft Store(MSIX) 빌드의 시작 프로그램 등록. 패키지 앱은 Run 키를 쓸 수 없으므로
/// 매니페스트의 windows.startupTask(TaskId=JoyConMouseStartup)를 켜고 끈다.
/// </summary>
public static class StartupRegistration
{
    private const string TaskId = "JoyConMouseStartup";
    private static StartupTask? _task;

    public static async Task InitializeAsync()
    {
        try
        {
            _task = await StartupTask.GetAsync(TaskId);
        }
        catch (Exception ex)
        {
            Log.Error("StartupTask 조회 실패", ex);
        }
    }

    public static bool IsRegistered => _task?.State is StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy;

    /// <summary>로그인 시 StartupTask 로 실행되었는지(이때는 창 없이 트레이로 시작한다).</summary>
    public static bool LaunchedAtStartup()
    {
        try
        {
            return AppInstance.GetActivatedEventArgs()?.Kind == ActivationKind.StartupTask;
        }
        catch (Exception ex)
        {
            Log.Warn("활성화 정보 조회 실패: " + ex.Message);
            return false;
        }
    }

    /// <returns>사용자에게 알릴 문구(성공이면 null).</returns>
    public static async Task<string?> SetAsync(bool enabled)
    {
        if (_task is null)
        {
            return Strings.Get("Startup.Unavailable");
        }

        if (!enabled)
        {
            _task.Disable();
            Log.Info("시작 프로그램 해제(StartupTask)");
            return null;
        }

        var state = await _task.RequestEnableAsync();
        Log.Info($"시작 프로그램 등록 요청(StartupTask): {state}");
        return state switch
        {
            StartupTaskState.Enabled or StartupTaskState.EnabledByPolicy => null,
            StartupTaskState.DisabledByUser => Strings.Get("Startup.DisabledByUser"),
            _ => Strings.Get("Startup.DisabledByPolicy"),
        };
    }
}
#else
/// <summary>HKCU\...\Run 의 시작 프로그램 등록. 상태는 파일이 아니라 레지스트리 값을 그대로 읽는다.</summary>
public static class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "JoyConMouse";

    public static string ExpectedCommand => $"\"{Environment.ProcessPath}\" --tray";

    public static Task InitializeAsync() => Task.CompletedTask;

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

    /// <summary>zip 배포판은 --tray 인자로 시작 여부를 판단한다.</summary>
    public static bool LaunchedAtStartup() => false;

    /// <returns>사용자에게 알릴 문구(성공이면 null).</returns>
    public static Task<string?> SetAsync(bool enabled)
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
            return Task.FromResult<string?>(null);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or System.IO.IOException)
        {
            Log.Error("시작 프로그램 설정 실패", ex);
            return Task.FromResult<string?>(Strings.Get("Startup.Unavailable"));
        }
    }
}
#endif
