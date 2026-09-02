(function () {
    const initialData = window.TermsData || [];

    document.addEventListener('DOMContentLoaded', function () {
        DT.load('termsTable', { data: initialData });

        const filterInput = document.getElementById('filterTerm');
        filterInput?.addEventListener('change', function () {
            const val = filterInput.value;
            const filtered = val ? initialData.filter(t => t.id === val) : initialData;
            DT.load('termsTable', { data: filtered });
        });

        document.getElementById('clearFilter')?.addEventListener('click', function () {
            window.customSelectRegistry?.['filterTerm']?.clear();
            DT.load('termsTable', { data: initialData });
        });

        document.addEventListener('dt:action', function (e) {
            const { key, row } = e.detail;
            if (key === 'delete') deleteTerm(row.id, row.name);
        });
    });

    async function deleteTerm(id, name) {
        const result = await Swal.fire({
            title: '¿Eliminar periodo?',
            html: `Estás a punto de eliminar el periodo <strong>"${name}"</strong>.<br/><span class="text-xs text-ink-500">Esta acción no se puede deshacer y podría afectar datos relacionados.</span>`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#f43f5e',
            cancelButtonColor: '#8b93a5',
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar',
            reverseButtons: true,
        });

        if (result.isConfirmed) {
            const form = document.createElement('form');
            form.method = 'POST';
            form.action = `/admin/periodos/eliminar/${id}`;

            const token = document.createElement('input');
            token.type = 'hidden';
            token.name = '__RequestVerificationToken';
            token.value = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

            form.appendChild(token);
            document.body.appendChild(form);
            form.submit();
        }
    }

    window.deleteTerm = deleteTerm;
})();
