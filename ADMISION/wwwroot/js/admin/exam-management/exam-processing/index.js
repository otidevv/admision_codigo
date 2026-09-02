(function () {
    const tematicAreaOptions = window.ExamProcessingConfig?.tematicAreas || [];

    document.getElementById('btnAddArea')?.addEventListener('click', () => {
        const container = document.getElementById('areaRows');
        const opts = tematicAreaOptions.map(t => `<option value="${t.id}">${t.code}</option>`).join('');
        const html = `
            <div class="grid grid-cols-1 md:grid-cols-12 gap-2 area-row">
                <div class="md:col-span-5">
                    <select name="tematicAreaIds" class="form-input">
                        <option value="">-- Área --</option>${opts}
                    </select>
                </div>
                <div class="md:col-span-2"><input type="number" name="numeroInicios" value="1" placeholder="Desde" class="form-input" /></div>
                <div class="md:col-span-2"><input type="number" name="numeroFines" value="100" placeholder="Hasta" class="form-input" /></div>
                <div class="md:col-span-2"><input type="number" step="0.001" name="pesos" value="1" placeholder="Peso" class="form-input" /></div>
                <div class="md:col-span-1 flex"><button type="button" class="btn-remove-area w-10 h-10 inline-flex items-center justify-center rounded-md ring-1 ring-rose-200 dark:ring-rose-500/30 text-rose-500 hover:bg-rose-50 dark:hover:bg-rose-500/10 transition-colors"><i class="ti ti-trash text-xs"></i></button></div>
            </div>`;
        container.insertAdjacentHTML('beforeend', html);
    });

    document.addEventListener('click', (e) => {
        if (e.target.closest('.btn-remove-area')) {
            e.target.closest('.area-row')?.remove();
        }
    });

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    document.querySelectorAll('.answer-cell').forEach(cell => {
        let clickTimer;
        cell.addEventListener('click', () => {
            clearTimeout(clickTimer);
            clickTimer = setTimeout(async () => {
                const id = cell.dataset.id;
                const anulada = cell.dataset.anulada === 'true';
                const fd = new FormData();
                fd.append('keyId', id);
                fd.append('isAnulada', (!anulada).toString());
                fd.append('__RequestVerificationToken', token);
                const r = await fetch('/admin/exam-management/processing/toggle-anulada', { method: 'POST', body: fd });
                if (r.ok) location.reload();
            }, 250);
        });
        cell.addEventListener('dblclick', async () => {
            clearTimeout(clickTimer);
            const current = cell.dataset.respuesta;
            const ov = cell.dataset.override;
            const { value: formValues } = await Swal.fire({
                title: `Pregunta ${cell.dataset.num}`,
                html: `
                    <label class="block text-xs font-semibold text-left mb-1">Respuesta correcta</label>
                    <select id="swalResp" class="swal2-input" style="display:flex;margin:0 auto">
                        ${['A', 'B', 'C', 'D', 'E'].map(l => `<option value="${l}" ${l === current ? 'selected' : ''}>${l}</option>`).join('')}
                    </select>
                    <label class="block text-xs font-semibold text-left mt-2 mb-1">Puntos override (opcional)</label>
                    <input id="swalPts" type="number" step="0.0001" class="swal2-input" value="${ov}" />`,
                focusConfirm: false,
                preConfirm: () => ({
                    respuesta: document.getElementById('swalResp').value,
                    puntos: document.getElementById('swalPts').value
                })
            });
            if (formValues) {
                const fd = new FormData();
                fd.append('keyId', cell.dataset.id);
                fd.append('respuesta', formValues.respuesta);
                if (formValues.puntos) fd.append('puntosOverride', formValues.puntos);
                fd.append('__RequestVerificationToken', token);
                const r = await fetch('/admin/exam-management/processing/update-key', { method: 'POST', body: fd });
                if (r.ok) location.reload();
            }
        });
    });

    document.getElementById('btnProcess')?.addEventListener('click', () => {
        Swal.fire({ title: '¿Procesar puntajes?', text: 'Se recalcularán todos los resultados.', icon: 'question', showCancelButton: true, confirmButtonText: 'Sí, procesar', cancelButtonText: 'Cancelar', confirmButtonColor: '#f31a5b', cancelButtonColor: '#8b93a5', reverseButtons: true })
            .then(r => { if (r.isConfirmed) document.getElementById('formProcess').submit(); });
    });
    document.getElementById('btnPublish')?.addEventListener('click', () => {
        Swal.fire({ title: '¿Publicar sesión?', text: 'La sesión se marcará como publicada.', icon: 'warning', showCancelButton: true, confirmButtonText: 'Sí, publicar', cancelButtonText: 'Cancelar', confirmButtonColor: '#10b981', cancelButtonColor: '#8b93a5', reverseButtons: true })
            .then(r => { if (r.isConfirmed) document.getElementById('formPublish').submit(); });
    });
    document.getElementById('btnReset')?.addEventListener('click', () => {
        Swal.fire({ title: '¿Limpiar datos?', text: 'Se eliminarán clave, respuestas y resultados. Los parámetros y áreas permanecen.', icon: 'warning', showCancelButton: true, confirmButtonText: 'Sí, limpiar', cancelButtonText: 'Cancelar', confirmButtonColor: '#f43f5e', cancelButtonColor: '#8b93a5', reverseButtons: true })
            .then(r => { if (r.isConfirmed) document.getElementById('formReset').submit(); });
    });
})();
