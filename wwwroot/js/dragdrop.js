function initDragDrop(zoneId, inputId, accept) {
    const zone = document.getElementById(zoneId);
    const input = document.getElementById(inputId);
    if (!zone || !input) return;
    zone.addEventListener('click', () => input.click());
    zone.addEventListener('dragover', e => { e.preventDefault(); zone.classList.add('dragover'); });
    zone.addEventListener('dragleave', () => zone.classList.remove('dragover'));
    zone.addEventListener('drop', e => {
        e.preventDefault();
        zone.classList.remove('dragover');
        const dt = new DataTransfer();
        Array.from(e.dataTransfer.files).forEach(f => {
            if (!accept || accept.split(',').some(ext => f.name.toLowerCase().endsWith(ext.trim()))) dt.items.add(f);
        });
        input.files = dt.files;
        updateZoneLabel(zone, dt.files);
        input.dispatchEvent(new Event('change'));
    });
    input.addEventListener('change', () => updateZoneLabel(zone, input.files));
}

function updateZoneLabel(zone, files) {
    const lbl = zone.querySelector('p');
    if (!lbl) return;
    if (files.length === 0) { lbl.textContent = 'Drag & drop or click to select files'; return; }
    lbl.textContent = files.length === 1
        ? files[0].name + ' (' + formatBytes(files[0].size) + ')'
        : files.length + ' files selected';
}

function formatBytes(bytes) {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / 1024 / 1024).toFixed(1) + ' MB';
}
