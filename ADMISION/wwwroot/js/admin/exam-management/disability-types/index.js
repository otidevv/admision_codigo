$(document).ready(function () {
    let timeout;
    $('#filterSearch').on('input', function () {
        clearTimeout(timeout);
        timeout = setTimeout(refreshData, 300);
    });

    $('#filterStatus').change(refreshData);

    document.addEventListener('dt:action', function (e) {
        const { key, row } = e.detail;

        if (key === 'delete') {
            Swal.fire({
                title: '¿Eliminar tipo de discapacidad?',
                html: `Se eliminará <strong>"${row.name}"</strong> de forma permanente.`,
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
                    form.action = '/admin/exam-management/disability-types/delete/' + row.id;

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
        search: $('#filterSearch').val() || null,
        isActive: $('#filterStatus').val() || null
    };

    DT.filter('disabilityTypesTable', params);
}

window.refreshData = refreshData;
