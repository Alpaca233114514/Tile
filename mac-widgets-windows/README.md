# Tile for Windows · v0.1 demo

C# / WPF / Win32 独立桌面程序，Windows 11 x64，.NET 8 Desktop Runtime。没有第三方 NuGet 包。

```powershell
dotnet build -c Release
dotnet run -c Release
```

双击构建目录的 `bin/Release/net8.0-windows/Tile.exe` 可运行。启动默认把时钟、天气、照片、日历放到桌面，不弹组件库。系统托盘可添加、编辑或退出。

- 直接按住卡片的非按钮区域拖动，无需先进入编辑。越过系统拖动阈值后移动，松手 8 px 吸附并保存；拖动时按 Esc 取消。托盘的编辑入口仅用于突出显示卡片。组件始终保留在桌面层。
- 天气配置搜索城市并选择结果，15 分钟刷新，失败后 2 分钟重试；请求超时 12 秒。数据来源 Open-Meteo。
- 照片卡片直接点击“选择本地照片”，或拖入 JPG / PNG / BMP / GIF；右键可更换。原图不复制、不上传，读取后不占用文件锁，缺失时显示重新选择入口。
- 主题支持系统 / 浅色 / 深色，卡片随系统外观变化。
- `%LOCALAPPDATA%/Tile/layout.json` 保存布局与配置；原子替换并保留上一版。损坏配置不覆盖，临时模式会提示。
- 默认连接 Explorer 的 `SHELLDLL_DefView` 桌面视图；不会因连接失败自动变成悬浮窗口。失败时托盘提示，可点击“重新连接桌面”。该 Win32 宿主方式尚需实机检查可见性、图标命中和 Win+D。Explorer 重启后若组件丢失，同样从托盘重新连接。
- 卡片使用原生 `HwndSource` 子窗口，不再把 WPF `Window` 强行重新挂接到桌面；创建时带 `WS_EX_TOOLWINDOW`，不设置 `WS_EX_APPWINDOW`，没有每卡片一个的隐藏 owner。照片选择等按钮仍通过 WPF 处理，拖动使用原生窗口消息和捕获。

## 验证

不显示窗口、不操作鼠标的逻辑、本地照片和隐藏原生宿主检查：

```powershell
dotnet run --project tests/Tile.CoreChecks.csproj -c Release -- ../artifacts/core-checks
```

下面的旧版 smoke 会临时创建测试窗口，桌面操作被禁止时不要运行：

```powershell
dotnet run -c Release -- --smoke-test ../artifacts/windows
# 可选：实际请求 Open-Meteo，并保存独立的天气结果与渲染图
dotnet run -c Release -- --check-weather ../artifacts/weather
```

隔离配置，验证 WPF 加载、布局往返、网格与天气代码，输出卡片及组件库 PNG。不会变更真实配置或嵌入桌面。真实桌面验证需另行执行。

## 打包

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -o dist/win-x64
```

当前为便携 demo，没有 MSIX、开机自启或安装器。未实现 Explorer 右键菜单注入、真毛玻璃、混合 DPI 完整恢复、照片轮播。Google 相册尚未接入，需先配置项目 OAuth 客户端及 Photos Picker API。代码采用 Apache-2.0，见本目录 LICENSE。
