// PCL-In download site — 普通模式 + 友好模式，自动从 GitHub Releases 获取版本
(function () {
  "use strict";

  const REPO = "ningqi24/PCL-In";
  const API = "https://api.github.com/repos/" + REPO + "/releases";
  const DL = "https://github.com/" + REPO + "/releases/download";
  const ARCH_ORDER = ["x64", "arm64"];
  const STORE_KEY = "pclinDownloadMode";

  // 内置回退数据（与 v1.0.0 保持一致），在无法联网时使用
  const STATIC = {
    tag: "v1.0.0",
    name: "PCL-In v1.0.0",
    publishedAt: "2026-09-06",
    assets: [
      { name: "PCL-In-x64.exe", size: 17.79 * 1048576, sha256: "917e3c9fba8a044e1c50226c62ea299b13d8dfa22dc5e418eb6a90e85e2bc7c1" },
      { name: "PCL-In-arm64.exe", size: 17.56 * 1048576, sha256: "ba3ea7acb07865670a3bf3cafc94a172abb34107f0a8ebdf641d436f4c54f944" }
    ]
  };

  const state = { releases: [], latest: null, mode: "normal", device: { isMobile: false, arch: null } };
  const fm = { release: null, assets: [], arch: null, pkg: null };

  /* ---------------- helpers ---------------- */
  const $ = (id) => document.getElementById(id);
  function mb(bytes) { return bytes ? (bytes / 1048576).toFixed(2) + " MB" : ""; }
  function cleanSha(d) { return d ? String(d).replace(/^sha256:/i, "").trim() : ""; }
  function fmtDate(iso) { try { const d = new Date(iso); return isNaN(d) ? "" : d.toISOString().slice(0, 10); } catch (e) { return ""; } }
  function assetUrl(tag, name) { return DL + "/" + tag + "/" + name; }
  function isVerificationFile(n) { const s = n.toLowerCase(); return s.endsWith(".asc") || s.endsWith(".sha256"); }
  function archOf(name) {
    const n = name.toLowerCase();
    if (n.includes("arm64") || n.includes("aarch64")) return "arm64";
    if (n.includes("x64") || n.includes("amd64")) return "x64";
    return null;
  }

  function normalizeRelease(rel) {
    const raw = rel.assets || [];
    const digestMap = {};
    raw.forEach((a) => { if (a.digest) digestMap[a.name] = cleanSha(a.digest); });
    const tag = rel.tag_name;
    const assets = raw.filter((a) => !isVerificationFile(a.name)).map((a) => ({
      name: a.name,
      tag: tag,
      url: a.browser_download_url || assetUrl(tag, a.name),
      size: a.size,
      sha256: cleanSha(a.digest) || digestMap[a.name + ".sha256"] || ""
    }));
    return { tag: tag, name: rel.name || "PCL-In " + tag, publishedAt: rel.published_at, assets: assets };
  }

  function staticRelease() {
    const tag = STATIC.tag;
    return {
      tag: tag, name: STATIC.name, publishedAt: STATIC.publishedAt,
      assets: STATIC.assets.map((a) => ({ name: a.name, tag: tag, url: assetUrl(tag, a.name), size: a.size, sha256: a.sha256 }))
    };
  }

  /* ---------------- 普通模式渲染 ---------------- */
  function applyNormal(rel) {
    state.latest = rel;
    $("hero-version").textContent = rel.tag;
    $("release-name").textContent = rel.name;
    $("hero-updated").textContent = rel.publishedAt ? fmtDate(rel.publishedAt) : STATIC.publishedAt;
    const link = $("release-link"); if (link) link.setAttribute("href", "https://github.com/" + REPO + "/releases/tag/" + rel.tag);

    ARCH_ORDER.forEach(function (arch) {
      const a = rel.assets.find((x) => archOf(x.name) === arch);
      if (!a) return;
      if (a.size) { const s = $("size-" + arch); if (s) s.textContent = mb(a.size); }
      const dl = $("dl-" + arch); if (dl) dl.setAttribute("href", a.url);
      const hero = $("dl-hero-" + arch); if (hero) hero.setAttribute("href", a.url);
      const sig = $("sig-" + arch); if (sig) sig.setAttribute("href", a.url + ".asc");
      const sha = a.sha256 || "";
      if (sha) {
        const code = $("code-" + arch); if (code) code.textContent = a.name + "\n" + sha;
        const card = document.querySelector(".dl-card[data-arch='" + arch + "']");
        const btn = card && card.querySelector("[data-copy]");
        if (btn) { btn.dataset.copy = sha; }
      }
    });
  }

  /* ---------------- 模式切换 ---------------- */
  function switchMode(mode) {
    state.mode = mode;
    document.querySelectorAll(".mode-btn").forEach(function (b) {
      const on = b.dataset.mode === mode;
      b.classList.toggle("active", on);
      b.setAttribute("aria-selected", on ? "true" : "false");
    });
    $("view-normal").hidden = mode !== "normal";
    $("view-friendly").hidden = mode !== "friendly";
    const hint = $("mode-hint");
    if (hint) hint.textContent = mode === "friendly"
      ? "友好模式：选择你的设备类型，接着按引导选择版本与配置，自动匹配最适合的下载文件。"
      : "普通模式：直接列出全部下载文件。友好模式：按引导一步步选择，自动匹配适合你设备的文件。";
    try { localStorage.setItem(STORE_KEY, mode); } catch (e) {}
    if (mode === "friendly") initFriendlyBadges();
  }

  /* ---------------- 设备识别 ---------------- */
  async function detectDevice() {
    const u = (navigator.userAgent || "").toLowerCase();
    const isMobile = /android|iphone|ipad|ipod|harmony|openharmony/.test(u);
    let arch = null;
    try {
      if (navigator.userAgentData && navigator.userAgentData.getHighEntropyValues) {
        const hv = await navigator.userAgentData.getHighEntropyValues(["architecture", "bitness"]);
        if (hv.architecture === "arm") arch = "arm64";
        else if (hv.architecture === "x86" && hv.bitness === "64") arch = "x64";
      }
    } catch (e) {}
    if (!arch) {
      if (/arm64|aarch64/.test(u)) arch = "arm64";
      else if (/wow64|win64|x64|amd64/.test(u)) arch = "x64";
    }
    state.device = { isMobile: isMobile, arch: arch };
  }

  function initFriendlyBadges() {
    const { isMobile, arch } = state.device;
    document.querySelectorAll(".platform-card").forEach(function (card) {
      const badge = card.querySelector("[data-badge]");
      card.classList.remove("recommended", "not-suitable");
      if (!badge) return;
      badge.hidden = true;
      badge.classList.remove("muted");
      if (isMobile) {
        card.classList.add("not-suitable");
        badge.hidden = false; badge.classList.add("muted"); badge.textContent = "不适合此设备";
      } else if (arch && card.dataset.arch === arch) {
        card.classList.add("recommended");
        badge.hidden = false; badge.textContent = "适合此设备";
      }
    });
  }

  /* ---------------- 友好模式：对话框 ---------------- */
  function openFriendlyModal(arch) {
    fm.release = null; fm.assets = []; fm.arch = arch; fm.pkg = null;
    $("fm-title").textContent = arch === "arm64" ? "Windows · ARM64" : "Windows · x64";
    const loading = $("fm-loading"), content = $("fm-content");
    loading.hidden = false;
    loading.innerHTML = '<span class="fm-spinner"></span><span>正在获取版本列表...</span>';
    content.hidden = true;
    $("fm-version-dropdown").hidden = true;
    $("friendly-modal").classList.add("show");
    document.body.style.overflow = "hidden";
    loadFriendlyVersions();
  }
  function closeFriendlyModal() {
    $("friendly-modal").classList.remove("show");
    document.body.style.overflow = "";
  }

  async function loadFriendlyVersions() {
    let rels = state.releases;
    if (!rels.length) rels = await fetchReleases();
    if (!rels.length) { showFriendlyError("暂无可用的下载版本"); return; }
    state.releases = rels;
    finishLoadingVersions(rels);
  }

  function finishLoadingVersions(rels) {
    const dd = $("fm-version-dropdown");
    dd.innerHTML = "";
    rels.forEach(function (rel, i) {
      const div = document.createElement("div");
      div.className = "fm-version-option" + (i === 0 ? " selected" : "");
      div.textContent = rel.tag + " (" + fmtDate(rel.publishedAt) + ")";
      div.addEventListener("click", function (e) { e.stopPropagation(); selectVersion(i); });
      dd.appendChild(div);
    });
    fm.releases = rels;
    selectVersion(0, true);
    $("fm-loading").hidden = true;
    $("fm-content").hidden = false;
    renderFriendlySteps();
  }

  function selectVersion(idx, silent) {
    const rels = fm.releases || state.releases;
    const rel = rels[idx];
    if (!rel) return;
    fm.release = rel;
    fm.assets = rel.assets.filter(function (a) { return !isVerificationFile(a.name); });
    $("fm-version-btn").textContent = rel.tag + " (" + fmtDate(rel.publishedAt) + ")";
    const dd = $("fm-version-dropdown");
    Array.from(dd.children).forEach(function (c, i) { c.classList.toggle("selected", i === idx); });
    dd.hidden = true;
    $("fm-version-btn").classList.remove("open");
    if (!silent) {
      // 保持已选架构（若新版本仍有），否则回退到首个可用
      const archs = availableArchs();
      if (!fm.arch || archs.indexOf(fm.arch) < 0) fm.arch = archs[0] || null;
      fm.pkg = null;
      renderFriendlySteps();
    }
  }

  function availableArchs() {
    const set = new Set();
    fm.assets.forEach(function (a) { const ar = archOf(a.name); if (ar) set.add(ar); });
    return Array.from(set).sort(function (a, b) { return ARCH_ORDER.indexOf(a) - ARCH_ORDER.indexOf(b); });
  }
  function availablePkgs() {
    const set = new Set();
    fm.assets.forEach(function (a) { const n = a.name.toLowerCase(); if (n.endsWith(".exe")) set.add(".exe"); else if (n.endsWith(".zip")) set.add(".zip"); });
    return Array.from(set);
  }
  function pkgLabel(k) { return k === ".zip" ? "便携版 (.zip)" : ".exe 安装程序"; }
  function pkgTip(k) { return k === ".zip" ? "解压即用，适合免安装" : "标准安装程序，适合大多数用户"; }

  function renderFriendlySteps() {
    if (!fm.assets.length) { showFriendlyError("该版本暂无当前平台的文件"); return; }
    const archs = availableArchs();
    if (!fm.arch || archs.indexOf(fm.arch) < 0) fm.arch = archs[0] || null;
    const pkgs = availablePkgs();
    if (!fm.pkg || pkgs.indexOf(fm.pkg) < 0) fm.pkg = pkgs[0] || null;

    const archRow = $("fm-arch-btns");
    $("fm-arch-section").hidden = archs.length === 0;
    archRow.innerHTML = "";
    archs.forEach(function (ar) {
      const b = document.createElement("button");
      b.type = "button";
      b.className = "fm-step-btn" + (fm.arch === ar ? " selected" : "");
      b.innerHTML = '<span class="step-btn-label">' + (ar === "arm64" ? "ARM64" : "x64 (64 位)") + "</span>" +
        '<span class="step-btn-tip">' + (ar === "arm64" ? "ARM 架构设备专用（骁龙等）" : "64 位处理器，性能最佳") + "</span>";
      b.addEventListener("click", function () { fm.arch = ar; fm.pkg = null; renderFriendlySteps(); });
      archRow.appendChild(b);
    });

    const pkgRow = $("fm-pkg-btns");
    $("fm-pkg-section").hidden = pkgs.length === 0;
    pkgRow.innerHTML = "";
    pkgs.forEach(function (k) {
      const b = document.createElement("button");
      b.type = "button";
      b.className = "fm-step-btn" + (fm.pkg === k ? " selected" : "");
      b.innerHTML = '<span class="step-btn-label">' + pkgLabel(k) + "</span>" +
        '<span class="step-btn-tip">' + pkgTip(k) + "</span>";
      b.addEventListener("click", function () { fm.pkg = k; renderFriendlySteps(); });
      pkgRow.appendChild(b);
    });

    updateFriendlyResult();
  }

  function updateFriendlyResult() {
    const section = $("fm-result-section");
    const list = $("fm-btn-list");
    const matched = fm.assets.filter(function (a) {
      const n = a.name.toLowerCase();
      if (fm.arch && archOf(a.name) !== fm.arch) return false;
      if (fm.pkg === ".exe" && !n.endsWith(".exe")) return false;
      if (fm.pkg === ".zip" && !n.endsWith(".zip")) return false;
      return true;
    });

    if (!matched.length) {
      section.hidden = false;
      list.innerHTML = '<div class="fm-btn-item fm-btn-empty">没有匹配的文件，请调整上方选择</div>';
      return;
    }
    section.hidden = false;
    list.innerHTML = "";
    matched.forEach(function (a) {
      const item = document.createElement("div");
      item.className = "fm-btn-item";
      const info = document.createElement("div");
      info.className = "fm-btn-info";
      const nm = document.createElement("div");
      nm.className = "fm-btn-name"; nm.textContent = a.name;
      const tip = document.createElement("div");
      tip.className = "fm-btn-tip";
      const parts = [];
      if (a.size) parts.push(mb(a.size));
      parts.push(fm.arch === "arm64" ? "ARM64 架构设备专用" : "64 位系统，兼容大多数电脑");
      tip.textContent = parts.join("  ·  ");
      info.appendChild(nm); info.appendChild(tip);

      const group = document.createElement("div");
      group.style.cssText = "display:flex;gap:6px;align-items:center;flex-shrink:0";
      if (a.sha256) {
        const sb = document.createElement("button");
        sb.type = "button"; sb.className = "fm-btn-dl"; sb.textContent = "SHA256 校验";
        sb.addEventListener("click", function (e) { e.stopPropagation(); showShaModal(a.name, a.sha256); });
        group.appendChild(sb);
      }
      const dl = document.createElement("a");
      dl.className = "fm-btn-dl"; dl.textContent = "下载"; dl.href = a.url;
      dl.setAttribute("target", "_blank"); dl.setAttribute("rel", "noopener");
      group.appendChild(dl);

      item.appendChild(info); item.appendChild(group);
      list.appendChild(item);
    });
  }

  function showFriendlyError(msg) {
    const loading = $("fm-loading"), content = $("fm-content");
    loading.hidden = false;
    loading.innerHTML = '<span style="color:#dc2626">' + msg + "</span>";
    content.hidden = true;
  }

  /* ---------------- SHA256 弹窗 ---------------- */
  function showShaModal(name, sha) {
    $("sha-modal-name").textContent = name;
    $("sha-modal-hash").textContent = sha;
    $("sha-modal-cmd").textContent = 'certutil -hashfile "' + name + '" SHA256';
    $("sha-modal").classList.add("show");
  }
  function closeShaModal() { $("sha-modal").classList.remove("show"); }

  /* ---------------- 复制 ---------------- */
  function bindCopy(btn) {
    btn.addEventListener("click", function () {
      const target = btn.dataset.copyTarget;
      const text = target ? (($(target) || {}).textContent || "") : (btn.dataset.copy || "");
      function done() { const old = btn.textContent; btn.textContent = "已复制"; setTimeout(function () { btn.textContent = old; }, 1500); }
      if (navigator.clipboard && navigator.clipboard.writeText) { navigator.clipboard.writeText(text).then(done, done); }
      else {
        const ta = document.createElement("textarea"); ta.value = text; document.body.appendChild(ta); ta.select();
        try { document.execCommand("copy"); } catch (e) {}
        document.body.removeChild(ta); done();
      }
    });
  }

  /* ---------------- 数据获取 ---------------- */
  function fetchReleases() {
    return fetch(API + "?per_page=20", { headers: { Accept: "application/vnd.github+json" } })
      .then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); })
      .then(function (list) {
        if (!Array.isArray(list)) throw new Error("bad payload");
        const rels = list.filter(function (x) { return x && x.tag_name; }).map(normalizeRelease);
        return rels;
      })
      .catch(function () { return []; });
  }

  /* ---------------- 初始化 ---------------- */
  document.addEventListener("DOMContentLoaded", async function () {
    // 复制按钮
    document.querySelectorAll("[data-copy]").forEach(bindCopy);
    document.querySelectorAll("[data-copy-target]").forEach(bindCopy);

    // 模式切换
    document.querySelectorAll(".mode-btn").forEach(function (b) {
      b.addEventListener("click", function () { switchMode(b.dataset.mode); });
    });

    // 平台卡片
    document.querySelectorAll(".platform-card").forEach(function (c) {
      c.addEventListener("click", function () { openFriendlyModal(c.dataset.arch); });
    });

    // 对话框事件
    $("fm-close").addEventListener("click", closeFriendlyModal);
    $("friendly-modal").addEventListener("click", function (e) { if (e.target === this) closeFriendlyModal(); });
    $("sha-close").addEventListener("click", closeShaModal);
    $("sha-modal").addEventListener("click", function (e) { if (e.target === this) closeShaModal(); });
    $("fm-version-btn").addEventListener("click", function (e) {
      e.stopPropagation();
      const dd = $("fm-version-dropdown");
      const open = !dd.hidden;
      dd.hidden = open;
      $("fm-version-btn").classList.toggle("open", !open);
    });
    document.addEventListener("click", function (e) {
      const wrap = document.querySelector(".fm-version-select");
      const dd = $("fm-version-dropdown");
      if (wrap && !wrap.contains(e.target) && dd && !dd.hidden) { dd.hidden = true; $("fm-version-btn").classList.remove("open"); }
    });
    document.addEventListener("keydown", function (e) {
      if (e.key !== "Escape") return;
      if ($("friendly-modal").classList.contains("show")) closeFriendlyModal();
      else if ($("sha-modal").classList.contains("show")) closeShaModal();
    });

    // 先渲染内置数据，保证页面永不为空
    applyNormal(staticRelease());
    await detectDevice();

    // 默认进入友好模式（用户若曾手动切到普通模式则尊重其选择）
    let saved = null;
    try { saved = localStorage.getItem(STORE_KEY); } catch (e) {}
    switchMode(saved === "normal" ? "normal" : "friendly");

    const rels = await fetchReleases();
    if (rels.length) {
      state.releases = rels;
      applyNormal(rels[0]);
      if (state.mode === "friendly") initFriendlyBadges();
    }
  });
})();
