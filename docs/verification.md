# Demo 验证记录 · 2026-10-11

这是初始 demo 的历史记录。默认桌面行为、拖动和照片入口已在 revision 2 修订，见 [新版记录](verification-v02.md)。

## Windows

- .NET SDK 8.0.423；目标 net8.0-windows / win-x64。
- Release build：0 warnings / 0 errors。framework-dependent publish 成功，需 .NET 8 Desktop Runtime。
- WPF 程序实际启动；组件库与独立 WidgetWindow 加载通过。
- 内置隔离 smoke：新增、切换尺寸、移除、空桌面保存/重载、损坏配置不覆盖、网格吸附及天气代码映射通过。
- RenderTargetBitmap 输出四种原生卡片的浅色/深色 PNG 及组件库 PNG，已视觉检查。这些是 WPF 渲染图，不是 Windows 桌面截图。
- 独立联网检查首次在城市搜索时 TLS 握手 EOF；系统 HTTP 客户端核对成功后，demo 客户端重试成功。北京搜索及预报结果：2026-10-11 19:43 +08:00 收到 21.6°C，晴间多云。未放宽证书校验、未改代理设置。
- 首次失败：`artifacts/weather/weather-error.txt`；成功：`artifacts/weather-retry/weather-live.json`。
- 最终 smoke：`artifacts/windows-final/smoke-result.txt`；原生渲染：同目录 PNG。

尚未验证：手动拖拽全过程、实际照片文件选择流程、WorkerW 可见性 / Win+D / 桌面图标命中、Explorer 重启、混合 DPI、多显示器和长期资源占用。实验模式默认关闭，可回到浮窗。

## GNOME

- Node 平台无关测试 6/6：配置解析、空布局、实例唯一性、尺寸、负坐标显示器/边界吸附、天气分类、schema 初始数据。
- extension.js / prefs.js / network.js 的 Node 语法检查通过；这不等于 GJS 或 Shell 运行测试。
- Python 安装与打包助手通过语法检查。
- Debian 13 WSL 的现有 GLib 2.84.4 schema 编译器执行 `--strict` 成功；ZIP 内含 `schemas/gschemas.compiled`。包成员与 ZIP 完整性检查通过。
- WSL 未安装 GNOME Shell / GJS，不能在该环境验证 Shell 扩展和 GTK 偏好界面。未声称 Debian 桌面、Wayland、45–49 全版本兼容已通过。

schema 编译器在 `/usr/lib/x86_64-linux-gnu/glib-2.0/`。寻找工具时曾下载 Debian 官方 libglib2.0-bin 到项目 `.tools` 解包检查；未安装、未执行维护脚本，最终使用的是系统已有编译器。产物不包含这些临时工具。

## 交付与边界

两套工程有独立入口、README 和 LICENSE，当前同放 Tile 目录，没有创建远程仓库。分支 `codex/native-widgets-demo`，未提交、未推送。

时钟、天气、照片、日历为 demo 范围。便签、待办、音乐、系统监控、完整安装器及发布流程仍在路线图中。
