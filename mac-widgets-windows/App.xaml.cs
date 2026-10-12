using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace Tile;
public partial class App : Application
{
    public Settings State { get; private set; } = Settings.CreateDefault();
    private SettingsStore _store = new();
    private readonly List<WidgetWindow> _widgets = new();
    private ManagerWindow? _manager;
    private Forms.NotifyIcon? _tray;
    private Mutex? _mutex;
    public bool Exiting { get; private set; }
    private bool _desktop = true;
    public bool Editing { get; private set; }
    public string DesktopStatus { get; private set; } = "直接按住卡片拖动，右键配置。";
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--smoke-test")) { Smoke(e.Args.Last()); return; }
        if (e.Args.Contains("--check-weather")) { CheckWeather(e.Args.Last()); return; }
        _mutex = new Mutex(true, "Local\\Tile.Windows.Demo", out bool created);
        if (!created) { MessageBox.Show("Tile 已在运行，可从系统托盘打开组件库。", "Tile"); Shutdown(); return; }
        State = _store.Load();
        _tray = new Forms.NotifyIcon { Text = "Tile · 桌面小组件", Icon = System.Drawing.SystemIcons.Application, Visible = true };
        var menu = new Forms.ContextMenuStrip(); menu.Items.Add("添加小组件…", null, (_, _) => ShowManager()); menu.Items.Add("编辑小组件", null, (_, _) => SetEditing(true)); menu.Items.Add("完成编辑", null, (_, _) => SetEditing(false)); menu.Items.Add("重新连接桌面", null, (_, _) => Rebuild()); menu.Items.Add("退出", null, (_, _) => Shutdown());
        _tray.ContextMenuStrip = menu; _tray.DoubleClick += (_, _) => ShowManager();
        SystemEvents.UserPreferenceChanged += PreferenceChanged;
        SystemEvents.DisplaySettingsChanged += DisplayChanged;
        Rebuild();
        if (_store.Warning != null) { _tray.ShowBalloonTip(5000, "Tile", _store.Warning, Forms.ToolTipIcon.Warning); }
    }
    private void PreferenceChanged(object sender, UserPreferenceChangedEventArgs e) => Dispatcher.BeginInvoke(new Action(() => { foreach (var w in _widgets) _ = w.Refresh(); _manager?.Refresh(); }));
    private void DisplayChanged(object? sender, EventArgs e) => Dispatcher.BeginInvoke(new Action(Rebuild));
    public void ShowManager() { _manager ??= new ManagerWindow(this); _manager.Show(); _manager.Activate(); _manager.Refresh(); }
    public void Save() { _store.Save(State); if (_store.Warning != null) _manager?.SetStatus(_store.Warning); }
    public void Add(string kind) { State.Widgets.Add(new WidgetModel { Kind = kind, X = 64 + State.Widgets.Count % 4 * 208, Y = 80 + State.Widgets.Count / 4 * 208 }); Save(); Rebuild(); }
    public void Remove(WidgetModel model) { State.Widgets.Remove(model); Save(); Rebuild(); }
    public void Configure(WidgetModel model) { ShowManager(); new ConfigureWindow(this, model) { Owner = _manager }.ShowDialog(); }
    public void SetEditing(bool enabled) { Editing = enabled; foreach (var w in _widgets) w.UpdateEditing(); _manager?.SetStatus(enabled ? "编辑中：整张卡片可拖动，右键完成编辑。" : DesktopStatus); }
    public void ChoosePhoto(WidgetModel model)
    {
        var dialog = new OpenFileDialog { Title = "选择本地照片", Filter = Photos.Filter, CheckFileExists = true };
        if (dialog.ShowDialog() == true) UsePhoto(model, dialog.FileName);
    }
    public void UsePhoto(WidgetModel model, string path)
    {
        if (!Photos.TryLoad(path, out _)) { MessageBox.Show("无法读取这张照片。请选择 JPG、PNG、BMP 或 GIF。", "Tile"); return; }
        model.Photo = Path.GetFullPath(path); Save(); Rebuild();
    }
    public void Rebuild()
    {
        foreach (var w in _widgets.ToArray()) w.Close(); _widgets.Clear();
        bool failed = false;
        foreach (var model in State.Widgets) {
            var w = new WidgetWindow(this, model);
            if (_desktop && !w.TryEmbed()) { failed = true; w.Close(); continue; }
            try { w.Show(); w.UpdateEditing(); _widgets.Add(w); }
            catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException) {
                w.Close(); failed = true;
                try { Directory.CreateDirectory(Path.GetDirectoryName(_store.Path)!); File.AppendAllText(Path.Combine(Path.GetDirectoryName(_store.Path)!, "desktop-errors.log"), DateTimeOffset.Now + " " + error + Environment.NewLine); }
                catch (Exception logError) when (logError is IOException or UnauthorizedAccessException) { System.Diagnostics.Trace.WriteLine(logError.Message); }
            }
        }
        DesktopStatus = failed ? "暂未连接到桌面，可从托盘重新连接。" : "直接按住卡片拖动，右键配置。";
        if (failed) _tray?.ShowBalloonTip(5000, "Tile", DesktopStatus, Forms.ToolTipIcon.Warning);
        _manager?.Refresh();
        _manager?.SetStatus(DesktopStatus);
    }
    protected override void OnExit(ExitEventArgs e)
    {
        Exiting = true;
        foreach (var widget in _widgets.ToArray()) widget.Close();
        _widgets.Clear();
        SystemEvents.UserPreferenceChanged -= PreferenceChanged; SystemEvents.DisplaySettingsChanged -= DisplayChanged;
        _tray?.Dispose(); _mutex?.Dispose();
        base.OnExit(e);
    }
    private void Smoke(string directory)
    {
        try {
            directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory);
            _desktop = false; // Isolated rendering checks never reparent into Explorer.
            _store = new SettingsStore(Path.Combine(directory, "isolated-state"));
            State = Settings.CreateDefault();
            State.Widgets[0].X = SettingsStore.Snap(107); State.Widgets[1].City = "测试城市";
            if (!_store.Save(State) || _store.Load().Widgets[0].X != 104 || _store.Load().Widgets.Count != 4) throw new Exception("Layout round trip failed");
            if (WeatherService.Describe(95).Item1 != "雷雨") throw new Exception("Weather code mapping failed");
            State.Widgets[1].City = "";
            _manager = new ManagerWindow(this); _manager.Show(); _manager.UpdateLayout();
            Capture(_manager, Path.Combine(directory, "windows-gallery.png"));
            foreach (var theme in new[] { "light", "dark" }) {
                var panel = new System.Windows.Controls.WrapPanel { Background = WidgetView.Brush(theme == "dark" ? "#101820" : "#DCE5D8"), Width = 850 };
                foreach (var kind in new[] { "clock", "weather", "photo", "calendar" }) { var v = new WidgetView(new WidgetModel { Kind = kind }) { Margin = new Thickness(10) }; v.Update(theme); panel.Children.Add(v); }
                panel.Measure(new Size(850, 220)); panel.Arrange(new Rect(0, 0, 850, 220)); panel.UpdateLayout(); Capture(panel, Path.Combine(directory, "windows-cards-" + theme + ".png"));
            }
            Add("clock");
            if (_widgets.Count != 5 || _store.Load().Widgets.Count != 5) throw new Exception("Add instance failed");
            var added = State.Widgets.Last(); added.Size = "large"; Rebuild();
            if (_widgets.Last().Width != 400 || _widgets.Last().Height != 400) throw new Exception("Resize failed");
            Remove(added);
            if (_widgets.Count != 4 || _store.Load().Widgets.Count != 4) throw new Exception("Remove instance failed");
            foreach (var model in State.Widgets.ToArray()) Remove(model);
            if (_widgets.Count != 0 || _store.Load().Widgets.Count != 0) throw new Exception("Empty desktop persistence failed");
            var badStore = new SettingsStore(Path.Combine(directory, "invalid-state-" + Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(Path.GetDirectoryName(badStore.Path)!); File.WriteAllText(badStore.Path, "broken");
            badStore.Load();
            if (!badStore.ReadOnly || badStore.Save(State) || File.ReadAllText(badStore.Path) != "broken") throw new Exception("Invalid state was not preserved");
            File.WriteAllText(Path.Combine(directory, "smoke-result.txt"), "PASS: WPF manager and native widget windows loaded; light/dark renders; atomic layout round trip; snap; weather codes; add/resize/remove; empty desktop persistence; corrupt state preserved. Desktop embedding, manual drag and live weather not tested by this smoke run.");
            Shutdown(0);
        } catch (Exception error) { Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "smoke-error.txt"), error.ToString()); Shutdown(1); }
    }
    private static void Capture(FrameworkElement element, string path)
    {
        var bitmap = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var file = File.Create(path); encoder.Save(file);
    }
    private async void CheckWeather(string directory)
    {
        Directory.CreateDirectory(directory);
        try {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var places = await WeatherService.Search("Beijing", cancellation.Token);
            if (places.Count == 0) throw new Exception("No geocoding result");
            var place = places[0];
            var model = new WidgetModel { Kind = "weather", City = place.Name, Latitude = place.Latitude, Longitude = place.Longitude };
            var weather = await WeatherService.Fetch(model, cancellation.Token);
            File.WriteAllText(Path.Combine(directory, "weather-live.json"), System.Text.Json.JsonSerializer.Serialize(new { source = "Open-Meteo", place, weather }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            var card = new WidgetView(model); card.Update("light", weather);
            card.Measure(new Size(192, 192)); card.Arrange(new Rect(0, 0, 192, 192)); card.UpdateLayout();
            Capture(card, Path.Combine(directory, "weather-live.png"));
            Shutdown(0);
        } catch (Exception error) { File.WriteAllText(Path.Combine(directory, "weather-error.txt"), error.ToString()); Shutdown(1); }
    }
}
