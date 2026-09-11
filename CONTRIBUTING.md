# 貢獻指南

歡迎貢獻!如果你要對 PCL-In 提交貢獻,請遵循以下規範。

## 分支規範

- 主分支:`dev` — 所有 PR 請發到此分支
- 提交規範:遵循 Angular Conventional Commits,例如:
 - `feat: 新增 XXX 功能`
 - `fix: 修復 XXX 問題`
 - `docs: 更新 XXX 文檔`
 - `refactor: 重構 XXX 模組`

## 編譯要求

- .NET 10 SDK
- Windows 10 1809 或更高(僅在 Windows 上驗證編譯;macOS/Linux 可交叉編譯但無法運行)

## 提交 PR 流程

1. Fork 此倉庫到你的 GitHub 賬號
2. 從 `dev` 建立新分支
4.  提交更改並推送
5. 在 GitHub 上開啟 PR,目標分支為 `PCL-In/PCL-In:dev`

## 重要提醒

- **不要修改 `Plain Craft Launcher 2/LICENCE`**(法律文件)
- **不要修改 `Plain Craft Launcher 2/metadata.json` 的 `licenses[]`**(第三方版權資訊)
- 修改其他內容前請確認不會破壞上游 attribution
