(function () {
    const cfg = window.ModalityRequisitesConfig || {};
    const termsJson = cfg.terms || [];
    const defaultTermId = cfg.defaultTermId || null;

    const placeholder = document.getElementById('gridPlaceholder');
    const loader = document.getElementById('gridLoader');
    const empty = document.getElementById('gridEmpty');
    const container = document.getElementById('gridContainer');
    const btnSubmit = document.getElementById('btnSubmit');
    const btnSubmitLabel = document.getElementById('btnSubmitLabel');
    const btnSelectAll = document.getElementById('btnSelectAll');
    const btnClearAll = document.getElementById('btnClearAll');

    function showOnly(el) {
        [placeholder, loader, empty, container].forEach(x => x.classList.add('hidden'));
        el.classList.remove('hidden');
    }

    function getTermId() { return document.getElementById('TermId')?.value || ''; }
    function getRequirementId() { return document.getElementById('requirementId')?.value || ''; }

    function updateSummary() {
        const checked = container.querySelectorAll('input[type=checkbox]:checked:not(:disabled)');
        const count = checked.length;
        btnSubmit.disabled = count === 0;
        btnSubmitLabel.textContent = count === 0
            ? 'Guardar asignaciones'
            : `Guardar ${count} asignación${count === 1 ? '' : 'es'}`;

        const total = container.querySelectorAll('input[type=checkbox]:not(:disabled)').length;
        btnSelectAll.disabled = total === 0;
        btnClearAll.disabled = total === 0;
    }

    function renderGrid(items) {
        container.innerHTML = '';
        if (!items || items.length === 0) { showOnly(empty); updateSummary(); return; }

        const tpl = document.getElementById('modalityRowTpl');
        items.forEach(m => {
            const node = tpl.content.firstElementChild.cloneNode(true);
            node.querySelector('.modality-name').textContent = m.modalityName;

            const cb = node.querySelector('.modality-check');
            const hasTypes = (m.types || []).length > 0;

            if (hasTypes) {
                cb.dataset.kind = 'parent';
                cb.dataset.modalityId = m.modalityId;
                const toggle = node.querySelector('.toggle-types');
                const panel = node.querySelector('.types-panel');
                toggle.classList.remove('hidden');

                m.types.forEach(t => {
                    const row = document.createElement('label');
                    row.className = 'flex items-center gap-3 cursor-pointer pl-7';
                    row.innerHTML = `
                        <input type="checkbox" name="selections" value="${m.modalityId}:${t.id}"
                               class="type-check h-4 w-4 text-primary-600 rounded border-ink-300 focus:ring-primary-500" />
                        <span class="text-sm text-ink-700 dark:text-ink-200">${t.name}</span>
                        ${t.alreadyAssigned ? '<span class="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-wide bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200 px-2 py-0.5 rounded-md"><i class="ti ti-check"></i> Ya asignado</span>' : ''}
                    `;
                    const tcb = row.querySelector('input');
                    if (t.alreadyAssigned) {
                        tcb.checked = true;
                        tcb.disabled = true;
                        tcb.removeAttribute('name');
                    }
                    tcb.addEventListener('click', (e) => {
                        if (tcb.disabled) {
                            e.preventDefault();
                            Swal.fire({
                                icon: 'info',
                                title: 'Ya está asignado',
                                text: `El tipo "${t.name}" ya tiene este requisito en este periodo.`,
                                confirmButtonColor: '#2563eb'
                            });
                        }
                        updateSummary();
                    });
                    panel.appendChild(row);
                });

                toggle.addEventListener('click', () => {
                    const open = !panel.classList.contains('hidden');
                    panel.classList.toggle('hidden', open);
                    toggle.querySelector('.toggle-label').textContent = open ? 'Ver tipos' : 'Ocultar tipos';
                    toggle.querySelector('i').classList.toggle('ti-chevron-down', open);
                    toggle.querySelector('i').classList.toggle('ti-chevron-up', !open);
                });

                cb.addEventListener('click', (e) => {
                    const willCheck = cb.checked;
                    panel.querySelectorAll('input.type-check:not(:disabled)').forEach(c => c.checked = willCheck);
                    updateSummary();
                });
            } else {
                cb.dataset.kind = 'leaf';
                cb.setAttribute('name', 'selections');
                cb.value = m.modalityId;
                if (m.alreadyAssigned) {
                    cb.checked = true;
                    cb.disabled = true;
                    cb.removeAttribute('name');
                    node.querySelector('.assigned-badge').classList.remove('hidden');
                }
                cb.addEventListener('click', (e) => {
                    if (cb.disabled) {
                        e.preventDefault();
                        Swal.fire({
                            icon: 'info',
                            title: 'Ya está asignado',
                            text: `La modalidad "${m.modalityName}" ya tiene este requisito en este periodo.`,
                            confirmButtonColor: '#2563eb'
                        });
                    }
                    updateSummary();
                });
            }

            container.appendChild(node);
        });

        showOnly(container);
        updateSummary();
    }

    async function loadGrid() {
        const termId = getTermId();
        const reqId = getRequirementId();
        if (!termId || !reqId) { showOnly(placeholder); updateSummary(); return; }

        showOnly(loader);
        try {
            const resp = await fetch(`/admin/exam-management/requirements-by-modality/api/grid?termId=${termId}&requirementId=${reqId}`);
            if (!resp.ok) throw new Error('HTTP ' + resp.status);
            const data = await resp.json();
            renderGrid(data);
        } catch (err) {
            console.error(err);
            Swal.fire({ icon: 'error', title: 'No se pudo cargar la grilla', text: 'Intenta de nuevo en unos segundos.' });
            showOnly(placeholder);
        }
    }

    setTimeout(() => {
        if (defaultTermId && window.customSelectRegistry?.['TermId']) {
            const term = termsJson.find(t => t.id === defaultTermId);
            if (term) window.customSelectRegistry['TermId'].setValue(term.id, term.name);
        }
    }, 200);

    document.getElementById('TermId')?.addEventListener('change', loadGrid);
    document.getElementById('requirementId')?.addEventListener('change', loadGrid);

    btnSelectAll.addEventListener('click', () => {
        container.querySelectorAll('input[type=checkbox]:not(:disabled)').forEach(c => { c.checked = true; });
        updateSummary();
    });
    btnClearAll.addEventListener('click', () => {
        container.querySelectorAll('input[type=checkbox]:not(:disabled)').forEach(c => { c.checked = false; });
        updateSummary();
    });

    document.getElementById('bulkAssignForm').addEventListener('submit', (e) => {
        const reqId = getRequirementId();
        if (!reqId) {
            e.preventDefault();
            Swal.fire({ icon: 'warning', title: 'Falta el requisito', text: 'Selecciona el requisito a asignar.' });
            return;
        }
        const count = container.querySelectorAll('input[type=checkbox][name="selections"]:checked').length;
        if (count === 0) {
            e.preventDefault();
            Swal.fire({ icon: 'warning', title: 'Sin selecciones', text: 'Marca al menos una modalidad o tipo.' });
        }
    });
})();
