using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Tile;
public sealed class ConfigureWindow : Window
{
    private readonly CancellationTokenSource _cancel = new();
    public ConfigureWindow(App app, WidgetModel model)
    {
        Title = "配置 · " + model.Title; Width = 480; Height = model.Kind == "weather" ? 510 : 330;
        ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var body = new StackPanel { Margin = new Thickness(24) }; Content = body;
        body.Children.Add(ManagerWindow.Label(model.Title, 26));
        body.Children.Add(ManagerWindow.Label("卡片尺寸"));
        var sizes = new ComboBox { ItemsSource = new[] { "小 · 192 × 192", "中 · 400 × 192", "大 · 400 × 400" }, SelectedIndex = model.Size == "small" ? 0 : model.Size == "medium" ? 1 : 2 };
        body.Children.Add(sizes);
        string photo = model.Photo;
        Place? selected = null;
        if (model.Kind == "photo") {
            var path = ManagerWindow.Label(string.IsNullOrEmpty(photo) ? "选择一张本地照片" : System.IO.Path.GetFileName(photo), 12); body.Children.Add(path);
            body.Children.Add(ManagerWindow.Button("选择本地照片…", () => { var dialog = new OpenFileDialog { Filter = Photos.Filter, CheckFileExists = true }; if (dialog.ShowDialog(this) == true && Photos.TryLoad(dialog.FileName, out _)) { photo = dialog.FileName; path.Text = System.IO.Path.GetFileName(photo); } }));
        }
        if (model.Kind == "weather") {
            body.Children.Add(ManagerWindow.Label("搜索城市，然后选择地点"));
            var query = new TextBox { Text = model.City.Split('·')[0].Trim() }; body.Children.Add(query);
            var status = ManagerWindow.Label("", 12);
            var results = new ListBox { Height = 100, DisplayMemberPath = "Name" }; results.SelectionChanged += (_, _) => selected = results.SelectedItem as Place;
            var search = new Button { Content = "搜索城市" };
            search.Click += async (_, _) => {
                if (query.Text.Trim().Length < 2) { status.Text = "请输入至少两个字符"; return; }
                search.IsEnabled = false; status.Text = "搜索中…";
                try { var places = await WeatherService.Search(query.Text, _cancel.Token); results.ItemsSource = places; status.Text = places.Count == 0 ? "没有找到，试试拼音或英文城市名" : "请选择一个搜索结果"; }
                catch (Exception e) when (e is System.Net.Http.HttpRequestException or System.Threading.Tasks.TaskCanceledException or System.Text.Json.JsonException or System.Collections.Generic.KeyNotFoundException or InvalidOperationException) { if (!_cancel.IsCancellationRequested) status.Text = "搜索失败，请检查网络后重试"; }
                finally { search.IsEnabled = true; }
            };
            body.Children.Add(search); body.Children.Add(results); body.Children.Add(status);
        }
        body.Children.Add(ManagerWindow.Button("保存", () => {
            model.Size = new[] { "small", "medium", "large" }[sizes.SelectedIndex]; model.Photo = photo;
            if (selected != null) { model.City = selected.Name; model.Latitude = selected.Latitude; model.Longitude = selected.Longitude; }
            app.Save(); app.Rebuild(); Close();
        }));
        Closed += (_, _) => { _cancel.Cancel(); _cancel.Dispose(); };
    }
}
