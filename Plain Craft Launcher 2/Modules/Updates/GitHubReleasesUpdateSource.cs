// GitHubReleasesUpdateSource.cs
// 通过 GitHub Releases API 检测 PCL-In/desktop 仓库的最新发布版本。
// 发布规则:
// - Release tag: 遵循 SemVer,带或不带 "v" 前缀均可(例如 "v1.0.0" 或 "1.0.0")
// - Asset 命名: "PCL-In-x64.exe" / "PCL-In-arm64.exe"(匹配当前 UpdateArch)
// - Asset 必须为 .exe 类型;可选提供 .sha256 文件用于校验
//
// 优点:
// - 无需维护 update.json,只需在 GitHub 发布新 release
// - 与 GitHub workflow 自然集成(workflow 在 release 时上传对应 asset)
// - GitHub 自行提供 SHA-256 校验

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using PCL.Core.App;
using PCL.Core.App.Localization;
using PCL.Core.Utils;
using PCL.Core.Utils.OS;
using PCL.Network;
using PCL.Network.Loaders;

namespace PCL;

public class GitHubReleasesUpdateSource : IUpdateSource
{
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _assetPrefix; // 例如 "PCL-In"
    private const string GithubApiBase = "https://api.github.com";
    private static readonly HttpClient _http = new()
    {
        // 默认 100 秒；网络不通时没必要让更新检查卡这么久
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders =
        {
            { "User-Agent", "PCL-In-Updater" },
            { "Accept", "application/vnd.github+json" }
        }
    };

    public GitHubReleasesUpdateSource(string owner, string repo, string assetPrefix = "PCL-In")
    {
        _owner = owner;
        _repo = repo;
        _assetPrefix = assetPrefix;
        SourceName = $"GitHub:{owner}/{repo}";
    }

    public string SourceName { get; set; }

    // 缓存最新 release 信息(按 channel 缓存:stable/beta)
    private static readonly Dictionary<string, (string Tag, string Name, string AssetUrl, string Sha256, int Code, string Changelog)> _cache = new();

    public bool IsAvailable() => true;

    public bool RefreshCache()
    {
        // 同步触发缓存填充
        try
        {
            _ = GetLatestAsync(UpdateChannel.stable).GetAwaiter().GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public VersionDataModel GetLatestVersion(UpdateChannel channel, UpdateArch arch)
    {
        var key = $"{channel}_{arch}";
        if (!_cache.ContainsKey(key))
        {
            try
            {
                var info = GetLatestAsync(channel).GetAwaiter().GetResult();
                var archName = arch == UpdateArch.arm64 ? "arm64" : "x64";
                var asset = info.Assets.FirstOrDefault(a =>
                    a["name"]?.ToString() == $"{_assetPrefix}-{archName}.exe");
                var shaAsset = info.Assets.FirstOrDefault(a =>
                    a["name"]?.ToString() == $"{_assetPrefix}-{archName}.exe.sha256");
                if (asset is null)
                    throw new Exception($"未找到匹配 {_assetPrefix}-{archName}.exe 的 release asset");
                var sha256 = "";
                if (shaAsset is not null)
                    sha256 = _http.GetStringAsync(GithubProxyHelper.Apply(shaAsset["browser_download_url"]!.ToString())).GetAwaiter().GetResult().Trim();
                _cache[key] = (info.VersionTag, info.VersionTag, GithubProxyHelper.Apply(asset["browser_download_url"]!.ToString()), sha256, info.Code, info.Changelog);
            }
            catch (Exception ex)
            {
                ModBase.Log(ex, $"[Update] GitHub Releases API 获取失败");
                // Changelog 必须给空串：UpdateStart 会把它写进临时文件，null 会在那里抛异常，
                // 最后变成一个和真实原因无关的「获取启动器更新失败，请检查网络连接」。
                return new VersionDataModel
                {
                    VersionName = ModBase.versionBaseName,
                    VersionCode = ModBase.versionCode,
                    Changelog = string.Empty
                };
            }
        }

        var cached = _cache[key];
        return new VersionDataModel
        {
            VersionName = cached.Name,
            VersionCode = cached.Code,
            Sha256 = cached.Sha256,
            Source = SourceName,
            Changelog = cached.Changelog
        };
    }

    public bool IsLatest(UpdateChannel channel, UpdateArch arch, SemVer currentVersion, int currentVersionCode)
    {
        try
        {
            var info = GetLatestAsync(channel).GetAwaiter().GetResult();
            // 把 GitHub tag 名(可能带 "v" 前缀)解析为 SemVer 与当前版本比较
            var latestTag = info.Tag.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? info.Tag[1..] : info.Tag;
            return currentVersion >= SemVer.Parse(latestTag);
        }
        catch
        {
            // 网络失败时不要阻塞,返回 true(避免误报)
            return true;
        }
    }

    public VersionAnnouncementDataModel GetAnnouncementList()
    {
        // PCL-In:公告取自本仓库最新 Release 的更新日志(body)。
        // 你每次发布 release 时写的说明,会自动成为启动公告。
        try
        {
            var info = GetLatestAsync(UpdateChannel.stable).GetAwaiter().GetResult();
            if (string.IsNullOrWhiteSpace(info.Changelog))
                return new VersionAnnouncementDataModel { Content = new List<VersionAnnouncementContentModel>() };

            return new VersionAnnouncementDataModel
            {
                Content = new List<VersionAnnouncementContentModel>
                {
                    new()
                    {
                        Id = $"gh-{info.VersionTag}",
                        Title = $"PCL-In {info.VersionTag} 更新公告",
                        Detail = info.Changelog,
                        Date = DateTime.Now.ToString("yyyy-MM-dd"),
                        Btn1 = new AnnouncementBtnInfoModel
                        {
                            Text = "查看发布页",
                            Command = "打开网页",
                            CommandParameter = $"https://github.com/{_owner}/{_repo}/releases/tag/{info.Tag}"
                        }
                    }
                }
            };
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[Update] 获取 GitHub 公告失败");
            return new VersionAnnouncementDataModel { Content = new List<VersionAnnouncementContentModel>() };
        }
    }

    public List<ModLoader.LoaderBase> GetDownloadLoader(UpdateChannel channel, UpdateArch arch, string output)
    {
        var key = $"{channel}_{arch}";
        var loaders = new List<ModLoader.LoaderBase>();

        // 步骤 1:获取最新 release + 对应 asset URL
        loaders.Add(new ModLoader.LoaderTask<int, int>(Lang.Text("Update.Task.GetVersionInfo"), _ =>
        {
            // 强制刷新缓存
            _cache.Remove(key);
            var info = GetLatestAsync(channel).GetAwaiter().GetResult();
            var archName = arch == UpdateArch.arm64 ? "arm64" : "x64";
            var asset = info.Assets.FirstOrDefault(a =>
                a["name"]?.ToString() == $"{_assetPrefix}-{archName}.exe");
            if (asset is null)
                throw new Exception($"未找到匹配 {_assetPrefix}-{archName}.exe 的 release asset。请确认 release 已上传对应架构的资源。");

            var downloadUrl = GithubProxyHelper.Apply(asset["browser_download_url"]!.ToString());
            ModBase.Log($"[Update] GitHub release URL: {downloadUrl}");

            // 步骤 2:下载到临时目录,然后直接复制到 output(SHA-256 校验由 GitHub 自带)
            var tempPath = Path.Combine(ModBase.pathTemp, $"Cache/Update/Download/{_assetPrefix}-{archName}.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

            // 使用现有 LoaderDownload 框架
            var downloadTask = new LoaderDownload(
                Lang.Text("Update.Task.DownloadFile"),
                new List<DownloadFile> { new(new[] { downloadUrl }, tempPath) }
            );
            // 同步等待下载完成(WaitForExit 内部已 Start 并轮询 State)
            downloadTask.WaitForExit();

            // 复制到目标路径
            File.Copy(tempPath, output, overwrite: true);
            ModBase.Log($"[Update] 已写入 {output}");
        }));

        return loaders;
    }

    // 拉取最新 release(按 channel:stable 拉 latest,beta 拉 latest prerelease)
    private async Task<GitHubReleaseInfo> GetLatestAsync(UpdateChannel channel)
    {
        // stable:tag 形式 v1.0.0,非 prerelease
        // beta:tag 形式 v1.0.0-beta.1,prerelease
        var url = channel == UpdateChannel.beta
            ? $"repos/{_owner}/{_repo}/releases?per_page=20"
            : $"repos/{_owner}/{_repo}/releases/latest";

        // PCL-In:应用 GitHub 加速代理(ghproxy)并首次询问
        GithubProxyHelper.EnsureAsked();
        // 注意：api.github.com 不会被 GithubProxyHelper 加前缀（ghproxy 不支持代理 GitHub API），
        // 所以这里始终是直连，走加速的只有发布资源的下载链接。
        var requestUrl = GithubProxyHelper.Apply($"{GithubApiBase}/{url}");
        using var resp = await _http.GetAsync(requestUrl);
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"GitHub Releases API 返回 {(int)resp.StatusCode} {resp.ReasonPhrase}：{requestUrl}");
        var json = await resp.Content.ReadAsStringAsync();

        JsonNode? node;
        if (channel == UpdateChannel.beta)
        {
            // 数组,找第一个 prerelease
            var arr = JsonNode.Parse(json) as JsonArray
                ?? throw new Exception("GitHub API 返回格式异常(非数组)");
            node = arr.FirstOrDefault(r => r?["prerelease"]?.GetValue<bool>() == true);
            if (node is null)
            {
                // 仓库里暂时没有预发布版本时退回正式版：否则 beta 构建即使网络完全正常，
                // 也永远拿不到版本信息（旧版还会因此弹出一个与网络无关的报错）。
                ModBase.Log("[Update] 仓库中没有 beta（预发布）版本，退回最新的正式版本");
                node = arr.FirstOrDefault(r => r?["prerelease"]?.GetValue<bool>() != true);
            }
            if (node is null)
                throw new Exception("未找到 beta(pre-release) release，且仓库中没有可用的正式版");
        }
        else
        {
            node = JsonNode.Parse(json);
        }

        var tag = node!["tag_name"]?.ToString() ?? "";
        var name = node!["name"]?.ToString() ?? tag;
        var body = node!["body"]?.ToString() ?? "";

        // 计算 version code:基于 tag 的 SemVer 解析失败时,用 hash 后几位做兜底
        var versionTag = tag.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? tag[1..] : tag;
        int code;
        try
        {
            // 简易 version code:用 SemVer 整数 + 部分 prerelease 数值
            var semver = SemVer.Parse(versionTag);
            code = semver.Major * 1000000 + semver.Minor * 10000 + semver.Patch * 100;
            // 如果有 prerelease 数字,加上去(beta.1 -> +1,beta.2 -> +2)
            var preMatch = System.Text.RegularExpressions.Regex.Match(versionTag, @"-beta(?:\.(\d+))?");
            if (preMatch.Success && int.TryParse(preMatch.Groups[1].Value, out var preNum))
                code += preNum;
        }
        catch
        {
            // 兜底:用 tag 字符串 hash
            code = tag.GetHashCode() & 0x7FFFFFFF;
        }

        var assets = (node!["assets"] as JsonArray)?.ToList() ?? new List<JsonNode?>();

        return new GitHubReleaseInfo
        {
            Tag = tag,
            VersionTag = versionTag,
            Name = name,
            Changelog = body,
            Code = code,
            Assets = assets
        };
    }

    private record GitHubReleaseInfo
    {
        public string Tag { get; init; } = "";
        public string VersionTag { get; init; } = "";
        public string Name { get; init; } = "";
        public string Changelog { get; init; } = "";
        public int Code { get; init; }
        public List<JsonNode?> Assets { get; init; } = new();
    }
}
