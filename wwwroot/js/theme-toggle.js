document.addEventListener('DOMContentLoaded', function () {
    const btn = document.getElementById('themeToggle');
    const icon = document.getElementById('themeIcon');
    const html = document.documentElement;
    const stored = localStorage.getItem('theme') || 'light';
    html.setAttribute('data-bs-theme', stored);
    if (icon) icon.className = stored === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
    if (btn) {
        btn.addEventListener('click', function () {
            const current = html.getAttribute('data-bs-theme');
            const next = current === 'dark' ? 'light' : 'dark';
            html.setAttribute('data-bs-theme', next);
            localStorage.setItem('theme', next);
            if (icon) icon.className = next === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars';
        });
    }
});
