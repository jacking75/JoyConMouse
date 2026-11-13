using JoyConMouse.Core.Model;

namespace JoyConMouse.Core.Tests;

public class DeviceIdentityTests
{
    [Fact]
    public void 같은_MAC은_시리얼_형식이_달라도_같은_식별자다()
    {
        // SDL 초기화 성공 시 형식 / Windows HID 시리얼 형식(초기화 실패 시)
        var a = DeviceIdentity.CreateKey(JoyConSide.Right, "98-b6-e9-12-34-ab", null);
        var b = DeviceIdentity.CreateKey(JoyConSide.Right, "98B6E91234AB", null);
        var c = DeviceIdentity.CreateKey(JoyConSide.Right, "98:b6:e9:12:34:ab", null);

        Assert.Equal(a, b);
        Assert.Equal(a, c);
        Assert.NotEqual(a, DeviceIdentity.CreateKey(JoyConSide.Right, "98-b6-e9-12-34-ac", null));
    }
}
