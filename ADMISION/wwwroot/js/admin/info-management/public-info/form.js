// Shared form logic for PublicInfo Create + Edit: repoblar modalidades al cambiar el periodo.
(function () {
    function waitFor(id, cb, attempts) {
        attempts = attempts || 0;
        if (window.customSelectRegistry && window.customSelectRegistry[id]) cb();
        else if (attempts < 40) setTimeout(() => waitFor(id, cb, attempts + 1), 50);
    }
    waitFor('TermId', () => {
        const termInput = document.getElementById('TermId');
        termInput?.addEventListener('change', async () => {
            const termId = termInput.value;
            if (!window.customSelectRegistry['ModalityId']) return;
            window.customSelectRegistry['ModalityId'].clear();
            if (!termId) return;
            try {
                const resp = await fetch(`/admin/info-management/public-infos/modalities-by-term?termId=${termId}`);
                const data = await resp.json();
                const list = document.getElementById('options_ModalityId');
                if (!list) return;
                list.innerHTML = '';
                data.forEach(m => {
                    const li = document.createElement('li');
                    li.className = 'px-4 py-3 select-option transition-all';
                    li.textContent = m.name;
                    li.dataset.value = m.id;
                    li.onclick = () => window.customSelectRegistry['ModalityId'].setValue(m.id, m.name);
                    list.appendChild(li);
                });
            } catch (e) { console.error(e); }
        });
    });
})();
