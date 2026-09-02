(function () {
    const cfg = window.ExamScheduleConfig || {};
    const scheduleId = cfg.scheduleId || null;
    const modalityId = cfg.modalityId || null;
    const termId = cfg.termId || null;
    const existingRooms = cfg.existingRooms || [];
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    let pavilions = [];
    let allClassrooms = [];
    let teachers = [];
    let tematicAreas = [];

    function fmt(n) { return (n ?? 0).toLocaleString('es-PE'); }
    function authHeaders() {
        return { 'Content-Type': 'application/json', 'RequestVerificationToken': token || '' };
    }

    async function loadDropdowns() {
        const [pavRes, teachRes, areaRes, classRes] = await Promise.all([
            fetch('/admin/infrastructure/exam-schedule/pavilions').then(r => r.json()),
            fetch('/admin/infrastructure/exam-schedule/teachers').then(r => r.json()),
            termId ? fetch(`/admin/infrastructure/exam-schedule/tematic-areas/${termId}`).then(r => r.json()) : Promise.resolve([]),
            fetch('/admin/infrastructure/exam-schedule/classrooms').then(r => r.json())
        ]);
        pavilions = pavRes;
        teachers = teachRes;
        tematicAreas = areaRes;
        allClassrooms = classRes;
    }

    function populateSelect(sel, items, valueKey, labelKey, selectedVal) {
        sel.innerHTML = '<option value="">Seleccione…</option>';
        items.forEach(it => {
            const opt = document.createElement('option');
            opt.value = it[valueKey];
            opt.textContent = it[labelKey];
            if (selectedVal && opt.value === String(selectedVal)) opt.selected = true;
            sel.appendChild(opt);
        });
    }

    function populateClassroomsForPavilion(pavilionSel, classroomSel, selectedClassroomId) {
        const pavId = pavilionSel.value;
        const filtered = pavId ? allClassrooms.filter(c => c.pavilionId === pavId) : allClassrooms;
        populateSelect(classroomSel, filtered, 'id', 'name', selectedClassroomId);
        updateCapacityLabel(classroomSel);
    }

    function updateCapacityLabel(classroomSel) {
        const row = classroomSel.closest('.room-row');
        const capVal = row.querySelector('.room-capacity-val');
        const capInput = row.querySelector('.room-capacity');
        const cls = allClassrooms.find(c => c.id === classroomSel.value);
        capVal.textContent = cls ? cls.capacity : '—';
        if (cls) capInput.max = cls.capacity;
    }

    function createRoomRow(room) {
        const div = document.createElement('div');
        div.className = 'room-row ring-1 ring-ink-200 rounded-md p-3 space-y-2';
        div.innerHTML = `
            <div class="grid grid-cols-1 md:grid-cols-4 gap-2">
                <div class="form-field">
                    <label class="form-label text-[11px]">Pabellón</label>
                    <select class="form-input room-pavilion"></select>
                </div>
                <div class="form-field">
                    <label class="form-label text-[11px]">Aula</label>
                    <select class="form-input room-classroom"></select>
                </div>
                <div class="form-field">
                    <label class="form-label text-[11px]">Área temática</label>
                    <select class="form-input room-area"></select>
                </div>
                <div class="form-field">
                    <label class="form-label text-[11px]">Aforo (max: <span class="room-capacity-val">—</span>)</label>
                    <input type="number" class="form-input room-capacity" min="1" value="${room?.assignedCapacity || 1}" />
                </div>
            </div>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-2">
                <div class="form-field">
                    <label class="form-label text-[11px]">Docente (opcional)</label>
                    <select class="form-input room-teacher"><option value="">Sin docente</option></select>
                </div>
                <div class="flex items-end justify-end">
                    <button type="button" class="btn-remove-room text-rose-500 hover:text-rose-700 text-xs font-semibold px-2 py-1 rounded hover:bg-rose-50 transition-all">
                        <i class="ti ti-trash"></i> Quitar
                    </button>
                </div>
            </div>`;

        const pavSel = div.querySelector('.room-pavilion');
        const clsSel = div.querySelector('.room-classroom');
        const areaSel = div.querySelector('.room-area');
        const teachSel = div.querySelector('.room-teacher');
        const capInput = div.querySelector('.room-capacity');

        populateSelect(pavSel, pavilions, 'id', 'name', room?.pavilionId || '');
        pavSel.addEventListener('change', () => populateClassroomsForPavilion(pavSel, clsSel, room?.classroomId));
        populateClassroomsForPavilion(pavSel, clsSel, room?.classroomId);
        populateSelect(areaSel, tematicAreas, 'id', 'code', room?.tematicAreaId || '');
        populateSelect(teachSel, teachers, 'id', 'fullName', room?.teacherId || '');

        div.querySelector('.btn-remove-room').addEventListener('click', () => div.remove());

        return div;
    }

    async function openScheduleModal() {
        await loadDropdowns();
        const container = document.getElementById('roomsContainer');
        container.innerHTML = '';

        if (existingRooms.length > 0) {
            existingRooms.forEach(r => {
                const roomData = {
                    classroomId: r.classroomId,
                    tematicAreaId: r.tematicAreaId,
                    teacherId: r.teacherId || '',
                    assignedCapacity: r.assignedCapacity,
                    pavilionId: pavilions.find(p => {
                        const cls = allClassrooms.find(c => c.id === r.classroomId);
                        return cls && cls.pavilionId === p.id;
                    })?.id || ''
                };
                container.appendChild(createRoomRow(roomData));
            });
        } else {
            container.appendChild(createRoomRow(null));
        }

        window.ADM?.Modal?.open('scheduleModal');
    }

    async function saveSchedule() {
        const name = document.getElementById('scheduleName').value.trim();
        if (!name) { Swal.fire('Error', 'Ingrese un nombre para el examen', 'error'); return; }

        const rows = document.querySelectorAll('#roomsContainer .room-row');
        const rooms = [];
        for (const row of rows) {
            const classroomId = row.querySelector('.room-classroom').value;
            const tematicAreaId = row.querySelector('.room-area').value;
            const teacherId = row.querySelector('.room-teacher').value || null;
            const assignedCapacity = parseInt(row.querySelector('.room-capacity').value, 10);
            if (!classroomId || !tematicAreaId || !assignedCapacity) {
                Swal.fire('Error', 'Complete todos los campos de cada aula (aula, área y aforo).', 'error');
                return;
            }
            rooms.push({ classroomId, tematicAreaId, teacherId, assignedCapacity });
        }

        if (rooms.length === 0) { Swal.fire('Error', 'Agregue al menos un aula.', 'error'); return; }

        const existingId = document.getElementById('scheduleId').value;
        const payload = existingId
            ? { id: existingId, rooms }
            : { name, modalityId, termId, rooms };

        const url = existingId
            ? '/admin/infrastructure/exam-schedule/update'
            : '/admin/infrastructure/exam-schedule/create';

        try {
            const resp = await fetch(url, { method: 'POST', headers: authHeaders(), body: JSON.stringify(payload) });
            if (resp.ok) {
                Swal.fire({ title: 'Guardado', text: 'Horario configurado correctamente.', icon: 'success', confirmButtonColor: '#f54477' })
                    .then(() => location.reload());
            } else {
                const data = await resp.json();
                Swal.fire({ title: 'Error', html: data.errors ? data.errors.join('<br>') : 'Error al guardar', icon: 'error', confirmButtonColor: '#f54477' });
            }
        } catch (e) {
            Swal.fire({ title: 'Error', text: 'Error de conexión', icon: 'error', confirmButtonColor: '#f54477' });
        }
    }

    async function runPreview() {
        if (!scheduleId) { Swal.fire('Error', 'Primero configure el horario.', 'error'); return; }
        const panel = document.getElementById('previewPanel');
        panel.classList.remove('hidden');
        panel.innerHTML = '<div class="bg-white dark:bg-ink-900 rounded-md ring-soft p-6 text-center text-ink-500"><i class="ti ti-loader-2 fa-spin mr-2"></i> Calculando vista previa…</div>';

        const fd = new FormData();
        fd.append('examScheduleId', scheduleId);
        fd.append('__RequestVerificationToken', token);

        const resp = await fetch('/admin/infrastructure/exam-assignment/preview', { method: 'POST', body: fd });
        if (!resp.ok) { panel.innerHTML = '<div class="bg-rose-50 ring-1 ring-rose-200 rounded-md p-4 text-sm text-rose-700">Error al generar la vista previa.</div>'; return; }
        render(await resp.json());
    }

    function render(s) {
        const panel = document.getElementById('previewPanel');
        const areasRows = (s.porArea || []).map(a => `
            <tr>
                <td><span class="badge b-secondary">${a.areaCode}</span></td>
                <td class="text-center">${fmt(a.cantidad)}</td>
                <td class="text-center text-emerald-600 font-semibold">${fmt(a.asignadas)}</td>
                <td class="text-center text-rose-500 font-semibold">${fmt(a.cantidad - a.asignadas)}</td>
            </tr>`).join('');
        const salonRows = (s.porSalon || []).map(p => `
            <tr>
                <td><span class="badge b-blue">${p.pavilionCode}</span> <span class="text-xs text-ink-500 ml-1">${p.pavilionName}</span></td>
                <td class="text-center">${p.floor}</td>
                <td class="font-semibold text-ink-900 dark:text-ink-100">${p.classroomName}</td>
                <td><span class="badge b-secondary">${p.areaCode}</span></td>
                <td class="text-center">${p.capacity}</td>
                <td class="text-center text-emerald-600 font-semibold">${fmt(p.assigned)}</td>
                <td class="text-ink-600">${p.teacherName || 'Sin docente'}</td>
            </tr>`).join('');

        panel.innerHTML = `
        <div class="grid grid-cols-1 md:grid-cols-4 gap-4 mb-4">
            <div class="bg-white dark:bg-ink-900 rounded-md ring-soft p-4">
                <p class="text-[11px] text-ink-500 uppercase tracking-wide font-bold">Inscripciones</p>
                <p class="text-2xl font-semibold text-ink-900 dark:text-ink-100 mt-1">${fmt(s.totalInscripciones)}</p>
            </div>
            <div class="bg-white dark:bg-ink-900 rounded-md ring-soft p-4">
                <p class="text-[11px] text-ink-500 uppercase tracking-wide font-bold">Asignados</p>
                <p class="text-2xl font-semibold text-emerald-600 mt-1">${fmt(s.totalAsignadas)}</p>
            </div>
            <div class="bg-white dark:bg-ink-900 rounded-md ring-soft p-4">
                <p class="text-[11px] text-ink-500 uppercase tracking-wide font-bold">Sin cupo</p>
                <p class="text-2xl font-semibold text-rose-500 mt-1">${fmt(s.totalNoAsignadas)}</p>
            </div>
            <div class="bg-white dark:bg-ink-900 rounded-md ring-soft p-4">
                <p class="text-[11px] text-ink-500 uppercase tracking-wide font-bold">Aforo total</p>
                <p class="text-2xl font-semibold text-primary-600 mt-1">${fmt(s.totalAforo)}</p>
                <p class="text-[11px] text-ink-400 mt-0.5">${s.totalSalones} aulas</p>
            </div>
        </div>
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
            <div class="bg-white dark:bg-ink-900 rounded-md ring-soft overflow-hidden">
                <div class="px-5 py-3 border-b border-ink-200/60 dark:border-ink-800/60 flex items-center gap-3">
                    <span class="w-8 h-8 rounded-md bg-secondary-50 text-secondary-600 inline-flex items-center justify-center"><i class="ti ti-book text-xs"></i></span>
                    <h3 class="text-[13px] font-semibold text-ink-900 tracking-tight">Por área temática</h3>
                </div>
                <div class="overflow-x-auto">
                    <table class="atlas w-full">
                        <thead><tr><th>Área</th><th class="text-center">Total</th><th class="text-center">Asignados</th><th class="text-center">Pendientes</th></tr></thead>
                        <tbody>${areasRows || '<tr><td colspan="4" class="px-4 py-6 text-center text-ink-400">Sin datos</td></tr>'}</tbody>
                    </table>
                </div>
            </div>
            <div class="bg-white dark:bg-ink-900 rounded-md ring-soft overflow-hidden">
                <div class="px-5 py-3 border-b border-ink-200/60 dark:border-ink-800/60 flex items-center gap-3">
                    <span class="w-8 h-8 rounded-md bg-primary-50 text-primary-600 inline-flex items-center justify-center"><i class="ti ti-building text-xs"></i></span>
                    <h3 class="text-[13px] font-semibold text-ink-900 tracking-tight">Por aula</h3>
                </div>
                <div class="overflow-x-auto">
                    <table class="atlas w-full">
                        <thead><tr><th>Pabellón</th><th class="text-center">Piso</th><th>Aula</th><th>Área</th><th class="text-center">Aforo</th><th class="text-center">Asignados</th><th>Docente</th></tr></thead>
                        <tbody>${salonRows || '<tr><td colspan="7" class="px-4 py-6 text-center text-ink-400">Sin datos</td></tr>'}</tbody>
                    </table>
                </div>
            </div>
        </div>`;
    }

    document.getElementById('btnOpenScheduleModal')?.addEventListener('click', openScheduleModal);
    document.getElementById('btnSaveSchedule')?.addEventListener('click', saveSchedule);
    document.getElementById('btnAddRoom')?.addEventListener('click', () => {
        document.getElementById('roomsContainer').appendChild(createRoomRow(null));
    });
    document.getElementById('btnPreview')?.addEventListener('click', runPreview);

    document.getElementById('btnExecute')?.addEventListener('click', () => {
        Swal.fire({
            title: '¿Ejecutar sorteo?',
            text: 'Se sobrescribirán las asignaciones existentes.',
            icon: 'question', showCancelButton: true, confirmButtonColor: '#f54477', cancelButtonColor: '#6b7280',
            confirmButtonText: 'Sí, ejecutar', cancelButtonText: 'Cancelar', reverseButtons: true
        }).then(r => {
            if (r.isConfirmed) {
                Swal.fire({ title: 'Ejecutando sorteo…', allowOutsideClick: false, didOpen: () => Swal.showLoading() });
                document.getElementById('formExecute').submit();
            }
        });
    });

    document.getElementById('btnClear')?.addEventListener('click', () => {
        Swal.fire({
            title: '¿Revertir sorteo?',
            html: 'Se eliminarán las asignaciones. Podrá <strong>volver a ejecutar</strong> el sorteo después.',
            icon: 'warning', showCancelButton: true, confirmButtonColor: '#f54477', cancelButtonColor: '#6b7280',
            confirmButtonText: 'Sí, revertir', cancelButtonText: 'Cancelar', reverseButtons: true
        }).then(r => {
            if (r.isConfirmed) {
                Swal.fire({ title: 'Revirtiendo…', allowOutsideClick: false, didOpen: () => Swal.showLoading() });
                document.getElementById('formClear').submit();
            }
        });
    });
})();
