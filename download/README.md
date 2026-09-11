# PCL-In 下载站

**PCL-In** 是基于 PCL Community Edition (PCL CE) 的二次 fork，面向非正版玩家（尤其第三方皮肤站用户），
去除强制正版验证、去除赞助提示、具备独立更新机制。本站是其官方下载站，部署于 <https://pclin.astras.cc>。

## 目录说明

- `index.html` — 主页面（下载 / 功能 / 校验 / FAQ）
- `assets/css/style.css` — 样式
- `assets/js/app.js` — 从 GitHub Releases 自动拉取版本信息、校验值与引导逻辑
- `CNAME` — 自定义域名 `pclin.astras.cc`
- `.nojekyll` — 禁用 Jekyll 处理，避免特定目录/文件名被干扰

## 两种下载模式

站点采用与 `download.astras` 下载站一致的**深色主题**（`#111` 背景 / `#eee` 文字 / `#66ccff` 强调色），
并提供**普通模式**与**友好模式**；**默认进入友好模式**，切换状态记录在 `localStorage`。

- **普通模式**：直接列出全部下载文件（x64 / ARM64 两个按钮 + GPG 签名 + SHA256）。
- **友好模式**：先选择设备类型（Windows x64 / ARM64）卡片，再在弹出的配置对话框中
  依次选择 **版本 → 处理器架构 → 安装包类型**，实时显示**匹配文件**并可直接下载或查看 SHA256。
  页面会通过 User-Agent / `navigator.userAgentData` 识别当前设备，给匹配的卡片打上
  「适合此设备」标记；移动端设备则标记为「不适合此设备」。

## 数据来源

下载链接、版本号、SHA256、文件大小均自动从 GitHub Releases 获取
（读取 `https://api.github.com/repos/ningqi24/PCL-In/releases`，友好模式的版本下拉框会列出最近 20 个版本）。
当无法联网或接口不可用时，页面会回退到内置的当前版本信息，因此地址始终可用。

## 部署方式（二选一）

### 1. GitHub Pages（推荐）

仓库已包含 `.github/workflows/pages.yml`，会将本目录（`download/`）发布为
GitHub Pages 站点（并在每次部署前清理历史的 `github-pages` artifact，避免
「multiple artifacts」报错）。

- 在仓库 **Settings → Pages** 中，把 Source 设为 **GitHub Actions**；
- 自定义域名按提示配置（本站 `CNAME` 已写为 `pclin.astras.cc`）；
- 在 DNS 处把 `pclin.astras.cc` 的 CNAME 指向 `<用户名>.github.io`。

### 2. Cloudflare Pages 等第三方静态托管

多数静态托管商可直接连接本仓库并指定“构建输出目录 / 根目录”为 `download`，
无需构建命令，保存即自动发布。

## 更新版本

新增 GitHub Release 后，只要发布包含 `PCL-In-x64.exe` / `PCL-In-arm64.exe`
（及其 `.asc` / `.sha256`），下载站的普通模式与友好模式都会自动展示新版本，无需改代码。
