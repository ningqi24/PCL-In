# PCL-In 下载站

这是 **PCL-In** 的官方下载站，部署于 <https://pclin.astras.cc>。

## 目录说明

- `index.html` — 主页面（下载 / 功能 / 校验 / FAQ）
- `assets/css/style.css` — 样式
- `assets/js/app.js` — 从 GitHub Releases 自动拉取最新版本信息与校验值
- `CNAME` — 自定义域名 `pclin.astras.cc`
- `.nojekyll` — 禁用 Jekyll 处理，避免特定目录/文件名被干扰

## 数据来源

下载链接、版本号、SHA256、文件大小均自动从 GitHub Releases 获取（读取
`https://api.github.com/repos/ningqi24/PCL-In/releases/latest`）。当无法联网或
接口不可用时，页面会回退到内置的当前版本信息，因此地址始终可用。

## 部署方式（二选一）

### 1. GitHub Pages（推荐）

仓库已包含 `.github/workflows/pages.yml`，会将本目录（`download/`）发布为
GitHub Pages 站点。

- 在仓库 **Settings → Pages** 中，把 Source 设为 **GitHub Actions**；
- 自定义域名按提示配置（本站 `CNAME` 已写为 `pclin.astras.cc`）；
- 在 DNS 处把 `pclin.astras.cc` 的 CNAME 指向 `<用户名>.github.io`。

### 2. Cloudflare Pages 等第三方静态托管

多数静态托管商可直接连接本仓库并指定“构建输出目录 / 根目录”为 `download`，
无需构建命令，保存即自动发布。

## 更新版本

新增 GitHub Release 后，只要发布包含 `PCL-In-x64.exe` / `PCL-In-arm64.exe`
（及其 `.asc` / `.sha256`），下载站会自动展示新版本号与下载地址，无需改代码。
