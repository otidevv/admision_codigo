(function () {
    const form = document.getElementById('filterForm');
    const selTerm = form.querySelector('select[name="termId"]');
    const selModality = form.querySelector('select[name="modalityId"]');
    const selTypeModality = form.querySelector('select[name="typeModalityId"]');

    selTerm?.addEventListener('change', () => {
        selModality.value = '';
        selTypeModality.value = '';
        form.submit();
    });
    selModality?.addEventListener('change', () => {
        selTypeModality.value = '';
        form.submit();
    });
    selTypeModality?.addEventListener('change', () => form.submit());
    form.querySelector('select[name="typePostulantId"]')?.addEventListener('change', () => form.submit());

    function buildQuery() {
        const params = new URLSearchParams();
        ['termId', 'modalityId', 'typeModalityId', 'typePostulantId'].forEach(k => {
            const v = form.querySelector(`select[name="${k}"]`)?.value;
            if (v) params.set(k, v);
        });
        return params.toString();
    }

    document.getElementById('btnExportExcel')?.addEventListener('click', (e) => {
        e.preventDefault();
        window.location.href = '/admin/reportes/postulantes-por-area/export/excel?' + buildQuery();
    });
    document.getElementById('btnExportPdf')?.addEventListener('click', (e) => {
        e.preventDefault();
        window.location.href = '/admin/reportes/postulantes-por-area/export/pdf?' + buildQuery();
    });
})();
