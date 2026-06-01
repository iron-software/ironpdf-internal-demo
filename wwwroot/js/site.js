document.addEventListener('DOMContentLoaded', function () {
    // Mobile sidebar toggle
    const toggle = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebarOverlay');
    if (toggle && sidebar && overlay) {
        toggle.addEventListener('click', () => {
            sidebar.classList.toggle('show');
            overlay.classList.toggle('show');
        });
        overlay.addEventListener('click', () => {
            sidebar.classList.remove('show');
            overlay.classList.remove('show');
        });
    }

    // ── Navigation progress bar ──────────────────────────────────────────
    // Smooths the perceived gap between clicking a sidebar link and the
    // next page rendering. Works alongside the CSS View Transitions API
    // (which itself only fires on supporting browsers).
    const progress = document.getElementById('navProgress');
    let progressTimer = null;
    function startProgress() {
        if (!progress) return;
        clearTimeout(progressTimer);
        progress.classList.remove('done');
        progress.classList.add('active');
        progress.style.width = '0%';
        // Force reflow so the 0 -> target transition animates
        void progress.offsetWidth;
        progress.style.width = '70%';
        // Creep slowly toward 90% while waiting for the next document
        progressTimer = setTimeout(() => { progress.style.width = '90%'; }, 400);
    }
    function finishProgress() {
        if (!progress) return;
        clearTimeout(progressTimer);
        progress.classList.add('done');
        progress.classList.remove('active');
        setTimeout(() => { progress.style.width = '0%'; progress.classList.remove('done'); }, 400);
    }

    // ── SPA-style sidebar navigation ─────────────────────────────────────
    // The Razor pages share a layout, so a real browser navigation tears
    // down the DOM (blank flash) just to redraw the same sidebar/topbar.
    // We intercept clicks on the known navigable routes, fetch the next
    // page, and swap only the .page-body. document.startViewTransition()
    // does the cross-fade where available; the swap is instant otherwise.
    const NAV_PATHS = new Set([
        '/', '/index',
        '/conversion', '/templates',
        '/manipulation', '/annotations', '/metadata',
        '/security', '/forms',
        '/batch'
    ]);
    const LAYOUT_SCRIPT_HINTS = ['bootstrap.bundle', 'toast.js', 'theme-toggle.js', 'dragdrop.js', 'site.js'];

    function normalizePath(p) {
        if (!p) return '/';
        const trimmed = p.replace(/\/+$/, '').toLowerCase();
        return trimmed || '/';
    }
    function isNavigable(href) {
        try {
            const u = new URL(href, window.location.href);
            if (u.origin !== window.location.origin) return false;
            return NAV_PATHS.has(normalizePath(u.pathname));
        } catch { return false; }
    }

    let inFlight = null;
    async function spaNavigate(url, { push = true } = {}) {
        // Cancel any prior in-flight request so rapid clicks land on the latest target
        if (inFlight) inFlight.abort();
        const ctrl = new AbortController();
        inFlight = ctrl;
        startProgress();

        try {
            const res = await fetch(url, { signal: ctrl.signal, credentials: 'same-origin', headers: { 'X-SPA-Nav': '1' } });
            if (!res.ok) { window.location.href = url; return; }
            const html = await res.text();
            const doc = new DOMParser().parseFromString(html, 'text/html');
            const incomingBody  = doc.querySelector('.page-body');
            const incomingTitle = doc.querySelector('title')?.textContent;
            const incomingPageTitle = doc.querySelector('.page-title')?.textContent;
            if (!incomingBody) { window.location.href = url; return; }

            const targetPath = normalizePath(new URL(url, window.location.origin).pathname);

            const apply = () => {
                // Swap content
                const currentBody = document.querySelector('.page-body');
                if (!currentBody) return;
                currentBody.replaceWith(incomingBody);

                // Title + topbar
                if (incomingTitle) document.title = incomingTitle;
                const topbarTitle = document.querySelector('.page-title');
                if (topbarTitle && incomingPageTitle) topbarTitle.textContent = incomingPageTitle;

                // Sidebar active state
                document.querySelectorAll('.sidebar .nav-item').forEach(a => {
                    try {
                        const p = normalizePath(new URL(a.href, window.location.origin).pathname);
                        a.classList.toggle('active', p === targetPath);
                    } catch { /* ignore */ }
                });

                // Re-execute scripts inside the new page-body
                incomingBody.querySelectorAll('script').forEach(oldS => {
                    const s = document.createElement('script');
                    for (const attr of oldS.attributes) s.setAttribute(attr.name, attr.value);
                    s.text = oldS.textContent;
                    oldS.replaceWith(s);
                });
                // Re-execute the page's @section Scripts block (rendered after layout scripts in body)
                doc.querySelectorAll('script').forEach(oldS => {
                    if (oldS.closest('.page-body')) return;                     // already handled above
                    const src = oldS.getAttribute('src') || '';
                    if (src && LAYOUT_SCRIPT_HINTS.some(h => src.includes(h))) return; // skip shared layout scripts
                    const s = document.createElement('script');
                    for (const attr of oldS.attributes) s.setAttribute(attr.name, attr.value);
                    if (!src) s.text = oldS.textContent;
                    document.body.appendChild(s);
                });

                // Re-trigger the .page-body entrance animation only when the
                // browser can't do a view transition (otherwise the keyframe
                // would still be in flight when the cross-fade finishes).
                if (!document.startViewTransition) {
                    const newBody = document.querySelector('.page-body');
                    if (newBody) {
                        newBody.style.animation = 'none';
                        void newBody.offsetWidth;
                        newBody.style.animation = '';
                    }
                }

                // Scroll to top, no smooth so it doesn't fight the fade
                window.scrollTo(0, 0);

                // Close mobile sidebar if open
                sidebar?.classList.remove('show');
                overlay?.classList.remove('show');
            };

            if (push) history.pushState({ spa: true, url }, '', url);

            if (document.startViewTransition) {
                await document.startViewTransition(apply).finished.catch(() => {});
            } else {
                apply();
            }
        } catch (e) {
            if (e.name !== 'AbortError') {
                // Hard fallback: let the browser navigate normally
                window.location.href = url;
            }
        } finally {
            finishProgress();
            if (inFlight === ctrl) inFlight = null;
        }
    }

    // Single click handler: SPA-swap navigable links, kick the progress
    // bar for non-navigable same-origin links (e.g. /swagger if not _blank).
    document.addEventListener('click', (e) => {
        const a = e.target.closest('a');
        if (!a) return;
        const href = a.getAttribute('href');
        if (!href || href.startsWith('#') || href.startsWith('javascript:')) return;
        if (a.target && a.target !== '_self') return;
        if (e.metaKey || e.ctrlKey || e.shiftKey || e.altKey || e.button === 1) return;
        let url;
        try { url = new URL(a.href, window.location.href); }
        catch { return; }
        if (url.origin !== window.location.origin) return;
        const samePage = url.pathname === window.location.pathname && url.search === window.location.search;
        if (samePage) return;

        if (isNavigable(url.href)) {
            e.preventDefault();
            spaNavigate(url.href);
        } else {
            startProgress();
        }
    }, true);

    // Back/forward
    window.addEventListener('popstate', (e) => {
        const url = window.location.href;
        if (isNavigable(url)) {
            spaNavigate(url, { push: false });
        } else {
            startProgress();
        }
    });

    // Safety net: any classic navigation still rendering will trigger pageshow
    window.addEventListener('pageshow', finishProgress);

    // Generic form submit helper for API calls
    window.submitApiForm = async function (formId, endpoint, method = 'POST', isJson = false) {
        const form = document.getElementById(formId);
        if (!form) return;
        const btn = form.querySelector('[type=submit]');
        if (btn) { btn.disabled = true; btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Processing...'; }
        try {
            let body, headers = {};
            if (isJson) {
                body = JSON.stringify(Object.fromEntries(new FormData(form)));
                headers['Content-Type'] = 'application/json';
            } else {
                body = new FormData(form);
            }
            const res = await fetch(endpoint, { method, body, headers });
            const data = await res.json();
            if (!res.ok) {
                showToast(data.error || 'Operation failed.', 'error');
            } else {
                return data;
            }
        } catch (e) {
            showToast('Network error: ' + e.message, 'error');
        } finally {
            if (btn) { btn.disabled = false; btn.innerHTML = btn.getAttribute('data-original-text') || 'Submit'; }
        }
        return null;
    };

    // Save original button text
    document.querySelectorAll('form [type=submit]').forEach(btn => {
        btn.setAttribute('data-original-text', btn.innerHTML);
    });
});

function renderResult(data, containerId) {
    if (!data) return;
    const container = document.getElementById(containerId);
    if (!container) return;
    container.innerHTML = `
        <div class="result-card">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="filename"><i class="bi bi-file-pdf-fill me-1" style="color:#DC2626;"></i>${data.fileName || 'result.pdf'}</div>
                    <div class="meta">
                        ${data.fileSizeBytes ? formatBytes(data.fileSizeBytes) + ' &nbsp;·&nbsp; ' : ''}
                        ${data.pageCount ? data.pageCount + ' pages &nbsp;·&nbsp; ' : ''}
                        ${data.elapsedMs ? data.elapsedMs + 'ms' : ''}
                    </div>
                </div>
                <a href="${data.downloadUrl}" class="btn btn-success btn-sm" download="${data.fileName}">
                    <i class="bi bi-download me-1"></i>Download
                </a>
            </div>
        </div>`;
    container.style.display = 'block';
    showToast('PDF generated successfully!', 'success');
}

function formatBytes(bytes) {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024*1024) return (bytes/1024).toFixed(1) + ' KB';
    return (bytes/1024/1024).toFixed(2) + ' MB';
}
