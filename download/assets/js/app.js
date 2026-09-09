// PCL-In download site — auto-refresh latest release info from GitHub
(function () {
  "use strict";
  const REPO = "ningqi24/PCL-In";
  const API = "https://api.github.com/repos/" + REPO + "/releases/latest";
  const BASE = "https://github.com/" + REPO + "/releases/download";

  // Static fallback (kept in sync with v1.0.0) used when the API is unreachable.
  const STATIC = {
    tag: "v1.0.0",
    name: "PCL-In v1.0.0",
    publishedAt: "2026-09-06",
    x64: { file: "PCL-In-x64.exe", size: 17.79, sha256: "917e3c9fba8a044e1c50226c62ea299b13d8dfa22dc5e418eb6a90e85e2bc7c1" },
    arm64: { file: "PCL-In-arm64.exe", size: 17.56, sha256: "ba3ea7acb07865670a3bf3cafc94a172abb34107f0a8ebdf641d436f4c54f944" }
  };
  const ARCHS = ["x64", "arm64"];

  function mb(bytes) { return bytes ? (bytes / 1048576).toFixed(2) + " MB" : ""; }
  function cleanSha(digest) { return digest ? digest.replace(/^sha256:/i, "").trim() : ""; }
  function fmtDate(iso) { try { const d = new Date(iso); return isNaN(d) ? "" : d.toISOString().slice(0, 10); } catch (e) { return ""; } }
  function setAttr(id, attr, val) { const el = document.getElementById(id); if (el) el.setAttribute(attr, val); }
  function setText(id, val) { const el = document.getElementById(id); if (el) el.textContent = val; }

  function apply(data) {
    const tag = data.tag || STATIC.tag;
    const base = BASE + "/" + tag + "/";
    setText("hero-version", tag);
    setText("release-name", data.name || STATIC.name);
    setText("hero-updated", data.publishedAt ? fmtDate(data.publishedAt) : STATIC.publishedAt);
    setAttr("release-link", "href", "https://github.com/" + REPO + "/releases/tag/" + tag);

    ARCHS.forEach(function (arch) {
      const s = data[arch] || STATIC[arch];
      if (!s) return;
      const file = s.file;
      if (s.size) setText("size-" + arch, mb(s.size));
      setAttr("dl-" + arch, "href", base + file);
      setAttr("dl-hero-" + arch, "href", base + file);
      setAttr("sig-" + arch, "href", base + file + ".asc");
      const sha = s.sha256 || STATIC[arch].sha256;
      if (sha) {
        setText("code-" + arch, file + "\n" + sha);
        // Re-point the matching copy button.
        var card = document.querySelector(".dl-card[data-arch='" + arch + "']");
        if (card) { var b = card.querySelector("[data-copy]"); if (b) b.dataset.copy = sha; }
      }
    });
  }

  function initCopy() {
    document.querySelectorAll("[data-copy]").forEach(function (btn) {
      btn.addEventListener("click", function () {
        const text = btn.dataset.copy || "";
        function done() { const old = btn.textContent; btn.textContent = "已复制 ✓"; setTimeout(function () { btn.textContent = old; }, 1600); }
        if (navigator.clipboard && navigator.clipboard.writeText) {
          navigator.clipboard.writeText(text).then(done, done);
        } else {
          const ta = document.createElement("textarea");
          ta.value = text; document.body.appendChild(ta); ta.select();
          try { document.execCommand("copy"); } catch (e) {}
          document.body.removeChild(ta); done();
        }
      });
    });
  }

  function load(release) {
    const byName = {};
    (release.assets || []).forEach(function (a) { byName[a.name] = a; });
    const data = { tag: release.tag_name, name: release.name || "PCL-In " + release.tag_name, publishedAt: release.published_at || STATIC.publishedAt };
    ARCHS.forEach(function (arch) {
      const file = STATIC[arch].file;
      const a = byName[file];
      data[arch] = a
        ? { file: file, size: a.size, sha256: cleanSha(a.digest) || STATIC[arch].sha256 }
        : { file: file, size: STATIC[arch].size, sha256: STATIC[arch].sha256 };
    });
    apply(data);
  }

  document.addEventListener("DOMContentLoaded", function () {
    useStatic(); initCopy();
    fetch(API, { headers: { "Accept": "application/vnd.github+json" } })
      .then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); })
      .then(function (json) { if (json && json.tag_name) load(json); else useStatic(); })
      .catch(function () { useStatic(); });
  });
  function useStatic() { apply(STATIC); }
})();