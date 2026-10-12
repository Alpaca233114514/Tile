# Google 相册接入条件

本地照片已经实现。Google Photos 尚未接入，当前不会出现无效的“已连接”或“同步相册”按钮。

可行路线是官方 Photos Picker API：应用 OAuth 授权 → 创建选择会话 → 用户在 Google 页面选择照片 → 按服务指定间隔轮询 → 读取选中照片 → 下载到本地缓存供桌面展示 → 关闭会话。缓存文件继续复用两端各自的本地照片渲染路径。

开发前需要 Tile 对应的 Google Cloud 项目、已启用的 Photos Picker API、桌面应用 OAuth 客户端与同意屏幕。普通 Google 登录和已有 Drive 连接不能替代这个配置。当前仓库没有这项应用配置，因此本轮不宣称 Google 相册可用。

只申请 `photospicker.mediaitems.readonly` 范围；凭证不进源码、日志或命令参数。授权完成后仅访问用户在 Picker 明确选中的照片。Library API 不能作为任意读取用户已有相册的替代入口。

来源：[官方概览](https://developers.google.com/photos/overview/about)、[配置应用](https://developers.google.com/photos/overview/configure-your-app)、[Picker 流程](https://developers.google.com/photos/picker/guides/get-started-picker)。
