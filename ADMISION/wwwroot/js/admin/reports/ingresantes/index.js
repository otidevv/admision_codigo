(function () {
    var form = document.getElementById('filterForm');
    var selTerm = form.querySelector('select[name="termId"]');
    var selModality = form.querySelector('select[name="modalityId"]');
    var selTypeModality = form.querySelector('select[name="typeModalityId"]');
    var selTipoReporte = form.querySelector('select[name="tipoReporte"]');
    var btnExportConsolidado = document.getElementById('btnExportConsolidado');

    selTerm?.addEventListener('change', function () {
        selModality.value = '';
        selTypeModality.value = '';
        form.submit();
    });
    selModality?.addEventListener('change', function () {
        selTypeModality.value = '';
        form.submit();
    });
    selTypeModality?.addEventListener('change', function () { form.submit(); });
    form.querySelector('select[name="typePostulantId"]')?.addEventListener('change', function () { form.submit(); });
    form.querySelector('select[name="careerId"]')?.addEventListener('change', function () { form.submit(); });
    form.querySelector('select[name="tematicAreaId"]')?.addEventListener('change', function () { form.submit(); });
    form.querySelector('select[name="segundaCarrera"]')?.addEventListener('change', function () { form.submit(); });
    selTipoReporte?.addEventListener('change', function () { form.submit(); });

    function buildQuery() {
        var params = new URLSearchParams();
        ['termId', 'modalityId', 'typeModalityId', 'typePostulantId', 'careerId', 'tematicAreaId', 'segundaCarrera', 'tipoReporte'].forEach(function (k) {
            var v = form.querySelector('select[name="' + k + '"]')?.value;
            if (v) params.set(k, v);
        });
        return params.toString();
    }

    function updateConsolidadoButton() {
        if (!btnExportConsolidado || !selTipoReporte) return;
        var esPreliminar = selTipoReporte.value === 'preliminar';
        btnExportConsolidado.innerHTML = esPreliminar
            ? '<i class="ti ti-file-export text-xs"></i> Preliminar Excel'
            : '<i class="ti ti-database-export text-xs"></i> Consolidado Excel';
    }

    selTipoReporte?.addEventListener('change', updateConsolidadoButton);
    updateConsolidadoButton();

    document.getElementById('btnExportExcel')?.addEventListener('click', function (e) {
        e.preventDefault();
        window.location.href = '/admin/reportes/ingresantes/export/excel?' + buildQuery();
    });
    document.getElementById('btnExportPdf')?.addEventListener('click', function (e) {
        e.preventDefault();
        window.location.href = '/admin/reportes/ingresantes/export/pdf?' + buildQuery();
    });
    btnExportConsolidado?.addEventListener('click', function (e) {
        e.preventDefault();
        var params = new URLSearchParams(buildQuery());
        var esPreliminar = selTipoReporte?.value === 'preliminar';
        var url = esPreliminar
            ? '/admin/reportes/ingresantes/export/preliminar?'
            : '/admin/reportes/ingresantes/export/consolidado?';
        window.location.href = url + params.toString();
    });
})();