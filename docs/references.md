# 调研与来源

开发前已检索 GitHub：

- https://github.com/rebatnaath/gridgets — GNOME 背景层组件、桌面图标冲突说明，GPL-3.0。
- https://github.com/SatriaNadirakhma/tahoe-widgets — GJS / St 原生卡片及 Open-Meteo 方案，GPL-2.0-or-later。
- https://github.com/thiagobaldez/windows-widgets — WPF 桌面组件及 Windows 11 桌面层兼容问题。

本 demo 独立实现，没有复制上述项目的源文件或素材；保留 Tile 原有 Apache-2.0 许可。

接口依据：

- https://gjs.guide/extensions/overview/anatomy.html
- https://github.com/GNOME/gnome-shell/blob/main/js/ui/backgroundMenu.js
- https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setparent
- https://open-meteo.com/en/docs
- https://open-meteo.com/en/docs/geocoding-api

Win32 Explorer 桌面视图与 GNOME `_backgroundGroup` 都不是稳定的桌面插件接口，需真实桌面环境验收。天气数据署名 Open-Meteo，使用其免费非商业接口，后续商业分发需重新核对服务条款。

交互与照片接口：

- https://support.apple.com/en-gb/guide/mac-help/mchl52be5da5/mac
- https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.FileChooser.html
- https://developers.google.com/photos/picker/guides/get-started-picker
- https://developers.google.com/photos/overview/configure-your-app
