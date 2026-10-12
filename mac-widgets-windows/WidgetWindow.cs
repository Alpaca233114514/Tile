using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Tile;

// The historical filename is retained, but a card is no longer a WPF Window.
public sealed class WidgetWindow : IDisposable
{
    public WidgetModel Model { get; }
    public WidgetView View { get; }
    public double Width => Model.Width;
    public double Height => Model.Height;
    private readonly App _app;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(30) };
    private readonly DispatcherTimer _dragPoll = new(DispatcherPriority.Input) { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly CancellationTokenSource _cancel = new();
    private DesktopSurface? _surface;
    private HwndSource? _testParent;
    private IntPtr _parent;
    private DragSession? _drag;
    private bool _dragMoved, _busy, _disposed;
    private Weather? _weather;
    private DateTimeOffset _nextWeather;
    private string? _weatherStatus;
    public bool Embedded => _parent != IntPtr.Zero;
    public WidgetWindow(App app, WidgetModel model)
    {
        _app = app; Model = model;
        View = new WidgetView(model, true) { Cursor = Cursors.SizeAll };
        View.ChoosePhoto += () => app.ChoosePhoto(model);
        View.AllowDrop = model.Kind == "photo";
        View.DragOver += (_, e) => { e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; e.Handled = true; };
        View.Drop += (_, e) => { if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0) app.UsePhoto(model, files[0]); e.Handled = true; };
        View.ContextMenu = new ContextMenu();
        void Item(string title, Action action) { var item = new MenuItem { Header = title }; item.Click += (_, _) => action(); View.ContextMenu.Items.Add(item); }
        Item("配置小组件…", () => app.Configure(model));
        if (model.Kind == "photo") Item("选择本地照片…", () => app.ChoosePhoto(model));
        Item("突出显示拖动区域", () => app.SetEditing(true));
        Item("完成排列", () => app.SetEditing(false));
        foreach (var size in new[] { ("small", "小"), ("medium", "中"), ("large", "大") }) Item("尺寸 · " + size.Item2, () => { model.Size = size.Item1; app.Rebuild(); app.Save(); });
        Item("打开组件库", app.ShowManager);
        Item("移除小组件", () => app.Remove(model));
        _timer.Tick += async (_, _) => await Refresh();
        // While a card initiated a drag, track release even if Explorer takes focus.
        // No global hook or generated input: this only reads the user's live gesture.
        _dragPoll.Tick += (_, _) => {
            if (_drag == null) return;
            if ((DesktopLayer.GetAsyncKeyState(0x1B) & 0x8000) != 0) { FinishDrag(false); return; }
            if ((DesktopLayer.GetAsyncKeyState(1) & 0x8000) == 0) { FinishDrag(true); return; }
            MoveDrag();
        };
    }
    public bool TryEmbed() { _parent = DesktopLayer.FindDesktop(); return Embedded; }
    public void Show()
    {
        var parent = _parent;
        if (parent == IntPtr.Zero) {
            // Used only by the explicit isolated smoke command, never as a fallback.
            _testParent = new HwndSource(new HwndSourceParameters("Tile isolated test parent") {
                WindowStyle = 0, ExtendedWindowStyle = NativeWindowPolicy.ToolWindow,
                Width = 800, Height = 600, PositionX = 0, PositionY = 0,
            });
            parent = _testParent.Handle;
        }
        _surface = new DesktopSurface(parent, View, "Tile · " + Model.Title, Model.Width, Model.Height);
        _surface.Source.AddHook(NativeInput);
        var scale = DesktopLayer.Scale(_surface.Handle);
        var x = Model.X * scale; var y = Model.Y * scale;
        var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point((int)x, (int)y));
        var area = screen.WorkingArea;
        var point = DragSession.Drop(x, y, Model.Width * scale, Model.Height * scale,
            area.Left, area.Top, area.Right, area.Bottom, scale, scale);
        DesktopLayer.Move(_surface.Handle, point.X, point.Y);
        _surface.Show(); _timer.Start(); _ = Refresh();
    }
    public void UpdateEditing() => View.SetEditing(_app.Editing);
    private bool IsButton(IntPtr coordinates)
    {
        if (_surface == null) return false;
        long packed = coordinates.ToInt64();
        var point = new Point(unchecked((short)(packed & 0xffff)), unchecked((short)((packed >> 16) & 0xffff)));
        point = _surface.Source.CompositionTarget.TransformFromDevice.Transform(point);
        var source = View.InputHitTest(point) as DependencyObject;
        while (source != null) {
            if (source is ButtonBase) return true;
            source = source is Visual ? VisualTreeHelper.GetParent(source) : LogicalTreeHelper.GetParent(source);
        }
        return false;
    }
    private IntPtr NativeInput(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (message) {
            case 0x0021: // WM_MOUSEACTIVATE: remain part of the desktop.
                handled = true; return new IntPtr(3); // MA_NOACTIVATE, do not eat the click.
            case 0x0201: // WM_LBUTTONDOWN
                if (!IsButton(lParam) && DesktopLayer.GetCursorPos(out var pointer)) {
                    var rect = DesktopLayer.Bounds(hwnd);
                    _drag = new DragSession(pointer.X, pointer.Y, rect.Left, rect.Top); _dragMoved = false;
                    DesktopLayer.SetCapture(hwnd);
                    if (DesktopLayer.GetCapture() != hwnd) { _drag = null; break; }
                    _dragPoll.Start(); handled = true;
                }
                break;
            case 0x0200: // WM_MOUSEMOVE
                if (_drag != null) { MoveDrag(); handled = true; }
                break;
            case 0x0202: // WM_LBUTTONUP
                if (_drag != null) { FinishDrag(true); handled = true; }
                break;
            case 0x0215: // WM_CAPTURECHANGED
                if (lParam != hwnd) FinishDrag(false);
                break;
            case 0x001F: // WM_CANCELMODE
                FinishDrag(false);
                break;
        }
        return IntPtr.Zero;
    }
    private void MoveDrag()
    {
        if (_drag == null || _surface == null || !DesktopLayer.GetCursorPos(out var pointer)) return;
        var scale = DesktopLayer.Scale(_surface.Handle);
        if (!_dragMoved && !_drag.ExceedsThreshold(pointer.X, pointer.Y,
            SystemParameters.MinimumHorizontalDragDistance * scale, SystemParameters.MinimumVerticalDragDistance * scale)) return;
        _dragMoved = true; View.SetEditing(true);
        var position = _drag.Move(pointer.X, pointer.Y);
        if (!DesktopLayer.Move(_surface.Handle, position.X, position.Y)) FinishDrag(false);
    }
    private void FinishDrag(bool save)
    {
        var drag = _drag; if (drag == null || _surface == null) return;
        _drag = null; _dragPoll.Stop();
        if (DesktopLayer.GetCapture() == _surface.Handle) DesktopLayer.ReleaseCapture();
        View.SetEditing(_app.Editing);
        if (!save || !_dragMoved) { DesktopLayer.Move(_surface.Handle, drag.Origin.X, drag.Origin.Y); return; }
        var rect = DesktopLayer.Bounds(_surface.Handle);
        var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(rect.Left + (rect.Right - rect.Left) / 2, rect.Top + (rect.Bottom - rect.Top) / 2));
        var area = screen.WorkingArea; var scale = DesktopLayer.Scale(_surface.Handle);
        var point = DragSession.Drop(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top,
            area.Left, area.Top, area.Right, area.Bottom, scale, scale);
        if (DesktopLayer.Move(_surface.Handle, point.X, point.Y)) {
            Model.X = point.X / scale; Model.Y = point.Y / scale; _app.Save();
        }
    }
    public async Task Refresh()
    {
        if (_disposed || _busy) return;
        _busy = true;
        try {
            if (Model.Kind == "weather" && !string.IsNullOrEmpty(Model.City) && DateTimeOffset.Now >= _nextWeather) {
                _weatherStatus = "正在更新…"; View.Update(_app.State.Theme, _weather, _weatherStatus);
                try { _weather = await WeatherService.Fetch(Model, _cancel.Token); _weatherStatus = null; _nextWeather = DateTimeOffset.Now.AddMinutes(15); }
                catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or InvalidOperationException or System.Collections.Generic.KeyNotFoundException) {
                    _weatherStatus = _weather == null ? "暂时无法获取天气" : $"离线 · 上次 {_weather.Received:HH:mm}";
                    _nextWeather = DateTimeOffset.Now.AddMinutes(2);
                }
            }
            if (!_disposed) View.Update(_app.State.Theme, _weather, _weatherStatus);
        } finally { _busy = false; }
    }
    public void Close() => Dispose();
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; FinishDrag(false); _dragPoll.Stop(); _timer.Stop();
        _cancel.Cancel(); _cancel.Dispose();
        View.ContextMenu.IsOpen = false;
        if (_surface != null) { _surface.Source.RemoveHook(NativeInput); _surface.Dispose(); _surface = null; }
        _testParent?.Dispose(); _testParent = null;
    }
}
