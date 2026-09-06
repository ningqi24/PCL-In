// GithubProxyHelper.cs
// PCL-In:为 GitHub 相关 URL 提供 ghproxy 加速代理支持。
// 当 Config.Network.GithubProxy 开启时,把 github.com / api.github.com / raw.githubusercontent.com
// 等 URL 加上 ghproxy 前缀,以改善国内访问 GitHub 资源的速度。

using System;
using PCL.Core.App;

namespace PCL;

public static class GithubProxyHelper
{
    // ghproxy 前缀(可自行更换为其他镜像,如 gh-proxy.com / mirror.ghproxy.com 等)
    public const string DefaultProxyPrefix = "https://ghproxy.net/";

    // 判断该 URL 是否可能是 GitHub 相关资源(需要加速)
    public static bool IsGithubUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        var u = url.ToLowerInvariant();
        return u.Contains("github.com") ||
               u.Contains("raw.githubusercontent.com") ||
               u.Contains("api.github.com") ||
               u.Contains("raw.gitcode.com") ||
               u.Contains("gitee.com");
    }

    // 若开启了 GitHub 加速,返回加前缀后的 URL;否则原样返回
    public static string Apply(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        if (!Config.Network.GithubProxy) return url;
        if (!IsGithubUrl(url)) return url;
        // 已经加了 ghproxy 前缀则不重复
        if (url.StartsWith(DefaultProxyPrefix, StringComparison.OrdinalIgnoreCase)) return url;
        return DefaultProxyPrefix + url;
    }

    // 若用户尚未选择过,弹窗询问是否开启 GitHub 加速
    public static void EnsureAsked()
    {
        if (Config.Network.GithubProxyAsked) return;
        Config.Network.GithubProxyAsked = true;
        try
        {
            var choice = ModMain.MyMsgBox(
                "检测到启动器需要访问 GitHub 资源(如更新、公告、主页预设)。\n是否使用 GitHub 加速代理(ghproxy.net)来提升访问速度?\n\n如果网络访问 GitHub 正常,可以选择\"不使用\"。",
                "GitHub 加速代理",
                "使用加速",
                "不使用",
                "稍后再说");
            if (choice == 1)
            {
                Config.Network.GithubProxy = true;
                ModBase.Log("[System] 用户已启用 GitHub 加速代理(ghproxy)");
            }
            else if (choice == 2)
            {
                Config.Network.GithubProxy = false;
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "[System] GitHub 加速代理询问失败");
        }
    }
}
