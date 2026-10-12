namespace Tile;

// Screen-space arithmetic is independent of WPF parent-relative coordinates.
public sealed class DragSession
{
    private readonly double _pointerX, _pointerY, _originX, _originY;
    public DragSession(double pointerX, double pointerY, double originX, double originY)
    { _pointerX = pointerX; _pointerY = pointerY; _originX = originX; _originY = originY; }
    public (double X, double Y) Move(double pointerX, double pointerY) => (_originX + pointerX - _pointerX, _originY + pointerY - _pointerY);
    public (double X, double Y) Origin => (_originX, _originY);
    public bool ExceedsThreshold(double x, double y, double thresholdX, double thresholdY) =>
        System.Math.Abs(x - _pointerX) >= thresholdX || System.Math.Abs(y - _pointerY) >= thresholdY;
    public static (double X, double Y) Drop(double x, double y, double width, double height, double left, double top, double right, double bottom, double scaleX, double scaleY)
    {
        return (System.Math.Clamp(SettingsStore.Snap(x / scaleX) * scaleX, left, System.Math.Max(left, right - width)),
            System.Math.Clamp(SettingsStore.Snap(y / scaleY) * scaleY, top, System.Math.Max(top, bottom - height)));
    }
}
