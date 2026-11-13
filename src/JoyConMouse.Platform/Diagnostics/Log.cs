using System.Text;

namespace JoyConMouse.Platform.Diagnostics;

/// <summary>
/// 경량 파일 로거. %LOCALAPPDATA%\JoyConMouse\logs\joyconmouse-yyyyMMdd.log 에 쓴다.
/// 장치 시리얼·Bluetooth 주소·장치 경로는 기록하지 않는다(호출하는 쪽 규칙).
/// </summary>
public static class Log
{
    private const int KeepDays = 7;
    private static readonly Lock Gate = new();
    private static StreamWriter? _writer;
    private static string? _currentDate;

    public static string Directory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JoyConMouse", "logs");

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex is null ? message : $"{message}: {ex}");

    private static void Write(string level, string message)
    {
        var now = DateTime.Now;
        string line = $"{now:yyyy-MM-dd HH:mm:ss.fff} [{level}] [{Environment.CurrentManagedThreadId}] {message}";
        lock (Gate)
        {
            try
            {
                string date = now.ToString("yyyyMMdd");
                if (_writer is null || _currentDate != date)
                {
                    _writer?.Dispose();
                    System.IO.Directory.CreateDirectory(Directory);
                    var fs = new FileStream(Path.Combine(Directory, $"joyconmouse-{date}.log"),
                        FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                    _writer = new StreamWriter(fs, new UTF8Encoding(false)) { AutoFlush = true };
                    _currentDate = date;
                    DeleteOldFiles(now);
                }

                _writer.WriteLine(line);
            }
            catch (IOException)
            {
                // 로그 실패로 앱이 멈추지 않게 한다.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static void DeleteOldFiles(DateTime now)
    {
        foreach (var f in new DirectoryInfo(Directory).GetFiles("joyconmouse-*.log"))
        {
            if (f.LastWriteTime < now.AddDays(-KeepDays))
            {
                try
                {
                    f.Delete();
                }
                catch (IOException)
                {
                }
            }
        }
    }

    /// <summary>최근 로그 파일 경로(진단 내보내기용).</summary>
    public static IEnumerable<string> RecentFiles(int count) =>
        System.IO.Directory.Exists(Directory)
            ? new DirectoryInfo(Directory).GetFiles("joyconmouse-*.log")
                .OrderByDescending(f => f.LastWriteTime).Take(count).Select(f => f.FullName)
            : [];

    public static void Flush()
    {
        lock (Gate)
        {
            _writer?.Flush();
        }
    }
}
