using System.IO;
using System.Text;
using JoyConMouse.Core.Settings;
using JoyConMouse.Platform.Diagnostics;

namespace JoyConMouse.App.Services;

/// <summary>
/// %APPDATA%\JoyConMouse\settings.json 입출력. 변경은 500ms 디바운스 후 임시 파일 + File.Replace 로 원자적으로 저장한다.
/// </summary>
public sealed class SettingsStore : IDisposable
{
    private static readonly TimeSpan Debounce = TimeSpan.FromMilliseconds(500);

    private readonly Lock _gate = new();
    private readonly Lock _writeGate = new(); // 디바운스 타이머와 종료 시 Flush 가 같은 임시 파일에 동시에 쓰지 않게 한다.
    private readonly Timer _timer;
    private bool _dirty;

    private SettingsStore(string path, SettingsLoadResult load, string? brokenBackup)
    {
        FilePath = path;
        Current = load.Settings;
        LoadStatus = load.Status;
        BrokenBackupPath = brokenBackup;
        _timer = new Timer(_ => Flush(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JoyConMouse", "settings.json");

    public string FilePath { get; }

    public AppSettings Current { get; private set; }

    public SettingsLoadStatus LoadStatus { get; }

    /// <summary>새 버전 앱이 만든 파일이면 덮어쓰지 않는다.</summary>
    public bool ReadOnly => LoadStatus == SettingsLoadStatus.NewerVersion;

    /// <summary>손상된 파일을 옮겨 둔 경로(있을 때만).</summary>
    public string? BrokenBackupPath { get; }

    public static SettingsStore Load(string? path = null)
    {
        path ??= DefaultPath;
        string? json = null;
        try
        {
            if (File.Exists(path))
            {
                json = File.ReadAllText(path, Encoding.UTF8);
            }
        }
        catch (IOException ex)
        {
            Log.Error("설정 파일 읽기 실패", ex);
        }

        var result = SettingsSerializer.Deserialize(json);
        foreach (var w in result.Warnings)
        {
            Log.Warn(w);
        }

        string? backup = null;
        if (result.Status == SettingsLoadStatus.Corrupt)
        {
            backup = $"{path}.broken-{DateTime.Now:yyyyMMddHHmmss}";
            try
            {
                File.Move(path, backup);
                Log.Warn($"손상된 설정 파일을 옮김: {Path.GetFileName(backup)}");
            }
            catch (IOException ex)
            {
                Log.Error("손상된 설정 파일 이동 실패", ex);
                backup = null;
            }
        }
        else if (result.Status == SettingsLoadStatus.NewerVersion)
        {
            Log.Warn($"새 버전 앱이 만든 설정 파일(version={result.Settings.Version}). 읽기 전용으로 사용합니다.");
        }

        return new SettingsStore(path, result, backup);
    }

    public void Update(Func<AppSettings, AppSettings> change)
    {
        lock (_gate)
        {
            Current = change(Current);
            _dirty = true;
            _timer.Change(Debounce, Timeout.InfiniteTimeSpan);
        }
    }

    public void Flush()
    {
        lock (_writeGate)
        {
            FlushCore();
        }
    }

    private void FlushCore()
    {
        string json;
        lock (_gate)
        {
            if (!_dirty || ReadOnly)
            {
                return;
            }

            _dirty = false;
            json = SettingsSerializer.Serialize(Current);
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, json, new UTF8Encoding(false));
            if (File.Exists(FilePath))
            {
                File.Replace(tmp, FilePath, null);
            }
            else
            {
                File.Move(tmp, FilePath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error("설정 저장 실패", ex);
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        Flush();
    }
}
