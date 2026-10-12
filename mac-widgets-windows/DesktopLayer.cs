using System;
using System.Runtime.InteropServices;

namespace Tile;

public static class DesktopLayer
{
    private delegate bool EnumProc(IntPtr window, IntPtr param);
    [StructLayout(LayoutKind.Sequential)] public struct NativePoint { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] public struct NativeRect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string? title);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumProc callback, IntPtr param);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr GetParent(IntPtr window);
    [DllImport("user32.dll")] public static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);
    [DllImport("user32.dll")] private static extern bool ScreenToClient(IntPtr window, ref NativePoint point);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr SetCapture(IntPtr window);
    [DllImport("user32.dll")] public static extern IntPtr GetCapture();
    [DllImport("user32.dll")] public static extern bool ReleaseCapture();
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
    public static IntPtr FindDesktop()
    {
        IntPtr desktop = IntPtr.Zero;
        EnumWindows((h, _) => {
            var view = FindWindowEx(h, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (view != IntPtr.Zero && IsWindowVisible(view)) desktop = view;
            return desktop == IntPtr.Zero;
        }, IntPtr.Zero);
        return desktop;
    }
    public static double Scale(IntPtr handle) { var dpi = GetDpiForWindow(handle); return dpi == 0 ? 1 : dpi / 96.0; }
    public static NativeRect Bounds(IntPtr handle) { GetWindowRect(handle, out var rect); return rect; }
    public static bool Move(IntPtr handle, double screenX, double screenY)
    {
        var point = new NativePoint { X = (int)Math.Round(screenX), Y = (int)Math.Round(screenY) };
        var parent = GetParent(handle);
        if (parent != IntPtr.Zero && !ScreenToClient(parent, ref point)) return false;
        // Position only. Never activate or move above normal applications.
        return SetWindowPos(handle, IntPtr.Zero, point.X, point.Y, 0, 0, 0x0015);
    }
    public static void Show(IntPtr handle) => ShowWindow(handle, 4); // SW_SHOWNOACTIVATE
}
