document.querySelectorAll('.btn-delete').forEach(btn => {
    btn.addEventListener('click', function () {
        const form = this.closest('form');
        const name = this.getAttribute('data-name');
        Swal.fire({
            title: '¿Eliminar beneficiario?',
            html: `Se eliminará <strong>"${name}"</strong> de forma permanente.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#f54477',
            cancelButtonColor: '#6b7280',
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar',
            reverseButtons: true
        }).then((result) => {
            if (result.isConfirmed) {
                Swal.fire({
                    title: 'Eliminando…',
                    allowOutsideClick: false,
                    didOpen: () => Swal.showLoading()
                });
                form.submit();
            }
        });
    });
});
