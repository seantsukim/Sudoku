// YouTube player in the top-right corner of the Sudoku page.
// As the player types in the search bar, we ask the server for matching videos
// (GET /api/videos/search — the server's sorted word index + binary search does the
// matching) and show them in a dropdown. Picking one plays it in the embedded player.
(() => {
    "use strict";

    // localStorage keys so the last video and the hidden/shown state survive a page reload.
    const LAST_VIDEO_KEY = "sudoku.lastVideo";
    const COLLAPSED_KEY = "sudoku.videoCollapsed";

    // Wait this long after the last keystroke before searching, so fast typing
    // sends one request instead of one per letter. Short enough to feel instant.
    const SEARCH_DELAY_MS = 120;
    const MAX_RESULTS = 8;

    // ---------- References to page elements ----------
    const panel = document.getElementById("video-panel");
    const toggleBtn = document.getElementById("video-toggle");
    const searchInput = document.getElementById("video-search");
    const resultsList = document.getElementById("video-results");
    const frame = document.getElementById("video-frame");
    const statusEl = document.getElementById("video-status");

    // ---------- Search state ----------
    let results = [];           // videos currently shown in the dropdown
    let activeIndex = -1;       // which result the arrow keys have highlighted (-1 = none)
    let searchTimer = null;     // pending delayed search
    let pendingRequest = null;  // AbortController for the request in flight

    // ---------- Safe localStorage helpers (storage can be blocked, e.g. private browsing) ----------
    function load(key) {
        try { return localStorage.getItem(key); } catch { return null; }
    }

    function save(key, value) {
        try { localStorage.setItem(key, value); } catch { /* not saved; that's fine */ }
    }

    function setStatus(text, isError = false) {
        statusEl.textContent = text;
        statusEl.classList.toggle("error", isError);
    }

    // ---------- Player ----------

    /** Replaces the player area with an embedded YouTube player for the given video. */
    function showVideo(video, autoplay) {
        const params = new URLSearchParams({ rel: "0" });
        if (autoplay) params.set("autoplay", "1");

        const iframe = document.createElement("iframe");
        iframe.src = `https://www.youtube.com/embed/${encodeURIComponent(video.id)}?${params}`;
        iframe.title = `YouTube video player: ${video.title}`;
        iframe.allow = "accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture";
        iframe.allowFullscreen = true;
        // YouTube needs to know which site is embedding the video, or it may refuse to play it.
        iframe.referrerPolicy = "strict-origin-when-cross-origin";

        frame.replaceChildren(iframe);
        setStatus(`Now playing: ${video.title} — ${video.channel}`);
    }

    /** Plays the chosen search result and remembers it for next time. */
    function chooseVideo(video) {
        searchInput.value = video.title;
        closeResults();
        showVideo(video, true);
        save(LAST_VIDEO_KEY, JSON.stringify(video));
    }

    // ---------- Searching ----------

    /** Asks the server for videos matching the text typed so far. */
    async function runSearch(query) {
        // Cancel the previous request so an older, slower answer can't replace a newer one.
        pendingRequest?.abort();
        pendingRequest = new AbortController();

        try {
            const url = `/api/videos/search?q=${encodeURIComponent(query)}&limit=${MAX_RESULTS}`;
            const response = await fetch(url, { signal: pendingRequest.signal });
            if (!response.ok) {
                throw new Error(`Server returned ${response.status}`);
            }
            renderResults(await response.json());
        } catch (err) {
            if (err.name !== "AbortError") {
                setStatus(`Search failed: ${err.message}`, true);
            }
        }
    }

    /** Called on every keystroke: waits briefly, then searches (or clears for empty input). */
    function onInput() {
        clearTimeout(searchTimer);
        const query = searchInput.value.trim();

        if (query === "") {
            pendingRequest?.abort();
            closeResults();
            return;
        }

        searchTimer = setTimeout(() => runSearch(query), SEARCH_DELAY_MS);
    }

    // ---------- Results dropdown ----------

    /** Draws the dropdown list: thumbnail, title and channel for each match. */
    function renderResults(videos) {
        results = videos;
        activeIndex = -1;
        resultsList.replaceChildren();

        if (videos.length === 0) {
            const empty = document.createElement("li");
            empty.className = "video-no-results";
            empty.textContent = "No videos match your search.";
            resultsList.appendChild(empty);
        }

        videos.forEach((video, i) => {
            const item = document.createElement("li");
            item.className = "video-result";
            item.id = `video-result-${i}`;
            item.setAttribute("role", "option");
            item.setAttribute("aria-selected", "false");

            // Small preview image from YouTube's thumbnail server (hidden if it can't load).
            const thumb = document.createElement("img");
            thumb.src = `https://i.ytimg.com/vi/${encodeURIComponent(video.id)}/mqdefault.jpg`;
            thumb.alt = "";
            thumb.loading = "lazy";
            thumb.addEventListener("error", () => { thumb.style.visibility = "hidden"; });

            const text = document.createElement("span");
            text.className = "video-result-text";
            const title = document.createElement("span");
            title.className = "video-result-title";
            title.textContent = video.title;
            const channel = document.createElement("span");
            channel.className = "video-result-channel";
            channel.textContent = video.channel;
            text.append(title, channel);

            item.append(thumb, text);

            // mousedown (not click) so the choice happens before the input loses focus.
            item.addEventListener("mousedown", event => {
                event.preventDefault();
                chooseVideo(video);
            });
            item.addEventListener("mousemove", () => setActive(i));

            resultsList.appendChild(item);
        });

        resultsList.hidden = false;
        searchInput.setAttribute("aria-expanded", "true");
    }

    function closeResults() {
        resultsList.hidden = true;
        searchInput.setAttribute("aria-expanded", "false");
        searchInput.removeAttribute("aria-activedescendant");
        activeIndex = -1;
    }

    /** Highlights one result (used by the arrow keys and mouse hover). */
    function setActive(index) {
        const items = resultsList.querySelectorAll(".video-result");
        items.forEach((item, i) => {
            item.classList.toggle("active", i === index);
            item.setAttribute("aria-selected", String(i === index));
        });

        activeIndex = index;
        if (index >= 0 && items[index]) {
            items[index].scrollIntoView({ block: "nearest" });
            searchInput.setAttribute("aria-activedescendant", items[index].id);
        } else {
            searchInput.removeAttribute("aria-activedescendant");
        }
    }

    /** Keyboard control: Up/Down to move, Enter to play, Escape to close the list. */
    function onKeyDown(event) {
        const open = !resultsList.hidden && results.length > 0;

        switch (event.key) {
            case "ArrowDown":
                if (open) {
                    event.preventDefault();
                    setActive((activeIndex + 1) % results.length);
                }
                break;
            case "ArrowUp":
                if (open) {
                    event.preventDefault();
                    setActive(activeIndex <= 0 ? results.length - 1 : activeIndex - 1);
                }
                break;
            case "Enter":
                if (open) {
                    event.preventDefault();
                    // Enter with nothing highlighted plays the top (best) match.
                    chooseVideo(results[Math.max(activeIndex, 0)]);
                }
                break;
            case "Escape":
                closeResults();
                break;
        }
    }

    // ---------- Hide / show the panel ----------

    /** Hides or shows the player. Hiding keeps the video playing (only the panel is folded). */
    function setCollapsed(collapsed) {
        panel.classList.toggle("collapsed", collapsed);
        toggleBtn.textContent = collapsed ? "+" : "–";
        toggleBtn.title = collapsed ? "Show player" : "Hide player";
        toggleBtn.setAttribute("aria-expanded", String(!collapsed));
        save(COLLAPSED_KEY, collapsed ? "1" : "0");
    }

    // ---------- Wire up events and restore the last session ----------
    searchInput.addEventListener("input", onInput);
    searchInput.addEventListener("keydown", onKeyDown);
    searchInput.addEventListener("blur", closeResults);
    // Re-open the last results when the search bar is focused again.
    searchInput.addEventListener("focus", () => { if (searchInput.value.trim()) onInput(); });
    toggleBtn.addEventListener("click", () => setCollapsed(!panel.classList.contains("collapsed")));

    setCollapsed(load(COLLAPSED_KEY) === "1");

    // Reload the last video (without autoplaying, so the page doesn't start making noise by itself).
    try {
        const last = JSON.parse(load(LAST_VIDEO_KEY) || "null");
        if (last && typeof last.id === "string" && typeof last.title === "string") {
            searchInput.value = last.title;
            showVideo(last, false);
        }
    } catch {
        /* ignore a corrupted saved value */
    }
})();
