# Windows 宿主与拖动修订 · 2026-10-12

## 现场证据

用户反馈 v02 仍出现大量 Alt+Tab 后台项，桌面组件不能拖动。未使用电脑控制。通过只读进程/Win32 查询采样正在运行的 v02（PID 28408）：

- 五个可见卡片为 `SHELLDLL_DefView` 子窗口，但均没有 `WS_EX_TOOLWINDOW`。
- 每张卡片有一个对应的 WPF `Hidden Window` owner。
- 源码仍使用顶层 WPF `Window` 加上手工原生 reparent；拖动被 `Editing` 状态限制。

原始记录：`artifacts/windows-v02-window-inspection.json`。这些是窗口与代码证据；没有实际打开或读取 Alt+Tab UI，未把某一个隐藏窗口单独认定为全部故障的唯一原因。

## 修订

- 卡片改为从创建起就是子窗口的 `HwndSource`，明确设置桌面父窗口、`WS_EX_TOOLWINDOW`、非激活样式和子窗口透明 API。消除 WPF Window 隐藏 owner。
- 使用原生鼠标消息/捕获处理拖动，并在拖动期间读取指针和左键状态以处理跨越窗口边界后的释放；不安装全局钩子、不模拟输入。
- 直接拖动卡片非按钮区域，系统阈值区分点击与拖动。按钮仍正常进入 WPF 输入管线。
- GNOME 同步取消编辑模式门槛，并加入 4 px 拖动阈值。
- 添加 Windows 10/11 compatibility manifest 声明。隐藏宿主测试在添加声明前创建透明子窗口失败，在添加声明后通过；失败记录保留于 `artifacts/windows-v03-checks*`。

## 验证

- Release build / publish 成功，0 警告、0 错误。
- 22 项检查通过：14 项逻辑/本地图片检查，8 项真实但不可见的原生宿主检查。包括 child 关系、ToolWindow/AppWindow 标志、透明子窗口、owner=0、实际 SetWindowPos 坐标变化与释放 HWND。
- 隐藏宿主检查仅创建测试进程自己的不可见窗口，没有连接 Explorer，没有输入注入或操作用户窗口。结果：`artifacts/windows-v03-checks-manifest/result.txt`。
- 按已有授权，仅通过进程命令退出路径完全匹配 v02 的旧进程，启动 v03（PID 31988）。用户布局与照片配置保留。
- 启动后只读查询确认：五个可见卡片均为桌面子窗口，ToolWindow=true、AppWindow=false、Owner=0；原来五个 `Hidden Window` owner 不再存在。记录：`artifacts/windows-v03-window-inspection.json`。
- GNOME 7/7 测试通过，语法检查与 schema/扩展打包通过。

没有代用户执行鼠标拖动或 Alt+Tab；拖动手感、所有显示器布局与桌面图标交互仍需实际用户试用。没有宣称这些交互已由自动化验收。

参考：[Microsoft 窗口样式](https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles)、[WPF 原生宿主](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/walkthrough-hosting-wpf-content-in-win32)、[现代透明子窗口参数](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/HwndSourceParameters.cs)。
