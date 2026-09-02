(function () {
    var initialData = window.BannersData || [];

    $(document).ready(function () {
        DT.load('bannersTable', { data: initialData });

        document.addEventListener('dt:action', function (e) {
            var key = e.detail.key;
            var row = e.detail.row;
            if (key === 'delete') deleteBanner(row.id, row.previewType);
        });
    });

    async function deleteBanner(id, name) {
        var displayName = name || 'este banner';
        var result = await Swal.fire({
            title: '¿Eliminar banner?',
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
            form.action = '/admin/info-management/banners/eliminar/' + id;
            var token = document.querySelector('input[name="__RequestVerificationToken"]');
            if (token) form.appendChild(token.cloneNode(true));
            document.body.appendChild(form);
            form.submit();
        }
    }

    window.deleteBanner = deleteBanner;
})();