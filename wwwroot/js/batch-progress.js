// SignalR batch progress client
let batchConnection = null;

async function startBatchProgress(jobId) {
    if (batchConnection) {
        await batchConnection.stop();
    }
    batchConnection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/batch-progress')
        .withAutomaticReconnect()
        .build();

    batchConnection.on('progressUpdate', function (update) {
        updateBatchUI(update);
    });

    await batchConnection.start();
    await batchConnection.invoke('JoinJob', jobId);
}

function updateBatchUI(update) {
    const progressBar = document.getElementById('batchProgressBar');
    const progressText = document.getElementById('batchProgressText');
    const currentItem = document.getElementById('batchCurrentItem');
    const resultsList = document.getElementById('batchResultsList');
    const progressWrap = document.querySelector('.batch-progress-wrap');

    if (progressWrap) progressWrap.classList.add('visible');

    const pct = Math.round(update.progressPct || 0);
    if (progressBar) {
        progressBar.style.width = pct + '%';
        progressBar.setAttribute('aria-valuenow', pct);
        progressBar.textContent = pct + '%';
        progressBar.className = 'progress-bar progress-bar-striped progress-bar-animated' +
            (update.status === 'completed' ? ' bg-success' : ' bg-primary');
    }
    if (progressText) {
        progressText.textContent = `${update.processed} of ${update.totalItems} files processed · ${update.failed} failed`;
    }
    if (currentItem && update.currentItem) {
        currentItem.textContent = update.status === 'completed' ? 'Completed!' : 'Processing: ' + update.currentItem;
    }
    if (resultsList && update.results) {
        resultsList.innerHTML = '';
        update.results.forEach(r => {
            const li = document.createElement('li');
            li.className = 'list-group-item d-flex justify-content-between align-items-center py-2';
            li.innerHTML = r.success
                ? `<span class="text-success"><i class="bi bi-check-circle-fill me-1"></i>${r.fileName}</span>
                   <a href="${r.downloadUrl}" class="btn btn-sm btn-outline-success py-0 px-2" download>
                     <i class="bi bi-download"></i>
                   </a>`
                : `<span class="text-danger"><i class="bi bi-x-circle-fill me-1"></i>${r.fileName}</span>
                   <small class="text-muted">${r.error || 'Failed'}</small>`;
            resultsList.appendChild(li);
        });
    }
}
