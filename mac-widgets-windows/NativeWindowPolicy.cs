namespace Tile;

public static class NativeWindowPolicy
{
    public const int ChildStyle = 0x40000000 | 0x04000000; // WS_CHILD | WS_CLIPSIBLINGS
    public const int ToolWindow = 0x00000080;
    public const int AppWindow = 0x00040000;
    public const int NoActivate = 0x08000000;
    public const int Layered = 0x00080000;
    public const int ExtendedStyle = ToolWindow | NoActivate | Layered;
}
