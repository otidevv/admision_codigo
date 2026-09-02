function toLocalDate(dateStr) {
    if (!dateStr) return null;
    const parts = dateStr.split('-');
    if (parts.length !== 3) return dateStr;
    const d = new Date(parseInt(parts[0]), parseInt(parts[1]) - 1, parseInt(parts[2]));
    return d.toLocaleDateString('es-PE', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function toTime(timeStr) {
    if (!timeStr) return null;
    return String(timeStr).slice(0, 5);
}

function renderDates(val, col, row) {
    const start = toLocalDate(row.startDate) || '—';
    const end = toLocalDate(row.endDate) || '—';
    const startTime = toTime(row.startTime);
    const endTime = toTime(row.endTime);
    return `
        <div class="text-[11px] leading-tight space-y-0.5">
            <div class="flex items-center gap-1.5"><span class="text-ink-400 font-bold uppercase tracking-tighter">Inicia:</span> <span class="text-ink-900 font-bold">${start}</span>${startTime ? `<span class="text-ink-500 font-semibold">${startTime}</span>` : ''}</div>
            <div class="flex items-center gap-1.5"><span class="text-ink-400 font-bold uppercase tracking-tighter">Cierra:</span> <span class="text-ink-900 font-bold">${end}</span>${endTime ? `<span class="text-ink-500 font-semibold">${endTime}</span>` : ''}</div>
        </div>
    `;
}

$(document).ready(function () {
    const cfg = window.ModalitiesIndexConfig || {};
    const defaultTermId = cfg.defaultTermId || '';
    const termsJson = cfg.terms || [];

    if (defaultTermId) {
        const term = termsJson.find(t => t.id === defaultTermId);
        if (term) {
            setTimeout(() => {
                if (window.customSelectRegistry['filterTerm']) {
                    window.customSelectRegistry['filterTerm'].setValue(term.id, term.name);
                    refreshData();
                }
            }, 150);
        }
    }

    let searchTimeout;
    $('#filterSearch').on('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(refreshData, 300);
    });

    $('#filterTerm').change(refreshData);

    document.addEventListener('dt:action', function (e) {
        const { key, row } = e.detail;

        if (key === 'delete') {
            Swal.fire({
                title: '¿Eliminar modalidad?',
                text: 'Esta acción eliminará la modalidad de forma permanente.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#f43f5e',
                cancelButtonColor: '#8b93a5',
                confirmButtonText: 'Sí, eliminar',
                cancelButtonText: 'Cancelar',
                reverseButtons: true
            }).then((result) => {
                if (result.isConfirmed) {
                    const form = document.createElement('form');
                    form.method = 'POST';
                    form.action = '/admin/exam-management/modalities/eliminar/' + row.id;

                    const token = document.querySelector('input[name="__RequestVerificationToken"]');
                    if (token) form.appendChild(token.cloneNode(true));

                    document.body.appendChild(form);
                    form.submit();
                }
            });
        }
    });
});

function refreshData() {
    const params = {
        termId: $('#filterTerm').val() || null,
        search: $('#filterSearch').val() || null
    };
    DT.filter('modalitiesTable', params);
}

window.renderDates = renderDates;
window.refreshData = refreshData;
