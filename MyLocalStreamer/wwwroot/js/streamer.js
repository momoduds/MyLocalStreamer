// ES module imported by Home.razor.
// Everything is looked up at event time (no stale element references) and uses
// document-level delegation, so it keeps working however often Blazor re-renders.

const PLAYER_ID = 'player';
const CONTAINER_ID = 'videoContainer';
const TYPING_TAGS = ['INPUT', 'TEXTAREA', 'SELECT', 'BUTTON'];

let dotNet = null;
let handlers = [];
const thumbTimes = new Map(); // videoId -> chosen thumbnail time (seconds), stable while the page lives

const player = () => document.getElementById(PLAYER_ID);

function listen(target, type, fn, options) {
    target.addEventListener(type, fn, options);
    handlers.push(() => target.removeEventListener(type, fn, options));
}

export function init(dotNetRef, watchedAfterSeconds) {
    dispose();
    dotNet = dotNetRef;

    // ---- keyboard shortcuts -------------------------------------------------
    listen(document, 'keydown', e => {
        if (e.ctrlKey || e.metaKey || e.altKey) return;
        const t = e.target;
        if (t instanceof HTMLElement && (t.isContentEditable || TYPING_TAGS.includes(t.tagName))) return;

        const v = player();
        if (!v) return;

        switch (e.code) {
            case 'Space':
                e.preventDefault();
                v.paused ? v.play() : v.pause();
                break;
            case 'ArrowRight':
                e.preventDefault();
                v.currentTime = Math.min(v.duration || Infinity, v.currentTime + 5);
                break;
            case 'ArrowLeft':
                e.preventDefault();
                v.currentTime = Math.max(0, v.currentTime - 5);
                break;
            case 'KeyF':
                e.preventDefault();
                toggleFullscreen();
                break;
            case 'KeyM':
                v.muted = !v.muted;
                break;
            case 'KeyN':
                dotNet?.invokeMethodAsync('OnShortcut', 'next');
                break;
            case 'KeyT':
                dotNet?.invokeMethodAsync('OnShortcut', 'theater');
                break;
            case 'KeyS':
                dotNet?.invokeMethodAsync('OnShortcut', 'screenshot');
                break;
        }
    });

    // ---- "watched" tracking (runs in the browser; the server is told once) ---
    // timeupdate does not bubble, so listen in the capture phase.
    listen(document, 'timeupdate', e => {
        const v = e.target;
        if (!(v instanceof HTMLVideoElement) || v.id !== PLAYER_ID) return;
        if (v.currentTime < watchedAfterSeconds) return;

        const id = v.dataset.videoId;
        if (!id || v.dataset.reported === id) return;
        v.dataset.reported = id;
        dotNet?.invokeMethodAsync('OnWatchThresholdReached', id);
    }, true);

    // ---- thumbnails ---------------------------------------------------------
    // Each thumbnail <video> gets its src only when scrolled near the viewport, then
    // jumps to a random point so the card shows a frame from somewhere in the video.
    const lazyLoader = new IntersectionObserver(entries => {
        for (const entry of entries) {
            if (!entry.isIntersecting) continue;
            const v = entry.target;
            lazyLoader.unobserve(v);
            v.muted = true; // see note on hover below
            if (!v.getAttribute('src')) v.src = v.dataset.src;
        }
    }, { rootMargin: '200px' });

    const observeThumb = v => { if (!v.getAttribute('src')) lazyLoader.observe(v); };
    const THUMB = 'video.thumb-video';

    // Blazor adds/removes cards at will (filtering, "Show more"), so watch the DOM.
    const domWatcher = new MutationObserver(mutations => {
        for (const m of mutations) {
            for (const n of m.addedNodes) {
                if (n.nodeType !== Node.ELEMENT_NODE) continue;
                if (n.matches(THUMB)) observeThumb(n);
                n.querySelectorAll(THUMB).forEach(observeThumb);
            }
        }
    });
    domWatcher.observe(document.body, { childList: true, subtree: true });
    document.querySelectorAll(THUMB).forEach(observeThumb);
    handlers.push(() => { lazyLoader.disconnect(); domWatcher.disconnect(); });

    // loadedmetadata does not bubble, so listen in the capture phase.
    listen(document, 'loadedmetadata', e => {
        const v = e.target;
        if (!(v instanceof HTMLVideoElement) || !v.classList.contains('thumb-video')) return;
        if (!Number.isFinite(v.duration) || v.duration <= 0) return;

        const id = v.dataset.videoId;
        let t = thumbTimes.get(id);
        if (t === undefined || t >= v.duration) {
            t = v.duration * (0.1 + Math.random() * 0.8); // somewhere between 10% and 90%
            thumbTimes.set(id, t);
        }
        v.currentTime = t;
    }, true);

    // ---- hover preview: play the thumbnail in place, then return to its still frame --
    listen(document, 'mouseover', e => {
        const card = e.target instanceof Element ? e.target.closest('.thumbnail-card') : null;
        if (!card || card.contains(e.relatedTarget) || card.classList.contains('active')) return;

        const v = card.querySelector(THUMB);
        if (!v) return;

        // Blazor creates the element first and adds attributes afterwards, and browsers only
        // honour the `muted` attribute at creation time. Without muted = true the browser's
        // autoplay policy silently refuses play() on hover, so set the property explicitly.
        v.muted = true;
        if (!v.getAttribute('src')) v.src = v.dataset.src;
        v.play().catch(err => console.debug('Thumbnail preview was not allowed to play:', err));
    });

    listen(document, 'mouseout', e => {
        const card = e.target instanceof Element ? e.target.closest('.thumbnail-card') : null;
        if (!card || card.contains(e.relatedTarget)) return;

        const v = card.querySelector(THUMB);
        if (!v || v.paused) return;
        v.pause();
        const t = thumbTimes.get(v.dataset.videoId);
        if (t !== undefined) v.currentTime = t;
    });
}

export function dispose() {
    handlers.forEach(off => off());
    handlers = [];
    dotNet = null;
}

export function scrollToPlayer() {
    document.getElementById(CONTAINER_ID)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

export function toggleFullscreen() {
    if (document.fullscreenElement) {
        document.exitFullscreen();
    } else {
        document.getElementById(CONTAINER_ID)?.requestFullscreen?.();
    }
}

/**
 * Grabs the current frame as a PNG and POSTs it straight to the server.
 * (Sending it through the Blazor circuit would exceed SignalR's 32 KB message limit.)
 * Returns the saved file name, or null on failure.
 */
export async function captureScreenshot(videoId) {
    const v = player();
    if (!v || !v.videoWidth) return null;

    const canvas = document.createElement('canvas');
    canvas.width = v.videoWidth;
    canvas.height = v.videoHeight;
    canvas.getContext('2d').drawImage(v, 0, 0, canvas.width, canvas.height);

    const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/png'));
    if (!blob) return null;

    try {
        const res = await fetch(`/api/screenshots/${encodeURIComponent(videoId)}`, {
            method: 'POST',
            headers: { 'Content-Type': 'image/png' },
            body: blob
        });
        return res.ok ? (await res.json()).fileName : null;
    } catch {
        return null;
    }
}

export function getTheme() {
    return document.documentElement.dataset.theme || 'dark';
}

export function setTheme(theme) {
    document.documentElement.dataset.theme = theme;
    try { localStorage.setItem('themePreference', theme); } catch { /* ignore */ }
}