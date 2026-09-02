(function () {
    var form = document.getElementById('filterForm');
    var termSelect = form.querySelector('select[name="termId"]');
    var versionSelect = form.querySelector('select[name="versionId"]');

    termSelect?.addEventListener('change', function () {
        if (versionSelect) versionSelect.value = '';
        form.submit();
    });

    versionSelect?.addEventListener('change', function () { form.submit(); });

    function buildQuery() {
        var params = new URLSearchParams();
        var t = termSelect?.value;
        if (t) params.set('termId', t);
        var v = versionSelect?.value;
        if (v) params.set('versionId', v);
        return params.toString();
    }

    document.getElementById('btnExportExcel')?.addEventListener('click', function (e) {
        e.preventDefault();
        window.location.href = '/admin/reportes/cepre/export/excel?' + buildQuery();
    });
})();
