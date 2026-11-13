using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using JoyConMouse.Core.Buttons;
using JoyConMouse.Core.Model;
using JoyConMouse.Core.Motion;
using JoyConMouse.Core.Scroll;

namespace JoyConMouse.Core.Settings;

public enum SettingsLoadStatus
{
    /// <summary>정상 로드.</summary>
    Ok,

    /// <summary>파일 없음. 기본값.</summary>
    Missing,

    /// <summary>JSON 이 깨짐. 기본값.</summary>
    Corrupt,

    /// <summary>새 버전 앱이 만든 파일. 읽기만 하고 덮어쓰지 않는다.</summary>
    NewerVersion,
}

public sealed record SettingsLoadResult(AppSettings Settings, SettingsLoadStatus Status, IReadOnlyList<string> Warnings);

/// <summary>
/// settings.json 직렬화. 열거형은 문자열이고, 모르는 버튼·동작 이름은 무시하고 경고로 돌려준다.
/// 범위를 벗어난 값은 범위로 자른다.
/// </summary>
public static class SettingsSerializer
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Serialize(AppSettings s)
    {
        var root = new JsonObject
        {
            ["version"] = s.Version < AppSettings.CurrentVersion ? AppSettings.CurrentVersion : s.Version,
            ["lastActiveDevice"] = s.LastActiveDevice is { } k
                ? new JsonObject { ["side"] = k.Side.ToString(), ["id"] = k.Id }
                : null,
            ["startPaused"] = s.StartPaused,
            ["trayNoticeShown"] = s.TrayNoticeShown,
            ["profiles"] = new JsonObject
            {
                ["Left"] = WriteProfile(s.Left, JoyConSide.Left),
                ["Right"] = WriteProfile(s.Right, JoyConSide.Right),
            },
        };
        return root.ToJsonString(WriteOptions);
    }

    public static SettingsLoadResult Deserialize(string? json)
    {
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return new SettingsLoadResult(AppSettings.Default, SettingsLoadStatus.Missing, warnings);
        }

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException ex)
        {
            warnings.Add("설정 파일 JSON 오류: " + ex.Message);
            return new SettingsLoadResult(AppSettings.Default, SettingsLoadStatus.Corrupt, warnings);
        }

        if (root is null)
        {
            warnings.Add("설정 파일 최상위가 객체가 아닙니다.");
            return new SettingsLoadResult(AppSettings.Default, SettingsLoadStatus.Corrupt, warnings);
        }

        try
        {
            int version = GetInt(root, "version") ?? AppSettings.CurrentVersion;
            var profiles = root["profiles"] as JsonObject;
            var settings = new AppSettings
            {
                Version = version,
                LastActiveDevice = ReadDevice(root["lastActiveDevice"] as JsonObject, warnings),
                StartPaused = GetBool(root, "startPaused") ?? false,
                TrayNoticeShown = GetBool(root, "trayNoticeShown") ?? false,
                Left = ReadProfile(profiles?["Left"] as JsonObject, JoyConSide.Left, warnings),
                Right = ReadProfile(profiles?["Right"] as JsonObject, JoyConSide.Right, warnings),
            };

            var status = version > AppSettings.CurrentVersion ? SettingsLoadStatus.NewerVersion : SettingsLoadStatus.Ok;
            return new SettingsLoadResult(settings, status, warnings);
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or JsonException)
        {
            warnings.Add("설정 파일 형식 오류: " + ex.Message);
            return new SettingsLoadResult(AppSettings.Default, SettingsLoadStatus.Corrupt, warnings);
        }
    }

    private static JsonObject WriteProfile(JoyConProfile p, JoyConSide side)
    {
        var buttons = new JsonObject();
        foreach (var b in JoyConButtonInfo.ButtonsOf(side))
        {
            var action = p.Buttons[b];
            if (action != MouseAction.None)
            {
                buttons[b.ToString()] = action.ToString();
            }
        }

        return new JsonObject
        {
            ["stick"] = new JsonObject
            {
                ["maxSpeed"] = p.Stick.MaxSpeed,
                ["deadzone"] = p.Stick.Deadzone,
                ["curveExponent"] = p.Stick.CurveExponent,
            },
            ["scroll"] = new JsonObject
            {
                ["stepDelta"] = p.Scroll.StepDelta,
                ["initialDelayMs"] = p.Scroll.InitialDelayMs,
                ["repeatIntervalMs"] = p.Scroll.RepeatIntervalMs,
            },
            ["buttons"] = buttons,
        };
    }

    private static JoyConProfile ReadProfile(JsonObject? o, JoyConSide side, List<string> warnings)
    {
        var def = JoyConProfile.DefaultFor(side);
        if (o is null)
        {
            return def;
        }

        var stick = def.Stick;
        if (o["stick"] is JsonObject st)
        {
            stick = new StickSettings
            {
                MaxSpeed = GetDouble(st, "maxSpeed") ?? stick.MaxSpeed,
                Deadzone = GetDouble(st, "deadzone") ?? stick.Deadzone,
                CurveExponent = GetDouble(st, "curveExponent") ?? stick.CurveExponent,
            };
        }

        var scroll = def.Scroll;
        if (o["scroll"] is JsonObject sc)
        {
            scroll = new ScrollSettings
            {
                StepDelta = GetInt(sc, "stepDelta") ?? scroll.StepDelta,
                InitialDelayMs = GetInt(sc, "initialDelayMs") ?? scroll.InitialDelayMs,
                RepeatIntervalMs = GetInt(sc, "repeatIntervalMs") ?? scroll.RepeatIntervalMs,
            };
        }

        var buttons = def.Buttons;
        if (o["buttons"] is JsonObject bo)
        {
            var valid = JoyConButtonInfo.ButtonsOf(side);
            var map = new Dictionary<JoyConButtons, MouseAction>();
            foreach (var (name, value) in bo)
            {
                if (!Enum.TryParse<JoyConButtons>(name, ignoreCase: false, out var button) || !valid.Contains(button))
                {
                    warnings.Add($"{side} 프로필: 알 수 없는 버튼 '{name}' 무시");
                    continue;
                }

                string? actionName = value is JsonValue jv && jv.TryGetValue(out string? s) ? s : null;
                if (actionName is null || !Enum.TryParse<MouseAction>(actionName, ignoreCase: false, out var action)
                    || !Enum.IsDefined(action))
                {
                    warnings.Add($"{side} 프로필: 버튼 '{name}'의 알 수 없는 동작 '{value}' 무시");
                    continue;
                }

                map[button] = action;
            }

            buttons = new ButtonMap(map);
        }

        return new JoyConProfile(stick, scroll, buttons).Clamped();
    }

    private static DeviceKey? ReadDevice(JsonObject? o, List<string> warnings)
    {
        if (o is null)
        {
            return null;
        }

        string? sideName = GetString(o, "side");
        string? id = GetString(o, "id");
        if (!Enum.TryParse<JoyConSide>(sideName, out var side) || !Enum.IsDefined(side) || string.IsNullOrWhiteSpace(id))
        {
            warnings.Add("lastActiveDevice 형식 오류 무시");
            return null;
        }

        return new DeviceKey(side, id);
    }

    private static string? GetString(JsonObject o, string name) =>
        o[name] is JsonValue v && v.TryGetValue(out string? s) ? s : null;

    private static bool? GetBool(JsonObject o, string name) =>
        o[name] is JsonValue v && v.TryGetValue(out bool b) ? b : null;

    private static double? GetDouble(JsonObject o, string name) =>
        o[name] is JsonValue v && v.TryGetValue(out double d) ? d : null;

    private static int? GetInt(JsonObject o, string name)
    {
        if (o[name] is not JsonValue v)
        {
            return null;
        }

        if (v.TryGetValue(out int i))
        {
            return i;
        }

        return v.TryGetValue(out double d) && double.IsFinite(d)
            ? (int)Math.Clamp(Math.Round(d), int.MinValue, int.MaxValue)
            : null;
    }
}
