$(document).ready(function () {
    let searchTimeout;
    $('#filterSearch').on('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(refreshData, 300);
    });

    $('#filterFaculty').change(refreshData);

    document.addEventListener('dt:action', function (e) {
        const { key, row } = e.detail;
        if (key === 'delete') {
            Swal.fire({
                title: '¿Eliminar carrera?',
                html: `Se eliminará <strong>"${row.name}"</strong> de forma permanente.<br/><span class="text-xs text-ink-500">Esta acción no se puede deshacer.</span>`,
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
                    form.action = '/admin/exam-management/careers/eliminar/' + row.id;

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
        facultyId: $('#filterFaculty').val() || null,
        search: $('#filterSearch').val() || null
    };
    DT.filter('careersTable', params);
}

window.refreshData = refreshData;
