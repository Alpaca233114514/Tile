# 交互修订验证 · 2026-10-11 · demo revision 2

用户指出旧版默认浮窗、拖动入口不可用、照片入口隐藏。本轮同时修订 Windows 与 GNOME，禁止使用电脑控制的约束始终保留。

## 已修改

- Windows 默认连接 Explorer 桌面视图后才显示，不自动打开组件库。连接失败不回退成普通浮窗，而是保留托盘入口和提示。
- Windows 从 WorkerW 改为 DefView 子窗口宿主；拖拽使用鼠标捕获、屏幕坐标及父窗口坐标转换，编辑不脱离桌面。原先 `if (Embedded) return` 的禁用逻辑已移除。
- 两端右键/托盘或顶栏进入编辑，整张卡片可拖动，松手网格吸附并保存；GNOME 的取消拖动恢复原点。
- 两端照片卡片直接提供“选择本地照片”。Windows 使用原生文件对话框并支持拖入图片；GNOME 使用 FileChooser portal，偏好设置仍提供 GTK 选择入口。不再用山景表示未配置照片。
- Windows 本地图片读取后释放文件句柄，并对不可读图片保留更换入口。
- Google Photos 只完成官方方案与配置前置条件核对，尚未实现登录、Picker 或下载，不声称可用。

## 本轮实际验证

- Windows Release build / publish 成功：0 警告、0 错误。
- Windows 无窗口控制台检查 13/13：指针偏移、取消原点、跨平台网格中点、负坐标、150% DPI、工作区限制、中文/空格路径图片解码、文件锁释放、损坏/缺失图像、路径和布局持久化、空桌面持久化。
- 结果：`artifacts/windows-desktop-v02-checks-final/result.txt`。图像是测试程序生成的独立 PNG，未读取用户相册。
- GNOME Node 测试 7/7；extension.js / prefs.js / photoPicker.js 语法检查通过。
- GNOME schema 严格编译及新版 ZIP 打包通过，包含 photoPicker.js 和 gschemas.compiled。
- 没有调用电脑控制、没有自动鼠标/键盘操作、没有启动新版 GUI、没有关闭或重启旧版。

## 未验证

新版真实桌面显示、DefView 可见性和图标交互、鼠标实际拖动、系统文件选择器、拖入文件、Wayland、Explorer 重启及多屏混合 DPI 仍需用户试用或后续授权下的实机验收。逻辑检查不替代这些验证。

旧版记录与文件均保留；本记录取代旧版的默认浮窗设计描述。当前已解压 Windows 新版位于 `artifacts/windows-desktop-v02/Tile.exe`。旧版在运行时需要先从托盘退出，再打开新版，避免单实例检查拦截。
