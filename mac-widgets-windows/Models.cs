using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tile;

public sealed class WidgetModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Kind { get; set; } = "clock";
    public string Size { get; set; } = "small";
    public double X { get; set; } = 64;
    public double Y { get; set; } = 80;
    public string Photo { get; set; } = "";
    public string City { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    [JsonIgnore] public double Width => Size == "small" ? 192 : 400;
    [JsonIgnore] public double Height => Size == "large" ? 400 : 192;
    [JsonIgnore] public string Title => Kind switch { "clock" => "时钟", "weather" => "天气", "photo" => "照片", _ => "日历" };
}
public sealed class Settings
{
    public int Version { get; set; } = 1;
    public string Theme { get; set; } = "system";
    public List<WidgetModel> Widgets { get; set; } = new();
    public static Settings CreateDefault() => new() { Widgets = new() {
        new() { Kind = "clock", X = 64, Y = 80 },
        new() { Kind = "weather", X = 272, Y = 80 },
        new() { Kind = "photo", Size = "medium", X = 64, Y = 288 },
        new() { Kind = "calendar", X = 480, Y = 80 }
    }};
}
public sealed class SettingsStore
{
    public string Path { get; }
    public string? Warning { get; private set; }
    public bool ReadOnly { get; private set; }
    public SettingsStore(string? directory = null) => Path = System.IO.Path.Combine(directory ?? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tile"), "layout.json");
    public Settings Load()
    {
        if (!File.Exists(Path)) return Settings.CreateDefault();
        try {
            var state = JsonSerializer.Deserialize<Settings>(File.ReadAllText(Path)) ?? throw new JsonException("配置为空");
            if (state.Version != 1 || state.Widgets == null || state.Widgets.Count > 100 || state.Widgets.Any(w => w == null || !new[] {"clock","weather","photo","calendar"}.Contains(w.Kind) || !double.IsFinite(w.X) || !double.IsFinite(w.Y))) throw new JsonException("配置格式不兼容");
            foreach (var w in state.Widgets) { if (!new[] {"small","medium","large"}.Contains(w.Size)) w.Size = "small"; w.City ??= ""; w.Photo ??= ""; }
            return state;
        } catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException) {
            ReadOnly = true;
            Warning = "原配置无法读取，已保留；本次临时运行。";
            return Settings.CreateDefault();
        }
    }
    public bool Save(Settings state)
    {
        if (ReadOnly) return false;
        try {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            var temporary = Path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
            if (File.Exists(Path)) File.Replace(temporary, Path, Path + ".previous"); else File.Move(temporary, Path);
            Warning = null;
            return true;
        } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Warning = "布局保存失败：" + e.Message; return false; }
    }
    public static double Snap(double value) => Math.Floor(value / 8 + .5) * 8;
}
