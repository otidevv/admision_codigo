document.getElementById('formExternal')?.addEventListener('submit', () => {
    const btn = document.getElementById('btnProcess');
    if (btn) {
        btn.disabled = true;
        btn.innerHTML = '<i class="ti ti-loader-2 fa-spin"></i> Procesando...';
    }
});
