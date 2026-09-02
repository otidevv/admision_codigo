(function () {
    $(document).ready(function () {
        const cfg = window.TypeModalitiesIndexConfig || {};
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

        $('#filterTerm').change(function () {
            const termId = $(this).val();
            if (window.customSelectRegistry['filterModality']) {
                const modSelect = window.customSelectRegistry['filterModality'];
                modSelect.clear();
                if (termId) {
                    modSelect.load('/admin/exam-management/modalities/get-by-term/' + termId);
                }
            }
            refreshData();
        });

        let searchTimeout;
        $('#filterSearch').on('input', function () {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(refreshData, 300);
        });

        $('#filterModality').change(refreshData);

        document.addEventListener('dt:action', function (e) {
            const { key, row } = e.detail;
            if (key === 'delete') {
                Swal.fire({
                    title: '¿Eliminar tipo de modalidad?',
                    text: `Estás a punto de eliminar "${row.name}". Esta acción no se puede deshacer.`,
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
                        form.action = '/admin/exam-management/modality-types/eliminar/' + row.id;

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
            modalityId: $('#filterModality').val() || null,
            search: $('#filterSearch').val() || null
        };
        DT.filter('typeModalitiesTable', params);
    }

    window.refreshData = refreshData;
})();
