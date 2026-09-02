(function () {
    $(() => {
        $('#searchInput').on('keypress', function (e) {
            if (e.which === 13) searchHistory();
        });
        $('#btnSearch').click(searchHistory);
    });

    function searchHistory() {
        const code = $('#searchInput').val().trim();
        if (!code) return;

        const btn = $('#btnSearch');
        btn.prop('disabled', true).html('<i class="ti ti-loader-2 fa-spin"></i> Buscando...');
        $('#emptyState').hide();
        $('#resultsSection').hide();

        $.get('/admin/info-postulant/attendance/history/search?code=' + encodeURIComponent(code))
            .done((res) => {
                if (res.success) {
                    renderResults(code, res.items);
                }
            })
            .fail((err) => {
                const msg = err.responseJSON?.message || 'Error al consultar el historial.';
                Swal.fire({
                    title: 'Error',
                    text: msg,
                    icon: 'error',
                    confirmButtonColor: '#f43f5e',
                });
                $('#emptyState').show();
            })
            .always(() => {
                btn.prop('disabled', false).html('<i class="ti ti-search text-xs"></i> Buscar');
                $('#searchInput').focus().select();
            });
    }

    function renderResults(code, items) {
        const tbody = $('#historyBody');
        tbody.empty();

        $('#resultCode').text(code);
        $('#resultCount').text(items.length);

        if (items.length === 0) {
            tbody.html(`
                <tr>
                    <td colspan="11" class="text-center py-10">
                        <div class="flex flex-col items-center text-ink-400">
                            <i class="ti ti-fingerprint-off text-3xl mb-3"></i>
                            <p class="text-xs font-bold uppercase tracking-[0.16em]">No se encontraron registros de asistencia</p>
                            <p class="text-[11px] mt-1">Este postulante no tiene asistencias registradas para ningún periodo.</p>
                        </div>
                    </td>
                </tr>
            `);
            $('#noRecordsBadge').removeClass('hidden');
            $('#resultName').text('');
            $('#resultsSection').show();
            return;
        }

        $('#noRecordsBadge').addClass('hidden');

        const firstItem = items[0];
        $('#resultName').text(firstItem.fullName);

        items.forEach((item, index) => {
            const dt = new Date(item.verifiedAt);
            const dateStr = dt.toLocaleDateString('es-PE', {
                day: '2-digit', month: '2-digit', year: 'numeric'
            });
            const timeStr = dt.toLocaleTimeString('es-PE', {
                hour: '2-digit', minute: '2-digit', second: '2-digit'
            });

            let statusBadge = '';
            const status = item.biometricStatus || '';
            if (status === 'Verificado') {
                statusBadge = '<span class="badge b-emerald"><i class="ti ti-shield-check"></i> Verificado</span>';
            } else if (status === 'Manual') {
                statusBadge = '<span class="badge b-amber"><i class="ti ti-edit"></i> Manual</span>';
            } else if (status === 'Fallido') {
                statusBadge = '<span class="badge b-red"><i class="ti ti-shield-off"></i> Fallido</span>';
            } else {
                statusBadge = '<span class="badge b-ink">' + status + '</span>';
            }

            const scoreHtml = item.biometricScore != null
                ? '<span class="text-[11px] font-mono text-ink-500">Score: ' + item.biometricScore + '</span>'
                : '';

            const notesHtml = item.notes
                ? '<span class="text-[11px] text-ink-500 truncate block max-w-[150px]" title="' + $('<span>').text(item.notes).html() + '">' + $('<span>').text(item.notes).html() + '</span>'
                : '<span class="text-ink-300">—</span>';

            const tr = `
                <tr class="hover:bg-ink-50/60 dark:hover:bg-ink-800/40 transition-colors">
                    <td class="text-ink-400 font-mono text-[11px]">${index + 1}</td>
                    <td><span class="font-mono text-xs font-semibold">${$('<span>').text(item.codePostulant).html()}</span></td>
                    <td class="font-medium text-ink-900 dark:text-ink-100">${$('<span>').text(item.fullName).html()}</td>
                    <td class="font-mono text-xs">${$('<span>').text(item.document).html()}</td>
                    <td class="text-[12px]">${$('<span>').text(item.careerName).html()}</td>
                    <td><span class="badge b-violet">${$('<span>').text(item.modalityName).html()}</span></td>
                    <td class="font-mono text-xs">${$('<span>').text(item.termName).html()}</td>
                    <td>${statusBadge} ${scoreHtml}</td>
                    <td class="whitespace-nowrap">
                        <span class="font-mono text-[11px]">${dateStr}</span>
                        <span class="text-ink-400 text-[11px]">${timeStr}</span>
                    </td>
                    <td class="text-[12px]">${$('<span>').text(item.verifiedBy).html()}</td>
                    <td>${notesHtml}</td>
                </tr>
            `;
            tbody.append(tr);
        });

        $('#resultsSection').show();
    }

    window.searchHistory = searchHistory;
})();
