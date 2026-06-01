function showToast(message, type = 'success') {
    const container = document.getElementById('toastContainer');
    if (!container) return;
    const colors = { success: '#059669', error: '#DC2626', info: '#2563EB', warning: '#D97706' };
    const icons = { success: 'bi-check-circle-fill', error: 'bi-x-circle-fill', info: 'bi-info-circle-fill', warning: 'bi-exclamation-triangle-fill' };
    const id = 'toast_' + Date.now();
    const toastHtml = `
    <div id="${id}" class="toast align-items-center border-0 show" role="alert" style="background:${colors[type] || colors.info}; color:#fff; min-width:280px; max-width:420px;">
      <div class="d-flex">
        <div class="toast-body d-flex align-items-center gap-2">
          <i class="bi ${icons[type] || icons.info}" style="font-size:16px;flex-shrink:0;"></i>
          <span>${message}</span>
        </div>
        <button type="button" class="btn-close btn-close-white me-2 m-auto" onclick="this.closest('.toast').remove()"></button>
      </div>
    </div>`;
    container.insertAdjacentHTML('beforeend', toastHtml);
    setTimeout(() => { const el = document.getElementById(id); if (el) el.remove(); }, 4500);
}
