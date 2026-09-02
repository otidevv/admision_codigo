// Shared form logic for TypeModalities Create + Edit.
$(document).ready(function () {
    const cfg = window.TypeModalitiesFormConfig || {};
    const defaultTermId = cfg.defaultTermId || '';
    const termsJson = cfg.terms || [];
    const allCareers = cfg.allCareers || [];

    setTimeout(() => {
        if (defaultTermId && window.customSelectRegistry['TermId']) {
            const term = termsJson.find(t => t.id === defaultTermId);
            if (term) {
                window.customSelectRegistry['TermId'].setValue(term.id, term.name);
                loadModalities(term.id);
            }
        }
    }, 200);

    $('#TermId').change(function () {
        const termId = $(this).val();
        loadModalities(termId);
    });

    $('#ModalityId').change(function () {
        const modalityId = $(this).val();
        filterCareersByModality(modalityId);
    });

    function loadModalities(termId) {
        if (window.customSelectRegistry['ModalityId']) {
            const modSelect = window.customSelectRegistry['ModalityId'];
            modSelect.clear();
            if (termId) {
                modSelect.load('/admin/exam-management/modalities/get-by-term/' + termId, function () {
                    filterCareersByModality(modSelect.getValue());
                });
            }
        }
    }

    function filterCareersByModality(modalityId) {
        const container = document.getElementById('careersContainer');
        if (!container) return;

        if (!modalityId || !window.careerModalityMap || !window.careerModalityMap[modalityId]) {
            container.innerHTML = '<p class="text-sm text-ink-500 italic">Primero selecciona una modalidad para ver las carreras disponibles.</p>';
            return;
        }

        const allowedIds = new Set(window.careerModalityMap[modalityId].map(id => String(id)));
        const items = allCareers.filter(c => allowedIds.has(String(c.id)));

        if (items.length === 0) {
            container.innerHTML = '<p class="text-sm text-ink-500 italic">No hay carreras asociadas a esta modalidad.</p>';
            return;
        }

        container.innerHTML = '<div class="flex flex-col gap-2">' +
            items.map(c => `
                <label class="career-item flex items-center gap-3 p-3 rounded-md ring-1 ring-ink-200/60 dark:ring-ink-800 cursor-pointer hover:bg-primary-50/50 dark:hover:bg-primary-500/5 transition-colors">
                    <input type="checkbox" name="careerIds" value="${c.id}"
                           class="w-4 h-4 rounded border-ink-300 text-primary-600 focus:ring-primary-500" />
                    <div class="min-w-0">
                        <span class="text-sm font-medium text-ink-800 dark:text-ink-200 block">${escapeHtml(c.name)}</span>
                        ${c.facultyName ? `<span class="text-[10px] text-ink-400 block">${escapeHtml(c.facultyName)}</span>` : ''}
                    </div>
                </label>
            `).join('') +
            '</div>';
    }

    function escapeHtml(str) {
        if (!str) return '';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }
});
