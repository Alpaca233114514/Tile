using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Tile;
public sealed class WidgetView : Border
{
    public WidgetModel Model { get; }
    private readonly StackPanel _body = new();
    private readonly TextBlock _title;
    private readonly Grid _grid = new();
    public FrameworkElement DragHandle => _title;
    private bool _dark;
    private bool _editing;
    private readonly bool _interactive;
    private BitmapImage? _photo;
    private string? _photoPath;
    public event Action? ChoosePhoto;
    public WidgetView(WidgetModel model, bool interactive = false)
    {
        Model = model;
        _interactive = interactive;
        CornerRadius = new CornerRadius(24);
        BorderThickness = new Thickness(1);
        Padding = new Thickness(20, 14, 20, 16);
        _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _grid.RowDefinitions.Add(new RowDefinition());
        _title = Text(model.Title, 12);
        _title.ToolTip = "直接按住卡片拖动 · 右键配置";
        _title.Margin = new Thickness(0, 0, 0, 9);
        _grid.Children.Add(_title);
        Grid.SetRow(_body, 1);
        _grid.Children.Add(_body);
        Child = _grid;
        Update("system");
    }
    public static TextBlock Text(string value, double size = 14, Brush? color = null) => new() {
        Text = value, FontSize = size, Foreground = color ?? Brushes.White,
        TextWrapping = TextWrapping.Wrap, TextTrimming = TextTrimming.CharacterEllipsis,
        FontFamily = new FontFamily("Segoe UI"), VerticalAlignment = VerticalAlignment.Center
    };
    public static Brush Brush(string value) => (Brush)new BrushConverter().ConvertFromString(value)!;
    public static bool IsDark(string theme) => theme == "dark" || theme == "system" && (int?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) == 0;
    public void SetEditing(bool editing)
    {
        _editing = editing;
        _title.Text = Model.Title + (editing ? "  · 拖动排列" : "");
        BorderBrush = editing ? Brush("#8CB89B") : Brush(_dark ? "#304F5C64" : "#70FFFFFF");
    }
    public void Update(string theme, Weather? weather = null, string? status = null)
    {
        _dark = IsDark(theme);
        Width = Model.Width; Height = Model.Height;
        var colored = Model.Kind == "weather" || Model.Kind == "photo";
        var foreground = colored || _dark ? Brushes.White : Brush("#29312C");
        Background = Model.Kind switch {
            "weather" => new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#527CA1"), (Color)ColorConverter.ConvertFromString("#244661"), 70),
            "photo" => new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#D9C9AA"), (Color)ColorConverter.ConvertFromString("#577664"), 70),
            _ => Brush(_dark ? "#F0242831" : "#F4F6F5F1")
        };
        BorderBrush = Brush(_dark ? "#304F5C64" : "#70FFFFFF");
        SetEditing(_editing);
        _title.Foreground = foreground;
        _body.Children.Clear();
        void Add(string text, double size = 14, double opacity = 1) { var t = Text(text, size, foreground); t.Opacity = opacity; _body.Children.Add(t); }
        if (Model.Kind == "clock") {
            Add(DateTime.Now.ToString("HH:mm"), Model.Size == "small" ? 47 : 58);
            Add(DateTime.Now.ToString("M月d日 · dddd", CultureInfo.GetCultureInfo("zh-CN")), 13, .72);
            if (Model.Size == "large") { Add("\n把时间，留给生活。", 22, .7); Add(TimeZoneInfo.Local.DisplayName, 12, .6); }
        } else if (Model.Kind == "weather") {
            Add(string.IsNullOrEmpty(Model.City) ? "选择你的城市" : Model.City.Split('·')[0], 14);
            Add(weather == null ? "—°" : $"{weather.Temperature:0}°  {weather.Symbol}", 44);
            Add(status ?? (weather == null ? "右键配置城市" : $"{weather.Condition} · {weather.Received:HH:mm}"), 12, .9);
            if (Model.Size != "small") Add("Open-Meteo", 11, .7);
        } else if (Model.Kind == "calendar") {
            Add(DateTime.Now.ToString("dddd", CultureInfo.GetCultureInfo("zh-CN")), 13);
            Add(DateTime.Today.Day.ToString(), 56);
            Add(DateTime.Today.ToString("yyyy年 M月"), 13, .65);
            if (Model.Size == "large") {
                var days = new System.Windows.Controls.Primitives.UniformGrid { Columns = 7, Margin = new Thickness(0, 18, 0, 0) };
                foreach (var day in new[] { "一", "二", "三", "四", "五", "六", "日" }) days.Children.Add(Text(day, 12, foreground));
                var first = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                for (int i = 0; i < ((int)first.DayOfWeek + 6) % 7; i++) days.Children.Add(new TextBlock());
                for (int i = 1; i <= DateTime.DaysInMonth(first.Year, first.Month); i++) { var day = Text(i.ToString(), 14, foreground); day.Margin = new Thickness(0, 5, 0, 5); if (i == DateTime.Today.Day) day.FontWeight = FontWeights.Bold; days.Children.Add(day); }
                _body.Children.Add(days);
            }
        } else {
            if (_photoPath != Model.Photo) { _photoPath = Model.Photo; _photo = null; if (!string.IsNullOrEmpty(Model.Photo)) Photos.TryLoad(Model.Photo, out _photo); }
            var choose = new Button { Content = _photo == null ? "选择本地照片" : "更换照片", FontSize = 12, Padding = new Thickness(10, 6, 10, 6), IsEnabled = _interactive };
            choose.Click += (_, _) => ChoosePhoto?.Invoke();
            if (_photo != null) {
                var photo = new Grid { Height = Model.Height - 66 };
                photo.Children.Add(new Border { CornerRadius = new CornerRadius(12), Background = new ImageBrush(_photo) { Stretch = Stretch.UniformToFill } });
                choose.HorizontalAlignment = HorizontalAlignment.Right; choose.VerticalAlignment = VerticalAlignment.Bottom;
                photo.Children.Add(choose); _body.Children.Add(photo);
            } else {
                Add("▧", 36);
                Add(string.IsNullOrEmpty(Model.Photo) ? "留住喜欢的瞬间" : "照片不可用", 14);
                _body.Children.Add(choose);
            }
        }
    }
}
