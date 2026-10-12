using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Tile;

internal static class NativeChecks
{
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern IntPtr Style(IntPtr handle, int index);
    [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr handle, uint command);
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr handle);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr handle);
    public static void Run(Action<bool, string> check)
    {
        // Test only our own invisible HWNDs. No desktop reparenting, input injection,
        // pointer movement or interaction with any running application.
        using var parent = new HwndSource(new HwndSourceParameters("Tile invisible test host") {
            WindowStyle = 0, ExtendedWindowStyle = NativeWindowPolicy.ToolWindow,
            PositionX = 100, PositionY = 80, Width = 800, Height = 600,
        });
        var root = new Border { Background = Brushes.SlateGray };
        IntPtr handle;
        using (var child = new DesktopSurface(parent.Handle, root, "Tile invisible test card", 192, 192)) {
            handle = child.Handle;
            long style = Style(handle, -16).ToInt64(), extended = Style(handle, -20).ToInt64();
            check((style & 0x40000000) != 0 && DesktopLayer.GetParent(handle) == parent.Handle, "native card is a child of the supplied host from creation");
            check((extended & NativeWindowPolicy.ToolWindow) != 0 && (extended & NativeWindowPolicy.AppWindow) == 0, "native card is excluded from Alt+Tab and taskbar");
            check((extended & NativeWindowPolicy.Layered) != 0, "child host supports per-pixel transparency");
            check(GetWindow(handle, 4) == IntPtr.Zero, "native card has no hidden WPF Window owner");
            check(!IsWindowVisible(parent.Handle) && !IsWindowVisible(handle), "integration test stays invisible");
            var origin = DesktopLayer.Bounds(handle);
            check(DesktopLayer.Move(handle, origin.Left + 73, origin.Top + 41), "native child move returns success");
            var moved = DesktopLayer.Bounds(handle);
            check(moved.Left == origin.Left + 73 && moved.Top == origin.Top + 41, "screen-space drag maps correctly into parent-client coordinates");
        }
        check(!IsWindow(handle), "disposing a card destroys its native handle");
    }
}
