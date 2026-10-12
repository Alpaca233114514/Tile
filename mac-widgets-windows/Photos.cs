using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace Tile;
public static class Photos
{
    public const string Filter = "照片|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
    public static BitmapImage Load(string path)
    {
        // Decode once with OnLoad so the selected original is never locked.
        using var stream = File.OpenRead(path);
        var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = 1200; image.StreamSource = stream; image.EndInit(); image.Freeze();
        return image;
    }
    public static bool TryLoad(string path, out BitmapImage? image)
    {
        try { image = Load(path); return true; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or System.Runtime.InteropServices.COMException) { image = null; return false; }
    }
}
