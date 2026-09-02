$(document).ready(function () {
    const cfg = window.VacanciesConfig || {};
    const defaultTermId = cfg.defaultTermId || '';
    const selectedModalityId = cfg.selectedModalityId || '';
    const selectedModalityName = cfg.selectedModalityName || '';
    const termsJson = cfg.terms || [];

    let initializing = true;

    setTimeout(() => {
        if (defaultTermId && window.customSelectRegistry['termId']) {
            const term = termsJson.find(t => t.id === defaultTermId);
            if (term) window.customSelectRegistry['termId'].setValue(term.id, term.name);
        }
        if (selectedModalityId && selectedModalityName && window.customSelectRegistry['modalityId']) {
            window.customSelectRegistry['modalityId'].setValue(selectedModalityId, selectedModalityName);
        }
        setTimeout(() => { initializing = false; }, 50);
    }, 200);

    $('#termId').change(function () {
        if (initializing) return;
        const termId = $(this).val();
        if (termId) {
            const modSelect = window.customSelectRegistry['modalityId'];
            if (modSelect) {
                modSelect.clear();
                modSelect.load('/admin/exam-management/modalities/get-by-term/' + termId);
            }
        }
    });

    $('#modalityId').change(function () {
        if (initializing) return;
        const modalityId = $(this).val();
        if (modalityId) {
            const termId = $('#termId').val();
            window.location.href = `/admin/exam-management/vacancies?termId=${termId}&modalityId=${modalityId}`;
        }
    });
});
