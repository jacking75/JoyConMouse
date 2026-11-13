using System.Security.Cryptography;
using System.Text;

namespace JoyConMouse.Core.Model;

public static class DeviceIdentity
{
    /// <summary>
    /// 시리얼(조이콘은 Bluetooth 주소)이 있으면 그것을, 없으면 장치 경로를 SHA-256 으로 해시해 앞 8바이트를 hex 로 돌려준다.
    /// 원본 값은 저장·로그하지 않기 위해 이 함수 밖으로 내보내지 않는다.
    /// </summary>
    public static DeviceKey CreateKey(JoyConSide side, string? serial, string? path)
    {
        string source = !string.IsNullOrWhiteSpace(serial) ? "s:" + NormalizeSerial(serial)
            : !string.IsNullOrWhiteSpace(path) ? "p:" + path
            : "none";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return new DeviceKey(side, Convert.ToHexStringLower(hash.AsSpan(0, 8)));
    }

    /// <summary>
    /// MAC 주소 형태(12자리 hex, 구분자 '-' ':' 유무·대소문자 무관)면 "aa-bb-cc-dd-ee-ff" 로 맞춘다.
    /// SDL 은 조이콘 초기화에 성공하면 이 형식을 쓰고, 실패하면 Windows 가 준 다른 형식의 시리얼을 그대로 쓰므로
    /// 맞추지 않으면 같은 조이콘의 식별자가 연결 상태에 따라 달라진다.
    /// </summary>
    public static string NormalizeSerial(string serial)
    {
        var hex = new StringBuilder(12);
        foreach (char c in serial.Trim())
        {
            if (char.IsAsciiHexDigit(c))
            {
                hex.Append(char.ToLowerInvariant(c));
            }
            else if (c is not ('-' or ':'))
            {
                return serial; // MAC 형태가 아니면 그대로 쓴다.
            }
        }

        if (hex.Length != 12)
        {
            return serial;
        }

        var sb = new StringBuilder(17);
        for (int i = 0; i < 12; i += 2)
        {
            if (i > 0)
            {
                sb.Append('-');
            }

            sb.Append(hex[i]).Append(hex[i + 1]);
        }

        return sb.ToString();
    }
}
