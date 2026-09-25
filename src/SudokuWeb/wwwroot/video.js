// YouTube player in the top-right corner of the Sudoku page.
// The player pastes a YouTube link; we pull out the video id and show it in an embedded player.
(() => {
    "use strict";

    // localStorage keys so the last video and the hidden/shown state survive a page reload.
    const LAST_VIDEO_KEY = "sudoku.lastVideoUrl";
    const COLLAPSED_KEY = "sudoku.videoCollapsed";

    // ---------- References to page elements ----------
    const panel = document.getElementById("video-panel");
    const toggleBtn = document.getElementById("video-toggle");
    const form = document.getElementById("video-form");
    const urlInput = document.getElementById("video-url");
    const frame = document.getElementById("video-frame");
    const errorEl = document.getElementById("video-error");

    // ---------- Safe localStorage helpers (storage can be blocked, e.g. private browsing) ----------
    function load(key) {
        try { return localStorage.getItem(key); } catch { return null; }
    }

    function save(key, value) {
        try { localStorage.setItem(key, value); } catch { /* not saved; that's fine */ }
    }

    /**
     * Pulls the 11-character video id out of the link formats YouTube uses:
     *   https://www.youtube.com/watch?v=ID      https://youtu.be/ID
     *   https://www.youtube.com/shorts/ID       https://www.youtube.com/embed/ID
     *   https://www.youtube.com/live/ID         or just the ID on its own
     * Also returns the start time if the link has one (e.g. "&t=90" or "?t=1m30s").
     * Returns null when the text isn't a YouTube video link.
     */
    function parseYouTubeUrl(text) {
        const ID_PATTERN = /^[A-Za-z0-9_-]{11}$/;
        const trimmed = text.trim();

        // A bare video id.
        if (ID_PATTERN.test(trimmed)) {
            return { id: trimmed, start: 0 };
        }

        let url;
        try {
            // Allow links pasted without "https://".
            url = new URL(/^https?:\/\//i.test(trimmed) ? trimmed : `https://${trimmed}`);
        } catch {
            return null;
        }

        const host = url.hostname.replace(/^(www\.|m\.|music\.)/, "");
        let id = null;

        if (host === "youtu.be") {
            id = url.pathname.slice(1).split("/")[0];
        } else if (host === "youtube.com" || host === "youtube-nocookie.com") {
            if (url.pathname === "/watch") {
                id = url.searchParams.get("v");
            } else {
                // /shorts/ID, /embed/ID, /live/ID, /v/ID
                const match = url.pathname.match(/^\/(?:shorts|embed|live|v)\/([^/?#]+)/);
                id = match ? match[1] : null;
            }
        }

        if (!id || !ID_PATTERN.test(id)) {
            return null;
        }

        return { id, start: parseStartTime(url.searchParams.get("t") || url.searchParams.get("start")) };
    }

    /** Turns "90", "90s" or "1h2m3s" into a number of seconds (0 if missing or invalid). */
    function parseStartTime(value) {
        if (!value) {
            return 0;
        }
        if (/^\d+s?$/.test(value)) {
            return parseInt(value, 10);
        }
        const match = value.match(/^(?:(\d+)h)?(?:(\d+)m)?(?:(\d+)s)?$/);
        if (!match) {
            return 0;
        }
        const [, h = 0, m = 0, s = 0] = match;
        return Number(h) * 3600 + Number(m) * 60 + Number(s);
    }

    /** Replaces the player area with an embedded YouTube player for the given video. */
    function showVideo({ id, start }, autoplay) {
        const params = new URLSearchParams({ rel: "0" });
        if (start > 0) params.set("start", String(start));
        if (autoplay) params.set("autoplay", "1");

        const iframe = document.createElement("iframe");
        iframe.src = `https://www.youtube.com/embed/${id}?${params}`;
        iframe.title = "YouTube video player";
        iframe.allow = "accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture";
        iframe.allowFullscreen = true;
        // YouTube needs to know which site is embedding the video, or it may refuse to play it.
        iframe.referrerPolicy = "strict-origin-when-cross-origin";

        frame.replaceChildren(iframe);
    }

    /** Handles the Play button: check the link, then load the video. */
    function onSubmit(event) {
        event.preventDefault(); // stay on the page instead of submitting the form

        const video = parseYouTubeUrl(urlInput.value);
        if (!video) {
            errorEl.textContent = "That doesn't look like a YouTube video link.";
            return;
        }

        errorEl.textContent = "";
        showVideo(video, true);
        save(LAST_VIDEO_KEY, urlInput.value.trim());
    }

    /** Hides or shows the player. Hiding keeps the video playing (only the panel is folded). */
    function setCollapsed(collapsed) {
        panel.classList.toggle("collapsed", collapsed);
        toggleBtn.textContent = collapsed ? "+" : "–";
        toggleBtn.title = collapsed ? "Show player" : "Hide player";
        toggleBtn.setAttribute("aria-expanded", String(!collapsed));
        save(COLLAPSED_KEY, collapsed ? "1" : "0");
    }

    // ---------- Wire up events and restore the last session ----------
    form.addEventListener("submit", onSubmit);
    toggleBtn.addEventListener("click", () => setCollapsed(!panel.classList.contains("collapsed")));

    setCollapsed(load(COLLAPSED_KEY) === "1");

    // Reload the last video (without autoplaying, so the page doesn't start making noise by itself).
    const lastUrl = load(LAST_VIDEO_KEY);
    const lastVideo = lastUrl && parseYouTubeUrl(lastUrl);
    if (lastVideo) {
        urlInput.value = lastUrl;
        showVideo(lastVideo, false);
    }
})();
