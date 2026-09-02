(function () {
    const initialData = window.ProspectsData || [];

    $(document).ready(function () {
        DT.load('prospectsTable', { data: initialData });

        const select = document.getElementById('filterTerm');

        const termOptions = Array.from(new Set(initialData.map(p => p.termId))).map(id => {
            const p = initialData.find(x => x.termId === id);
            return { value: id, label: p.termName };
        }).sort((a, b) => b.label.localeCompare(a.label));

        termOptions.forEach(opt => {
            const el = document.createElement('option');
            el.value = opt.value;
            el.text = opt.label;
            select.appendChild(el);
        });

        if (typeof initCustomSelect === 'function') {
            initCustomSelect('filterTerm');
        }

        function applyFilters() {
            const nameVal = $('#filterName').val().toLowerCase();
            const termVal = $('#filterTerm').val();

            const filtered = initialData.filter(p => {
                const matchesName = p.name.toLowerCase().includes(nameVal) || (p.fileName || '').toLowerCase().includes(nameVal);
                const matchesTerm = !termVal || p.termId === termVal;
                return matchesName && matchesTerm;
            });

            DT.load('prospectsTable', { data: filtered });
        }

        $('#filterName').on('input', applyFilters);
        $('#filterTerm').on('change', applyFilters);

        document.addEventListener('dt:action', function (e) {
            const { key, row } = e.detail;
            if (key === 'delete') deleteProspect(row.id, row.name);
        });
    });

    async function deleteProspect(id, name) {
        const result = await Swal.fire({
            title: '¿Eliminar prospecto?',
            html: `Se eliminará <strong>"${name}"</strong> de forma permanente.`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#f54477',
            cancelButtonColor: '#6b7280',
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar',
            reverseButtons: true
        });

        if (result.isConfirmed) {
            Swal.fire({
                title: 'Eliminando…',
                allowOutsideClick: false,
                didOpen: () => Swal.showLoading()
            });
            const form = document.createElement('form');
            form.method = 'POST';
            form.action = `/admin/info-management/prospects/eliminar/${id}`;
            const token = document.querySelector('input[name="__RequestVerificationToken"]');
            if (token) form.appendChild(token.cloneNode(true));
            document.body.appendChild(form);
            form.submit();
        }
    }

    window.deleteProspect = deleteProspect;
})();
