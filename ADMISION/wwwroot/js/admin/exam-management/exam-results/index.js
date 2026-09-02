(function () {
    const initialData = window.ExamResultsData || [];

    $(document).ready(function () {
        DT.load('examResultsTable', { data: initialData });

        function applyFilters() {
            const termId = $('#filterTerm').val() || '';
            const modalityId = $('#filterModality').val() || '';
            const name = ($('#filterName').val() || '').toLowerCase();

            const filtered = initialData.filter(r => {
                if (termId && String(r.termId) !== termId) return false;
                if (modalityId && String(r.modalityId) !== modalityId) return false;
                if (name && !r.name.toLowerCase().includes(name) && !(r.description || '').toLowerCase().includes(name)) return false;
                return true;
            });
            DT.load('examResultsTable', { data: filtered });
        }

        $('#filterTerm').on('change', applyFilters);
        $('#filterModality').on('change', applyFilters);
        $('#filterName').on('input', applyFilters);

        document.addEventListener('dt:action', function (e) {
            const { key, row } = e.detail;
            if (key === 'delete') deleteResult(row.id, row.name);
        });
    });

    async function deleteResult(id, name) {
        const result = await Swal.fire({
            title: '¿Eliminar documento?',
            html: `Se eliminará <strong>"${name}"</strong> y el PDF asociado.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#f43f5e',
            cancelButtonColor: '#8b93a5',
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar',
            reverseButtons: true
        });

        if (result.isConfirmed) {
            const form = document.createElement('form');
            form.method = 'POST';
            form.action = `/admin/exam-management/results/eliminar/${id}`;
            const token = document.querySelector('input[name="__RequestVerificationToken"]');
            if (token) form.appendChild(token.cloneNode(true));
            document.body.appendChild(form);
            form.submit();
        }
    }

    window.deleteResult = deleteResult;
})();
