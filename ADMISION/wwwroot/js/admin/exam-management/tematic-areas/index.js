$(document).ready(function () {
    let searchTimeout;
    $('#filterSearch').on('input', function () {
        clearTimeout(searchTimeout);
        const search = $(this).val();
        searchTimeout = setTimeout(() => {
            DT.filter('tematicAreasTable', { search });
        }, 300);
    });

    document.addEventListener('dt:action', function (e) {
        const { key, row } = e.detail;
        if (key === 'delete') {
            Swal.fire({
                title: '¿Eliminar área temática?',
                html: `Se eliminará <strong>"${row.code}"</strong> de forma permanente.`,
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
                    form.action = '/admin/exam-management/tematic-areas/eliminar/' + row.id;

                    const token = document.querySelector('input[name="__RequestVerificationToken"]');
                    if (token) form.appendChild(token.cloneNode(true));

                    document.body.appendChild(form);
                    form.submit();
                }
            });
        }
    });
});
