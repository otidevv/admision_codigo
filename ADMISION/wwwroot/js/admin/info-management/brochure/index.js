(function () {
    const initialData = window.BrochureData || [];

    $(document).ready(function () {
        DT.load('brochuresTable', { data: initialData });

        $('#filterName').on('input', function () {
            const val = $(this).val().toLowerCase();
            const filtered = initialData.filter(o =>
                o.name.toLowerCase().includes(val) ||
                (o.description || '').toLowerCase().includes(val) ||
                (o.fileName || '').toLowerCase().includes(val)
            );
            DT.load('brochuresTable', { data: filtered });
        });

        document.addEventListener('dt:action', function (e) {
            const { key, row } = e.detail;
            if (key === 'delete') deleteBrochure(row.id, row.name);
        });
    });

    async function deleteBrochure(id, name) {
        const result = await Swal.fire({
            title: '¿Eliminar brochure?',
            html: `Se eliminará <strong>"${name}"</strong> de forma permanente.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#f54477',
            cancelButtonColor: '#6b7280',
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar',
            reverseButtons: true
        });

        if (result.isConfirmed) {
            Swal.fire({
                title: 'Eliminando…',
                allowOutsideClick: false,
                didOpen: () => Swal.showLoading()
            });
            const form = document.createElement('form');
            form.method = 'POST';
            form.action = `/admin/info-management/brochures/eliminar/${id}`;
            const token = document.querySelector('input[name="__RequestVerificationToken"]');
            if (token) form.appendChild(token.cloneNode(true));
            document.body.appendChild(form);
            form.submit();
        }
    }

    window.deleteBrochure = deleteBrochure;
})();
