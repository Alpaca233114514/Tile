# Tile · 原生桌面小组件

两个独立原生 demo，参考 macOS 的卡片与桌面交互，不包含 Apple 字体、图标或壁纸。

- [Windows demo](mac-widgets-windows/README.md)：C# / WPF / Win32，.NET 8。
- [GNOME demo](mac-widgets-gnome/README.md)：GJS / St / Clutter，GNOME Shell 45–49 目标版本。
- [视觉规范](docs/design.md) · [开发路线与验收](docs/roadmap.md) · [参考项目](docs/references.md)
- [Windows 宿主与拖动修订](docs/verification-v03.md) · [交互修订验证记录](docs/verification-v02.md) · [初始 demo 验证](docs/verification.md)

目前两套源码放在 Tile 中交付，每个目录有自己的构建、说明和许可证，可以单独复制为独立仓库。不共享运行时代码。尚未创建或推送远程仓库。

这是 v0.1 demo，不是完成版。天气使用 Open-Meteo，需联网；没有网络或未选城市时明确显示状态。照片由用户本地选择。普通用户通过图形界面管理组件。
