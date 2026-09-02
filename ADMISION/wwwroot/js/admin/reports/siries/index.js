(function () {
    var form = document.getElementById('filterForm');

    form.querySelector('select[name="termId"]')?.addEventListener('change', function () { form.submit(); });

    function buildQuery() {
        var params = new URLSearchParams();
        var v = form.querySelector('select[name="termId"]')?.value;
        if (v) params.set('termId', v);
        return params.toString();
    }

    document.getElementById('btnExportExcel')?.addEventListener('click', function (e) {
        e.preventDefault();
        window.location.href = '/admin/reportes/siries/export/excel?' + buildQuery();
    });
    document.getElementById('btnExportPdf')?.addEventListener('click', function (e) {
        e.preventDefault();
        window.location.href = '/admin/reportes/siries/export/pdf?' + buildQuery();
    });
})();
