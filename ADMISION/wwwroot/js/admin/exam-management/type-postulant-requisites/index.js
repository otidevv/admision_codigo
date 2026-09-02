$(document).ready(function () {
    $('#typePostulantInscriptionId').change(function () {
        applyFilters();
    });

    let searchTimeout;
    $('#filterSearch').on('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(applyFilters, 300);
    });

    function applyFilters() {
        const params = {
            typePostulantInscriptionId: $('#typePostulantInscriptionId').val(),
            search: $('#filterSearch').val()
        };
        DT.filter('postulantRequisitesTable', params);
    }

    document.addEventListener('dt:action', function (e) {
        const { key, row } = e.detail;
        if (key === 'delete') {
            Swal.fire({
                title: '¿Eliminar asignación?',
                text: 'Esta acción eliminará el requisito asignado al tipo de postulante de forma permanente.',
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
                    form.action = '/admin/exam-management/requirements-by-type-postulant/eliminar/' + row.id;

                    const token = document.querySelector('input[name="__RequestVerificationToken"]');
                    if (token) form.appendChild(token.cloneNode(true));

                    document.body.appendChild(form);
                    form.submit();
                }
            });
        }
    });
});
