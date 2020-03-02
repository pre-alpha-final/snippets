#based on https://stackoverflow.com/a/48967155
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public class Keyboard
{
    public void Send(short scanCode)
    {
        Send(scanCode, true);
        Send(scanCode, false);
    }

    private void Send(short scanCode, bool down)
    {
        var inputs = new Input[1];
        var input = new Input { type = 1 };
        input.U.ki.wScan = scanCode;
        input.U.ki.dwFlags = (uint)(down ? 8 : 10);
        if (scanCode < 0)
        {
            input.U.ki.dwFlags++;
        }
        inputs[0] = input;
        SendInput(1, inputs, Input.Size);
    }

    [DllImport("user32.dll")]
    internal static extern uint SendInput(
        uint nInputs,
        [MarshalAs(UnmanagedType.LPArray), In] Input[] pInputs,
        int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    public struct Input
    {
        public uint type;
        public InputUnion U;
        public static int Size
        {
            get { return Marshal.SizeOf(typeof(Input)); }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)]
        internal MouseInput mi;
        [FieldOffset(0)]
        internal KeybdInput ki;
        [FieldOffset(0)]
        internal HardwareInput hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MouseInput
    {
        internal int dx;
        internal int dy;
        internal uint mouseData;
        internal uint dwFlags;
        internal uint time;
        internal UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KeybdInput
    {
        internal short wVk;
        internal short wScan;
        internal uint dwFlags;
        internal int time;
        internal UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HardwareInput
    {
        internal int uMsg;
        internal short wParamL;
        internal short wParamH;
    }
}
'@

[Keyboard]::new().Send(35)
[Keyboard]::new().Send(18)
[Keyboard]::new().Send(38)
[Keyboard]::new().Send(38)
[Keyboard]::new().Send(24)
