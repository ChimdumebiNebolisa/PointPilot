using System.Runtime.InteropServices;

namespace PointPilot.Infrastructure.Windows;

internal static class NativeMethods
{
    internal const uint MouseMove = 0x0001, MouseLeftDown = 0x0002, MouseLeftUp = 0x0004, MouseRightDown = 0x0008, MouseRightUp = 0x0010, MouseWheel = 0x0800, MouseHWheel = 0x01000;
    internal const uint KeyUp = 0x0002, KeyUnicode = 0x0004;

    [DllImport("user32.dll")] internal static extern nint GetForegroundWindow();
    [DllImport("user32.dll", SetLastError = true)] internal static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);
    [DllImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetWindowRect(nint hWnd, out Rect rect);
    [DllImport("user32.dll", SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)] internal static extern bool PrintWindow(nint hwnd, nint hdc, uint flags);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] internal static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] internal static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll", SetLastError = true)] internal static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll")] internal static extern short VkKeyScan(char value);

    [StructLayout(LayoutKind.Sequential)] internal struct Rect { internal int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] internal struct Point { internal int X, Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct Input { internal uint Type; internal InputUnion Data; }
    [StructLayout(LayoutKind.Explicit)] internal struct InputUnion { [FieldOffset(0)] internal MouseInput Mouse; [FieldOffset(0)] internal KeyboardInput Keyboard; }
    [StructLayout(LayoutKind.Sequential)] internal struct MouseInput { internal int Dx, Dy; internal uint MouseData, Flags, Time; internal nuint ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] internal struct KeyboardInput { internal ushort VirtualKey, ScanCode; internal uint Flags, Time; internal nuint ExtraInfo; }
}
