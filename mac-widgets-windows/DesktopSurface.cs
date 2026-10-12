using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Tile;

// A real child HWND from birth. WPF Window.ShowInTaskbar creates hidden owner
// windows and assumes top-level coordinates; neither belongs on the desktop.
public sealed class DesktopSurface : IDisposable
{
    public HwndSource Source { get; }
    public IntPtr Handle => Source.Handle;
    public DesktopSurface(IntPtr parent, FrameworkElement content, string title, double width, double height)
    {
        if (parent == IntPtr.Zero) throw new ArgumentException("A desktop or isolated test parent is required.", nameof(parent));
        double scale = DesktopLayer.Scale(parent);
        var parameters = new HwndSourceParameters(title) {
            ParentWindow = parent,
            WindowStyle = NativeWindowPolicy.ChildStyle,
            ExtendedWindowStyle = NativeWindowPolicy.ExtendedStyle,
            PositionX = 0, PositionY = 0,
            Width = (int)Math.Ceiling(width * scale), Height = (int)Math.Ceiling(height * scale),
            UsesPerPixelTransparency = true,
            TreatAsInputRoot = true,
        };
        Source = new HwndSource(parameters) { SizeToContent = SizeToContent.Manual };
        Source.CompositionTarget.BackgroundColor = Colors.Transparent;
        content.Width = width; content.Height = height;
        Source.RootVisual = content;
    }
    public void Show() => DesktopLayer.Show(Handle);
    public void Dispose() { Source.RootVisual = null; Source.Dispose(); }
}
