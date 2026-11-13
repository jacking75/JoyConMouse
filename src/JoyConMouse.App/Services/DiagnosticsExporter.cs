using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using JoyConMouse.Platform;
using JoyConMouse.Platform.Diagnostics;
using JoyConMouse.Platform.Sdl;

namespace JoyConMouse.App.Services;

/// <summary>
/// 사용자가 요청할 때만 바탕화면에 진단 zip 을 만든다.
/// 원본 시리얼·Bluetooth 주소·장치 경로는 로그·상태 어디에도 기록하지 않으므로 포함되지 않는다.
/// </summary>
public static class DiagnosticsExporter
{
    public static string Export(InputLoop loop, EngineStatus status, SettingsStore settings)
    {
        settings.Flush();
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string zipPath = Path.Combine(desktop, $"JoyConMouse-진단-{DateTime.Now:yyyyMMdd-HHmmss}.zip");

        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        AddText(zip, "info.txt", BuildInfo(loop, status));

        if (File.Exists(settings.FilePath))
        {
            zip.CreateEntryFromFile(settings.FilePath, "settings.json");
        }

        Log.Flush();
        foreach (var f in Log.RecentFiles(3))
        {
            // 로거가 파일을 열고 있으므로 공유 모드로 읽어 복사한다.
            using var src = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var entry = zip.CreateEntry("logs/" + Path.GetFileName(f));
            using var dst = entry.Open();
            src.CopyTo(dst);
        }

        Log.Info($"진단 정보 내보냄: {Path.GetFileName(zipPath)}");
        return zipPath;
    }

    public static string BuildInfo(InputLoop loop, EngineStatus status)
    {
        var sb = new StringBuilder();
        var asm = Assembly.GetEntryAssembly();
        sb.AppendLine($"앱 버전: {asm?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? asm?.GetName().Version?.ToString()}");
        sb.AppendLine($"OS: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
        sb.AppendLine($".NET: {RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"SDL: {status.SdlVersion}");
        sb.AppendLine($"작성 시각: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        sb.AppendLine();
        sb.AppendLine($"엔진 상태: {status.State}, 일시정지 사유: {status.Pause}, 선택 필요: {status.NeedsChoice}");
        sb.AppendLine($"활성 장치: {(status.ActiveDevice is { } k ? $"{k.Side} {k.Id}" : "없음")}");
        if (status.Error is not null)
        {
            sb.AppendLine($"오류: {status.Error}");
        }

        sb.AppendLine("연결된 조이콘:");
        foreach (var d in status.Devices)
        {
            sb.AppendLine($"  - {d.Key.Side} id={d.Key.Id} 이름={d.Name} 배터리={d.Battery}({(d.BatteryPercent >= 0 ? d.BatteryPercent + "%" : "퍼센트 없음")}) 전원={d.Power}");
        }

        sb.AppendLine();
        var lat = loop.Latency.Snapshot();
        var tick = loop.TickIntervals.Snapshot();
        sb.AppendLine($"내부 지연(스냅샷→SendInput 반환, 최근 {lat.Count}개): p50={lat.P50Ms:F3}ms p95={lat.P95Ms:F3}ms p99={lat.P99Ms:F3}ms 최대={lat.MaxMs:F3}ms");
        sb.AppendLine($"틱 간격(최근 {tick.Count}개): p50={tick.P50Ms:F2}ms p95={tick.P95Ms:F2}ms p99={tick.P99Ms:F2}ms 최대={tick.MaxMs:F2}ms");
        sb.AppendLine($"고해상도 타이머: {loop.HighResolutionTimer}");
        sb.AppendLine($"SendInput 실패 횟수: {loop.SendFailures}");
        return sb.ToString();
    }

    private static void AddText(ZipArchive zip, string name, string text)
    {
        var entry = zip.CreateEntry(name);
        using var w = new StreamWriter(entry.Open(), new UTF8Encoding(true));
        w.Write(text);
    }
}
