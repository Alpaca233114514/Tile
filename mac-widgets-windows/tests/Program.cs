using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Tile;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var output = Path.GetFullPath(args.Length > 0 ? args[0] : "test-output");
        Directory.CreateDirectory(output);
        var checks = new List<string>();
        void Check(bool value, string title) { if (!value) throw new Exception(title); checks.Add("PASS " + title); }
        try {
            var drag = new DragSession(400, 300, 320, 240);
            Check(drag.Move(440, 390) == (360, 330), "drag uses pointer delta without jumping to pointer origin");
            Check(drag.Origin == (320, 240), "cancel retains original position");
            Check(!drag.ExceedsThreshold(402, 301, 4, 4) && drag.ExceedsThreshold(405, 300, 4, 4), "small click movement is not mistaken for a drag");
            Check(SettingsStore.Snap(180) == 184 && SettingsStore.Snap(-180) == -176, "grid midpoint agrees with GNOME on both sides of zero");
            var negative = new DragSession(-1600, 200, -1680, 120);
            Check(negative.Move(-1400, 260) == (-1480, 180), "drag across negative monitor coordinates");
            Check(DragSession.Drop(151, 83, 288, 288, 0, 0, 1920, 1080, 1.5, 1.5) == (156, 84), "150 percent DPI snaps on logical grid");
            Check(DragSession.Drop(1900, 1100, 288, 288, 0, 40, 1920, 1080, 1.5, 1.5) == (1632, 792), "drop remains in work area");
            var photo = Path.Combine(output, "本地 照片.png");
            var bitmap = BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null,
                new byte[] { 0, 0, 255, 255, 0, 255, 0, 255, 255, 0, 0, 255, 255, 255, 255, 255 }, 8);
            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(photo)) encoder.Save(stream);
            Check(Photos.TryLoad(photo, out var decoded) && decoded!.PixelWidth > 0 && decoded.IsFrozen, "local photo decodes with Unicode and spaces in filename");
            using (File.Open(photo, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) checks.Add("PASS selected photo is not file-locked");
            var bad = Path.Combine(output, "not-an-image.png"); File.WriteAllText(bad, "not an image");
            Check(!Photos.TryLoad(bad, out _), "invalid image is rejected");
            Check(!Photos.TryLoad(Path.Combine(output, "missing.png"), out _), "missing image has a recoverable state");
            var store = new SettingsStore(output);
            var state = Settings.CreateDefault(); state.Widgets[2].Photo = photo;
            state.Widgets[0].X = -1480; state.Widgets[0].Y = 180;
            Check(store.Save(state), "save photo and dragged position");
            var loaded = store.Load();
            Check(loaded.Widgets[2].Photo == photo && loaded.Widgets[0].X == -1480 && loaded.Widgets[0].Y == 180, "photo and dragged position survive restart");
            state.Widgets.Clear(); Check(store.Save(state) && store.Load().Widgets.Count == 0, "empty desktop survives restart");
            NativeChecks.Run(Check);
            File.WriteAllLines(Path.Combine(output, "result.txt"), checks); Console.WriteLine(string.Join(Environment.NewLine, checks));
            return 0;
        } catch (Exception error) { Console.Error.WriteLine(error); File.WriteAllText(Path.Combine(output, "error.txt"), error.ToString()); return 1; }
    }
}
