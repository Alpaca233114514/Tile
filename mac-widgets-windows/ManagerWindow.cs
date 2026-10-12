using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Tile;
public sealed class ManagerWindow : Window
{
    private readonly App _app;
    private readonly WrapPanel _library = new();
    private readonly StackPanel _active = new();
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, Foreground = WidgetView.Brush("#65746A"), Margin = new Thickness(0, 12, 0, 0) };
    public ManagerWindow(App app)
    {
        _app = app;
        Title = "Tile · 小组件库"; Width = 1180; Height = 790; MinWidth = 820; MinHeight = 580;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        var root = new Grid { Margin = new Thickness(28) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(205) });
        root.ColumnDefinitions.Add(new ColumnDefinition());
        var side = new StackPanel { Margin = new Thickness(0, 0, 26, 0) };
        side.Children.Add(Label("▦  Tile", 32)); side.Children.Add(Label("让桌面，更像你的生活。", 13));
        var line = Label("你的空间", 12); line.Margin = new Thickness(0, 38, 0, 8); side.Children.Add(line);
        side.Children.Add(Button("▦   全部小组件", () => {}));
        side.Children.Add(Label("外观", 12));
        var theme = new ComboBox { ItemsSource = new[] { "跟随系统", "浅色", "深色" }, SelectedIndex = app.State.Theme == "light" ? 1 : app.State.Theme == "dark" ? 2 : 0 };
        theme.SelectionChanged += (_, _) => { app.State.Theme = new[] { "system", "light", "dark" }[theme.SelectedIndex]; app.Rebuild(); app.Save(); Refresh(); };
        side.Children.Add(theme);
        side.Children.Add(Button("编辑桌面布局", () => { app.SetEditing(true); Hide(); }));
        side.Children.Add(Button("完成编辑", () => app.SetEditing(false)));
        side.Children.Add(Button("退出 Tile", () => app.Shutdown()));
        side.Children.Add(_status);
        var foot = Label("WINDOWS DEMO  /  0.1\n天气数据 · Open-Meteo", 11); foot.Margin = new Thickness(0, 28, 0, 0); side.Children.Add(foot);
        root.Children.Add(side);
        var content = new StackPanel(); Grid.SetColumn(content, 1);
        var hero = new Border { CornerRadius = new CornerRadius(24), Padding = new Thickness(26), Margin = new Thickness(0, 0, 0, 22), Background = new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#E5EBDE"), (Color)ColorConverter.ConvertFromString("#D5E4DC"), 35) };
        var intro = new StackPanel(); intro.Children.Add(Label("A LITTLE SPACE FOR WHAT MATTERS", 11)); intro.Children.Add(Label("一眼之间，生活在这里。", 30)); intro.Children.Add(Label("时间、天气，还有值得留在桌面的瞬间。", 14)); hero.Child = intro; content.Children.Add(hero);
        content.Children.Add(Label("小组件库", 22));
        content.Children.Add(Label("选一个喜欢的，添加到桌面。", 13));
        content.Children.Add(_library);
        content.Children.Add(Label("已在桌面", 22)); content.Children.Add(_active);
        var scroll = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; Grid.SetColumn(scroll, 1); root.Children.Add(scroll);
        Content = root;
        Closing += (_, e) => { if (!app.Exiting) { e.Cancel = true; Hide(); } };
        Refresh();
        SetStatus(app.DesktopStatus);
    }
    public static TextBlock Label(string text, double size = 14) => new() { Text = text, FontSize = size, Foreground = WidgetView.Brush("#28392F"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 8) };
    public static Button Button(string text, Action action) { var b = new Button { Content = text }; b.Click += (_, _) => action(); return b; }
    public void SetStatus(string message) => _status.Text = message;
    public void Refresh()
    {
        _library.Children.Clear(); _active.Children.Clear();
        foreach (var kind in new[] { "clock", "weather", "photo", "calendar" }) {
            var model = new WidgetModel { Kind = kind };
            var card = new StackPanel { Margin = new Thickness(0, 12, 16, 16), Width = 192 };
            var preview = new WidgetView(model); preview.Update(_app.State.Theme);
            card.Children.Add(preview); card.Children.Add(Button("＋ 添加" + model.Title, () => _app.Add(kind))); _library.Children.Add(card);
        }
        foreach (var model in _app.State.Widgets) {
            var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
            var remove = Button("移除", () => _app.Remove(model)); DockPanel.SetDock(remove, Dock.Right); row.Children.Add(remove);
            var edit = Button("配置", () => _app.Configure(model)); DockPanel.SetDock(edit, Dock.Right); row.Children.Add(edit);
            row.Children.Add(Label(model.Title + "  ·  " + (model.Size == "small" ? "小" : model.Size == "large" ? "大" : "中"))); _active.Children.Add(row);
        }
        if (_app.State.Widgets.Count == 0) _active.Children.Add(Label("桌面还是空的，添加第一个小组件吧。"));
    }
}
