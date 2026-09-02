(function () {
    $(document).ready(function () {
        const cfg = window.ScoringProfilesIndexConfig || {};
        const defaultTermId = cfg.defaultTermId || '';
        const termsJson = cfg.terms || [];

        setTimeout(() => {
            if (defaultTermId && window.customSelectRegistry['TermId']) {
                const term = termsJson.find(t => t.id === defaultTermId);
                if (term) {
                    window.customSelectRegistry['TermId'].setValue(term.id, term.name);
                    loadModalities(term.id);
                }
            }
        }, 200);

        $('#TermId').change(function () {
            const termId = $(this).val();
            loadModalities(termId);
            applyFilters();
        });

        $('#ModalityId').change(function () {
            const modalityId = $(this).val();
            loadTypes(modalityId);
            applyFilters();
        });

        $('#TypeModalityId').change(function () {
            applyFilters();
        });

        $('#filterWeighted, #filterActive').change(applyFilters);

        let searchTimeout;
        $('#filterSearch').on('input', function () {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(applyFilters, 300);
        });

        function loadModalities(termId) {
            if (window.customSelectRegistry['ModalityId']) {
                const modSelect = window.customSelectRegistry['ModalityId'];
                modSelect.clear();
                if (termId) modSelect.load('/admin/exam-management/modalities/get-by-term/' + termId);
            }
            if (window.customSelectRegistry['TypeModalityId']) {
                window.customSelectRegistry['TypeModalityId'].clear();
            }
        }

        function loadTypes(modalityId) {
            if (window.customSelectRegistry['TypeModalityId']) {
                const typeSelect = window.customSelectRegistry['TypeModalityId'];
                typeSelect.clear();
                if (modalityId) typeSelect.load('/admin/exam-management/scoring-profiles/api/types/' + modalityId);
            }
        }

        function applyFilters() {
            const params = {
                termId: $('#TermId').val(),
                modalityId: $('#ModalityId').val(),
                typeModalityId: $('#TypeModalityId').val(),
                isWeighted: $('#filterWeighted').val(),
                isActive: $('#filterActive').val(),
                search: $('#filterSearch').val()
            };
            DT.filter('scoringProfilesTable', params);
        }

        document.addEventListener('dt:action', function (e) {
            const { key, row } = e.detail;
            if (key === 'delete') {
                Swal.fire({
                    title: '¿Eliminar perfil?',
                    text: 'Esta acción eliminará el perfil de calificación de forma permanente.',
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
                        form.action = '/admin/exam-management/scoring-profiles/eliminar/' + row.id;

                        const token = document.querySelector('input[name="__RequestVerificationToken"]');
                        if (token) form.appendChild(token.cloneNode(true));

                        document.body.appendChild(form);
                        form.submit();
                    }
                });
            }
        });
    });
})();
