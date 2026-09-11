# PCL-In

> 基于 **PCL Community Edition (PCL CE)** 的二次 fork。

PCL-In 是一个为非正版用户(尤其是第三方皮肤站用户)提供更好体验的 Minecraft 启动器。

## 与上游 PCL CE 的区别

- **去除强制正版验证**:使用第三方皮肤站(Authlib / YggdrasilConnect)或离线档案的玩家不再被强制进入"试玩模式"。
- **去除所有赞助提示**:不弹赞助窗,不引导用户访问原作者的爱发电。
- **更新机制独立**:通过 GitHub Releases API 检测本仓库的新版本,不会覆盖你的 fork 改动。

## 下载

前往 [Releases](https://github.com/PCL-In/PCL-In/releases) 页面下载最新版本。

## 系统要求

- Windows 10 1809 (17763) 或更高
- [.NET 10 Desktop Runtime](https://get.dot.net/10)

## 致谢

本项目基于 [PCL Community Edition (PCL CE)](https://github.com/PCL-Community/PCL-CE) 开发,所有权利归属原作者 [龙腾猫跃](https://github.com/Meloong-Git/PCL) 及 [成都瓜皮龙科技有限公司](https://www.pclc.cc/)。

上游第三方组件的版权信息见 [`Plain Craft Launcher 2/metadata.json`](./Plain Craft Launcher 2/metadata.json) 的 `licenses[]` 数组。

## 许可证

- 根目录 [`LICENSE`](./LICENSE) 遵循 Apache License 2.0
- [`Plain Craft Launcher 2/LICENCE`](./Plain%20Craft%20Launcher%202/LICENCE) 遵循上游《PCL 分发有限许可》
