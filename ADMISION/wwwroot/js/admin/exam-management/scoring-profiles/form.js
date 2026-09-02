(function () {
    $(document).ready(function () {
        const cfg = window.ScoringProfileFormConfig || {};
        const defaultTermId = cfg.defaultTermId || '';
        const termsJson = cfg.terms || [];

        const rangesSection = document.getElementById('rangesSection');
        const rangesContainer = document.getElementById('rangesContainer');
        const rangesEmpty = document.getElementById('rangesEmpty');
        const btnAddRange = document.getElementById('btnAddRange');

        // ── Modo: Simple / Ponderado ──────────────────────────────
        function syncMode() {
            const weighted = document.getElementById('modeWeighted');
            const isWeighted = weighted && weighted.checked;
            if (rangesSection) rangesSection.classList.toggle('hidden', !isWeighted);
        }

        document.querySelectorAll('.js-mode-card').forEach(card => {
            card.addEventListener('click', () => {
                const radio = card.querySelector('input[type="radio"]');
                if (radio) {
                    radio.checked = true;
                    syncMode();
                }
            });
        });

        document.querySelectorAll('input[name="IsWeighted"]').forEach(radio => {
            radio.addEventListener('change', syncMode);
        });

        // ── Rangos dinámicos ──────────────────────────────────────
        function reindex() {
            const rows = rangesContainer.querySelectorAll('.range-row');
            rows.forEach((row, i) => {
                row.querySelector('.range-from').name = 'Ranges[' + i + '].FromQuestion';
                row.querySelector('.range-to').name = 'Ranges[' + i + '].ToQuestion';
                row.querySelector('.range-points').name = 'Ranges[' + i + '].PuntosCorrecta';
            });
            if (rangesEmpty) rangesEmpty.classList.toggle('hidden', rows.length > 0);
        }

        function addRange(from, to, points) {
            const row = document.createElement('div');
            row.className = 'range-row grid grid-cols-12 gap-3 items-end';
            row.innerHTML = `
                <div class="col-span-5 md:col-span-3">
                    <label class="block text-[11px] font-semibold text-ink-600 dark:text-ink-300 uppercase tracking-wide mb-1.5">Desde</label>
                    <input type="number" min="1" value="${from || ''}" placeholder="1"
                           class="form-input range-from" />
                </div>
                <div class="col-span-5 md:col-span-3">
                    <label class="block text-[11px] font-semibold text-ink-600 dark:text-ink-300 uppercase tracking-wide mb-1.5">Hasta</label>
                    <input type="number" min="1" value="${to || ''}" placeholder="12"
                           class="form-input range-to" />
                </div>
                <div class="col-span-10 md:col-span-4">
                    <label class="block text-[11px] font-semibold text-ink-600 dark:text-ink-300 uppercase tracking-wide mb-1.5">Puntos por correcta</label>
                    <input type="number" step="0.0001" min="0" value="${points || ''}" placeholder="5.0000"
                           class="form-input range-points" />
                </div>
                <div class="col-span-2 md:col-span-2 flex items-end justify-end">
                    <button type="button"
                            class="range-remove w-9 h-9 rounded-md ring-1 ring-ink-200 dark:ring-ink-700 text-ink-400 hover:text-rose-600 hover:ring-rose-300 hover:bg-rose-50 dark:hover:bg-rose-500/10 transition-all inline-flex items-center justify-center">
                        <i class="ti ti-trash text-[13px]"></i>
                    </button>
                </div>`;
            rangesContainer.appendChild(row);
            reindex();
        }

        rangesContainer.addEventListener('click', function (e) {
            const btn = e.target.closest('.range-remove');
            if (btn) {
                const row = btn.closest('.range-row');
                if (row) row.remove();
                reindex();
            }
        });

        if (btnAddRange) btnAddRange.addEventListener('click', () => addRange());

        // ── Selección en cascada: Periodo → Modalidad → Tipo ──────
        if (window.customSelectRegistry['TermId']) {
            window.customSelectRegistry['TermId'].enable();
        }

        $('#TermId').change(function () {
            const termId = $(this).val();
            loadModalities(termId);
        });

        $('#ModalityId').change(function () {
            const modalityId = $(this).val();
            loadTypes(modalityId);
        });

        function loadModalities(termId) {
            if (window.customSelectRegistry['ModalityId']) {
                const modSelect = window.customSelectRegistry['ModalityId'];
                modSelect.clear();
                if (termId) modSelect.load('/admin/exam-management/modalities/get-by-term/' + termId);
            }
            if (window.customSelectRegistry['TypeModalityId']) {
                window.customSelectRegistry['TypeModalityId'].clear();
            }
        }

        function loadTypes(modalityId) {
            if (window.customSelectRegistry['TypeModalityId']) {
                const typeSelect = window.customSelectRegistry['TypeModalityId'];
                typeSelect.clear();
                if (modalityId) typeSelect.load('/admin/exam-management/scoring-profiles/api/types/' + modalityId);
            }
        }

        // ── Init ──────────────────────────────────────────────────
        syncMode();
        reindex();

        // Al entrar con un periodo preseleccionado (edición), recargar las
        // modalidades; la edición ya trae las opciones precargadas en el partial.
        setTimeout(() => {
            if (defaultTermId && window.customSelectRegistry['TermId'] && !window.customSelectRegistry['TermId'].getOptions().length) {
                const term = termsJson.find(t => t.id === defaultTermId);
                if (term) {
                    window.customSelectRegistry['TermId'].setValue(term.id, term.name);
                    loadModalities(term.id);
                }
            }
        }, 200);
    });
})();
