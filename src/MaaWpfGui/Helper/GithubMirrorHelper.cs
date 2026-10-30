// <copyright file="GithubMirrorHelper.cs" company="MaaAssistantArknights">
// Part of the MaaWpfGui project, maintained by the MaaAssistantArknights team (Maa Team)
// Copyright (C) 2021-2026 MaaAssistantArknights Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License v3.0 only as published by
// the Free Software Foundation, either version 3 of the License, or
// any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using MaaWpfGui.Configuration.Factory;
using MaaWpfGui.ViewModels.UI;

namespace MaaWpfGui.Helper;

/// <summary>
/// 国内公共 GitHub 代理工具：将 github.com / raw.githubusercontent.com 等直链
/// 改写为前缀式公共代理地址，缓解国内更新时连接 GitHub 卡住的问题。
/// 公共代理为免费公益服务，可用性与带宽均不稳定，因此会生成多个候选，
/// 由调用方自行做测速选优或逐个回退，原始链接始终保留在最后兜底。
/// </summary>
public static class GithubMirrorHelper
{
    /// <summary>
    /// 前缀式公共代理列表，拼接形式为 ｢代理前缀 + 完整 GitHub URL｣。
    /// </summary>
    private static readonly string[] ProxyPrefixes =
    [
        "https://ghproxy.net/",
        "https://gh-proxy.com/",
        "https://ghfast.top/",
    ];

    /// <summary>
    /// 可经公共代理转发的 GitHub 相关主机名（release 附件、仓库归档、raw 文件等）。
    /// </summary>
    private static readonly string[] GithubHosts =
    [
        "github.com",
        "www.github.com",
        "raw.githubusercontent.com",
        "objects.githubusercontent.com",
        "github-releases.githubusercontent.com",
        "codeload.github.com",
    ];

    /// <summary>
    /// Gets a value indicating whether 用户显式要求直连 GitHub：勾选了 ｢强制使用 GitHub｣，或已自行配置网络代理。
    /// </summary>
    private static bool PreferDirectConnection =>
        SettingsViewModel.VersionUpdateSettings.ForceGithubGlobalSource
        || !string.IsNullOrWhiteSpace(ConfigFactory.Root.Update.Proxy);

    /// <summary>
    /// 判断链接是否指向可代理的 GitHub 相关资源。
    /// </summary>
    /// <param name="url">待判断的链接。</param>
    /// <returns>属于受支持的 GitHub 主机返回 true，否则返回 false。</returns>
    public static bool IsGithubUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && GithubHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 按 ｢官方镜像 → 公共代理 → 原始链接兜底｣ 的顺序生成下载候选并去重。
    /// 用户勾选 ｢强制使用 GitHub｣ 或已自行配置代理时不追加公共代理，尊重其显式网络选择。
    /// </summary>
    /// <param name="githubUrl">原始下载链接。</param>
    /// <param name="officialMirrors">MaaApi 下发的官方镜像地址（如 S3/CDN），可选。</param>
    /// <returns>去重后的候选链接列表，至少包含原始链接。</returns>
    public static List<string> BuildDownloadCandidates(string githubUrl, IEnumerable<string>? officialMirrors = null)
    {
        var candidates = new List<string>();

        if (officialMirrors != null)
        {
            foreach (var mirror in officialMirrors)
            {
                AddCandidate(candidates, mirror);
            }
        }

        if (IsGithubUrl(githubUrl) && !PreferDirectConnection)
        {
            foreach (var prefix in ProxyPrefixes)
            {
                AddCandidate(candidates, prefix + githubUrl);
            }
        }

        AddCandidate(candidates, githubUrl);
        return candidates;
    }

    /// <summary>
    /// 追加非空且不重复的候选链接。
    /// </summary>
    /// <param name="candidates">候选列表。</param>
    /// <param name="url">候选链接。</param>
    private static void AddCandidate(List<string> candidates, string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || candidates.Contains(url))
        {
            return;
        }

        candidates.Add(url);
    }
}
