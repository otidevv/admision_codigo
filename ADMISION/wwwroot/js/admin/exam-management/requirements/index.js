(function () {
    $(document).ready(function () {
        DT.registerRenderer('renderExtensions', (val, col, row) => {
            const btn = document.createElement('button');
            btn.className = 'inline-flex items-center gap-1.5 h-7 px-3 rounded-md text-[11px] font-semibold bg-primary-50 dark:bg-primary-500/10 text-primary-600 ring-1 ring-primary-200 dark:ring-primary-500/30 hover:bg-primary-100 dark:hover:bg-primary-500/20 transition-colors';
            btn.innerHTML = '<i class="ti ti-eye text-[9px]"></i> Ver';
            btn.onclick = (e) => {
                e.stopPropagation();
                showExtensions(row.name, row.filePathExtencion);
            };
            return btn;
        });

        let searchTimeout;
        $('#filterSearch').on('input', function () {
            clearTimeout(searchTimeout);
            const search = $(this).val();
            searchTimeout = setTimeout(() => {
                DT.filter('requirementsTable', { search });
            }, 300);
        });

        document.addEventListener('dt:action', function (e) {
            const { key, row } = e.detail;

            if (key === 'delete') {
                Swal.fire({
                    title: '¿Eliminar requisito?',
                    html: `Se eliminará <strong>"${row.name}"</strong> de forma permanente.`,
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#f43f5e',
                    cancelButtonColor: '#8b93a5',
                    confirmButtonText: 'Sí, eliminar',
                    cancelButtonText: 'Cancelar',
                    reverseButtons: true
                }).then((result) => {
                    if (result.isConfirmed) {
                        const form = document.createElement('form');
                        form.method = 'POST';
                        form.action = '/admin/exam-management/documents/eliminar/' + row.id;

                        const token = document.querySelector('input[name="__RequestVerificationToken"]');
                        if (token) form.appendChild(token.cloneNode(true));

                        document.body.appendChild(form);
                        form.submit();
                    }
                });
            }
        });
    });

    function showExtensions(name, extensions) {
        document.getElementById('extModalTitle').innerText = name;
        const content = document.getElementById('extModalContent');
        content.innerHTML = '';

        if (!extensions || extensions.trim() === '') {
            content.innerHTML = '<div class="w-full text-center py-8 text-ink-400 font-medium italic">No se han definido extensiones permitidas.</div>';
        } else {
            extensions.split(',').map(e => e.trim()).filter(e => e).forEach(ext => {
                const span = document.createElement('span');
                span.className = 'px-3 py-1.5 bg-ink-50 dark:bg-ink-800 ring-1 ring-ink-200 dark:ring-ink-700 text-ink-700 dark:text-ink-200 rounded-md text-[11px] font-semibold tracking-wide uppercase hover:ring-primary-300 hover:text-primary-600 transition-all';
                span.innerText = ext;
                content.appendChild(span);
            });
        }

        const modal = document.getElementById('extModal');
        modal.classList.remove('hidden');
        setTimeout(() => modal.classList.add('opacity-100'), 10);
        document.body.classList.add('overflow-hidden');
    }

    function closeModal() {
        const modal = document.getElementById('extModal');
        modal.classList.add('hidden');
        document.body.classList.remove('overflow-hidden');
    }

    document.addEventListener('keydown', e => { if (e.key === 'Escape') closeModal(); });
    document.getElementById('extModal').addEventListener('click', e => { if (e.target.id === 'extModal') closeModal(); });

    window.showExtensions = showExtensions;
    window.closeModal = closeModal;
})();
