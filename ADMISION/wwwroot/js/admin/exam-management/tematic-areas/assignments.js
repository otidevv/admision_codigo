(function () {
    const cfg = window.TematicAreasAssignmentsConfig || {};
    const defaultTermId = cfg.defaultTermId || '';
    const termsJson = cfg.terms || [];
    const areas = cfg.areas || [];

    function makeRenderer(areaId) {
        return (val, col, row) => {
            const careerId = row?.id || row?.Id || '';
            const assignments = Array.isArray(val) ? val.map(String) : [];
            const isChecked = assignments.includes(areaId);
            const label = document.createElement('label');
            label.className = 'flex items-center justify-center cursor-pointer p-2 rounded-md hover:bg-primary-50 dark:hover:bg-primary-500/10 transition-colors group';
            label.innerHTML = `
                <input type="checkbox" class="area-check w-5 h-5 rounded border-2 border-ink-300 text-primary-600 focus:ring-primary-500 transition-all cursor-pointer group-hover:border-primary-400"
                    data-career-id="${careerId}" data-area-id="${areaId}" ${isChecked ? 'checked' : ''}>
            `;
            return label;
        };
    }

    $(document).ready(function () {
        areas.forEach(a => DT.registerRenderer('renderArea_' + a.idN, makeRenderer(a.id)));

        setTimeout(() => {
            if (defaultTermId && window.customSelectRegistry['termId']) {
                const term = termsJson.find(t => t.id === defaultTermId);
                if (term) {
                    window.customSelectRegistry['termId'].setValue(term.id, term.name);
                    loadMatrix(term.id);
                }
            }
        }, 200);

        $('#termId').change(function () {
            const termId = $(this).val();
            if (termId) loadMatrix(termId);
            else hideMatrix();
        });
    });

    function loadMatrix(termId) {
        $('#matrixContainer').removeClass('hidden');
        $('#noSelectedTerm').addClass('hidden');
        DT.filter('matrixTable', { termId });
    }

    function hideMatrix() {
        $('#matrixContainer').addClass('hidden');
        $('#noSelectedTerm').removeClass('hidden');
    }

    async function saveMatrix() {
        const termId = $('#termId').val();
        if (!termId) {
            Swal.fire({ title: 'Atención', text: 'Debe seleccionar un periodo académico primero.', icon: 'warning', confirmButtonColor: '#3085d6' });
            return;
        }

        const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
        const careerMap = {};
        $('.area-check').each(function () {
            const cid = String($(this).data('career-id') ?? $(this).attr('data-career-id') ?? '');
            const aid = String($(this).data('area-id') ?? $(this).attr('data-area-id') ?? '');
            if (!GUID_RE.test(cid) || !GUID_RE.test(aid)) return;
            if (!careerMap[cid]) careerMap[cid] = [];
            if ($(this).is(':checked')) careerMap[cid].push(aid);
        });

        const assignments = Object.keys(careerMap).map(id => ({
            careerId: id,
            tematicAreaIds: careerMap[id]
        }));

        if (assignments.some(a => !GUID_RE.test(a.careerId))) {
            Swal.fire('Error', 'Se detectaron carreras inválidas en la grilla. Recargue la página.', 'error');
            return;
        }

        if (assignments.length === 0) {
            Swal.fire('Atención', 'No hay datos cargados para guardar.', 'info');
            return;
        }

        const btn = $('#btnSaveMatrix');
        const originalContent = btn.html();
        btn.prop('disabled', true).html('<i class="ti ti-loader-2 fa-spin"></i> Guardando...');

        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        const token = tokenInput ? tokenInput.value : '';
        if (!token) {
            btn.prop('disabled', false).html(originalContent);
            Swal.fire('Error de seguridad', 'No se encontró el token anti-CSRF. Recargue la página (Ctrl+F5).', 'error');
            return;
        }

        try {
            const response = await fetch('/admin/exam-management/tematic-areas/save-matrix', {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json',
                    'RequestVerificationToken': token,
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: JSON.stringify({ termId, assignments })
            });

            const raw = await response.text();
            let result = null;
            try { result = raw ? JSON.parse(raw) : null; } catch (_) { }

            if (response.ok && result && result.success) {
                Swal.fire({ title: '¡Guardado!', text: 'La matriz de asignaciones se actualizó correctamente.', icon: 'success', timer: 2000, showConfirmButton: false });
                DT.refresh('matrixTable');
            } else {
                const msg = (result && result.message)
                    ? result.message
                    : `Error ${response.status}: ${response.statusText || 'sin detalle'}`;
                console.error('save-matrix failed', response.status, raw);
                Swal.fire('Error', msg, 'error');
            }
        } catch (err) {
            console.error('save-matrix exception', err);
            Swal.fire('Error de red', 'No se pudo contactar con el servidor.', 'error');
        } finally {
            btn.prop('disabled', false).html(originalContent);
        }
    }

    window.saveMatrix = saveMatrix;
})();
