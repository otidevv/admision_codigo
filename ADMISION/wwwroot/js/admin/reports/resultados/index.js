(function () {
    var form = document.getElementById('filterForm');
    var selTerm = form.querySelector('select[name="termId"]');
    var selModality = form.querySelector('select[name="modalityId"]');
    var selTypeModality = form.querySelector('select[name="typeModalityId"]');

    selTerm?.addEventListener('change', function () {
        selModality.value = '';
        selTypeModality.value = '';
        form.submit();
    });
    selModality?.addEventListener('change', function () {
        selTypeModality.value = '';
        form.submit();
    });

    ['typeModalityId', 'typePostulantId', 'careerId', 'condicion'].forEach(function (name) {
        form.querySelector('select[name="' + name + '"]')?.addEventListener('change', function () { form.submit(); });
    });

    function buildQuery() {
        var params = new URLSearchParams();
        ['termId', 'modalityId', 'typeModalityId', 'typePostulantId', 'careerId', 'condicion'].forEach(function (k) {
            var v = form.querySelector('select[name="' + k + '"]')?.value;
            if (v) params.set(k, v);
        });
        return params.toString();
    }

    document.getElementById('btnExportExcel')?.addEventListener('click', function (e) {
        e.preventDefault();
        window.location.href = '/admin/reportes/resultados/export/excel?' + buildQuery();
    });
})();
