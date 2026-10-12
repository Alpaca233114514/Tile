using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Tile;
public sealed record Place(string Name, double Latitude, double Longitude)
{
    public override string ToString() => Name;
}
public sealed record Weather(double Temperature, string Condition, string Symbol, DateTimeOffset Received);
public static class WeatherService
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(12) };
    public static async Task<List<Place>> Search(string query, CancellationToken token)
    {
        var text = await Client.GetStringAsync("https://geocoding-api.open-meteo.com/v1/search?count=8&language=zh&name=" + Uri.EscapeDataString(query.Trim()), token);
        using var doc = JsonDocument.Parse(text);
        var result = new List<Place>();
        if (doc.RootElement.TryGetProperty("results", out var places)) foreach (var p in places.EnumerateArray()) {
            string Field(string name) => p.TryGetProperty(name, out var v) ? v.GetString() ?? "" : "";
            result.Add(new Place($"{Field("name")} · {Field("admin1")} {Field("country")}", p.GetProperty("latitude").GetDouble(), p.GetProperty("longitude").GetDouble()));
        }
        return result;
    }
    public static async Task<Weather> Fetch(WidgetModel model, CancellationToken token)
    {
        var url = string.Create(CultureInfo.InvariantCulture, $"https://api.open-meteo.com/v1/forecast?latitude={model.Latitude}&longitude={model.Longitude}&current=temperature_2m,weather_code&timezone=auto");
        var text = await Client.GetStringAsync(url, token);
        using var doc = JsonDocument.Parse(text);
        var current = doc.RootElement.GetProperty("current");
        var code = current.GetProperty("weather_code").GetInt32();
        var (label, symbol) = Describe(code);
        return new(current.GetProperty("temperature_2m").GetDouble(), label, symbol, DateTimeOffset.Now);
    }
    public static (string, string) Describe(int code) => code switch {
        0 => ("晴朗", "☀"), 1 or 2 => ("晴间多云", "☀"), 3 => ("多云", "☁"),
        45 or 48 => ("雾", "≋"), >= 71 and <= 77 or 85 or 86 => ("降雪", "❄"),
        >= 95 and <= 99 => ("雷雨", "ϟ"), >= 51 and <= 67 or >= 80 and <= 82 => ("降雨", "☂"), _ => ("天气", "◌") };
}
