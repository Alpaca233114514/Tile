# Tile for GNOME · v0.1 demo

独立 GJS / St / Clutter 扩展，目标 GNOME Shell 45–49（ES modules）。当前尚未在 Debian 的真实 GNOME 会话验收；声明版本是测试目标。GNOME 43 的 Debian 12 不支持此 ESM demo，需 GNOME 45+，例如 Debian 13 的 GNOME 48。

## 安装

需要 Python 3、GLib schemas 编译器、GNOME Shell、GTK4 / libadwaita、Soup 3。Debian 的系统包可提供这些依赖。图形安装助手另需 `python3-gi` 和 `gir1.2-gtk-4.0`。

```sh
python3 tools/install.py
```

点击“安装 demo”，注销再登录，通过“扩展”应用启用 Tile。不需要普通用户手写配置。已有同 UUID 扩展时不覆盖；升级前请备份源码与 dconf。

开发者命令行方式：

```sh
python3 tools/package.py
gnome-extensions install dist/tile-widgets@tile.local.shell-extension.zip
# Wayland 下注销再登录
gnome-extensions enable tile-widgets@tile.local
gnome-extensions prefs tile-widgets@tile.local
```

## 使用

启用后默认在桌面展示，不自动打开组件库。顶栏网格按钮打开组件库或直接添加；背景原生右键菜单会尝试添加/编辑入口。直接按住卡片非按钮区域拖动，越过 4 px 阈值后移动，松手网格吸附并保存；Esc 取消拖动。编辑入口只突出显示卡片，不是拖动的前置条件。组件不会脱离桌面层。

组件库提供时钟、天气、照片和日历，可以添加多份。天气搜索城市后选择并应用，每 15 分钟更新，失败后两分钟重试；Open-Meteo 免费接口需联网。照片卡片直接点击“选择本地照片”调用系统文件选择器，也可在偏好设置中更换。使用桌面 FileChooser portal（Debian GNOME 通常由 xdg-desktop-portal-gnome 提供），不上传原图。主题提供系统 / 浅色 / 深色。

配置由 GSettings 保存于 `/org/gnome/shell/extensions/tile-widgets/`，停用/卸载不主动删除配置。异步请求、计时器、事件捕获和背景菜单条目在停用时释放。

## 开发检查

```sh
node --test tests/*.test.js
glib-compile-schemas --strict --dry-run schemas
```

无 npm / pip 依赖。Node 仅用于平台无关规则测试，运行扩展无需 Node。Windows 上可以检查 JS 和构建源码包，但不能替代 Shell / GTK 真机验证。

已知边界：`_backgroundGroup` 是 Shell 私有接口；Desktop Icons NG 等扩展可能覆盖卡片或接管背景菜单，使用顶栏入口。尚未验证锁屏、概览、Wayland 拖拽、系统照片选择器、混合缩放、多屏拔插与长期运行。GNOME 卡片尚不支持从文件管理器拖入照片，请使用选择按钮。暂不含真实模糊、照片轮播、便签与待办。Google 相册需项目 OAuth 客户端及 Photos Picker API，尚未接入。使用系统字体，Apache-2.0。
