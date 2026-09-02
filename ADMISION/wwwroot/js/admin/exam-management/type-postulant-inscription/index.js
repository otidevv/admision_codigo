$(document).ready(function () {
    DT.registerRenderer('renderDiscount', (val) => {
        const wrap = document.createElement('div');
        wrap.className = 'flex items-center justify-center gap-1.5 font-black text-ink-700';
        wrap.innerHTML = `<span class="text-lg">${val ?? 0}</span><span class="text-[10px] text-ink-400">%</span>`;
        return wrap;
    });

    let searchTimeout;
    $('#filterSearch').on('input', function () {
        clearTimeout(searchTimeout);
        const search = $(this).val();
        searchTimeout = setTimeout(() => {
            DT.filter('postulantTypesTable', { search });
        }, 300);
    });

    document.addEventListener('dt:action', function (e) {
        const { key, row } = e.detail;
        if (key === 'delete') {
            Swal.fire({
                title: '¿Eliminar tipo de postulante?',
                text: `Estás a punto de eliminar el tipo "${row.name}". Esta acción no se puede deshacer.`,
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
                    form.action = '/admin/exam-management/applicant-types/eliminar/' + row.id;

                    const token = document.querySelector('input[name="__RequestVerificationToken"]');
                    if (token) form.appendChild(token.cloneNode(true));

                    document.body.appendChild(form);
                    form.submit();
                }
            });
        }
    });
});
