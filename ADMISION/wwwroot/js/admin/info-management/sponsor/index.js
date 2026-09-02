(function () {
    var initialData = window.SponsorsData || [];

    $(document).ready(function () {
        DT.load('sponsorsTable', { data: initialData });

        document.addEventListener('dt:action', function (e) {
            var key = e.detail.key;
            var row = e.detail.row;
            if (key === 'delete') deleteSponsor(row.id, row.name);
        });
    });

    async function deleteSponsor(id, name) {
        var displayName = name || 'este sponsor';
        var result = await Swal.fire({
            title: '¿Eliminar sponsor?',
            html: 'Se eliminará <strong>"' + displayName + '"</strong> de forma permanente.',
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
                didOpen: function () { Swal.showLoading(); }
            });
            var form = document.createElement('form');
            form.method = 'POST';
            form.action = '/admin/info-management/sponsors/eliminar/' + id;
            var token = document.querySelector('input[name="__RequestVerificationToken"]');
            if (token) form.appendChild(token.cloneNode(true));
            document.body.appendChild(form);
            form.submit();
        }
    }

    window.deleteSponsor = deleteSponsor;
})();
