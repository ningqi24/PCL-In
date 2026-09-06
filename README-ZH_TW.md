# PCL-In

> 基於 **PCL Community Edition (PCL CE)** 的二次 fork。

PCL-In 是一個為非正版使用者(尤其是第三方皮膚站使用者)提供更好體驗的 Minecraft 啟動器。

## 與上游 PCL CE 的差異

- **去除強制正版驗證**:使用第三方皮膚站(Authlib / YggdrasilConnect)或離線檔案的使用者,不再被強制進入「試玩模式」。
- **去除所有贊助提示**:不彈出開助助窗,不引導使用者訪問原作者的愛發電。
- **更新機制獨立**:透過 GitHub Releases API 偵測本倉庫的新版本,不會覆蓋你的 fork 改動。

## 下載

前往 [Releases](https://github.com/ningqi24/PCL-In/releases) 頁面下載最新版本。

## 系統需求

- Windows 10 1809 (17763) 或更高
- [.NET 10 Desktop Runtime](https://get.dot.net/10)

## 致謝

本專案基於 [PCL Community Edition (PCL CE)](https://github.com/PCL-Community/PCL-CE) 開發,所有權利歸原作者 [龍騰貓躍](https://github.com/Meloong-Git/PCL) 及 [成都瓜皮龍科技有限公司](https://www.pclc.cc/) 所有。

上游第三方元件的版權資訊詳見 [`Plain Craft Launcher 2/metadata.json`](./Plain%20Craft%20Launcher%202/metadata.json) 的 `licenses[]` 陣列。

## 授權

- 根目錄 [`LICENSE`](./LICENSE) 遵循 Apache License 2.0
- [`Plain Craft Launcher 2/LICENCE`](./Plain%20Craft%20Launcher%202/LICENCE) 遵循上游《PCL 分發有限許可》
