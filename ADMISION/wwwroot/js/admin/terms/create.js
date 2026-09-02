(function () {
    const master = document.getElementById('replicationEnabled');
    const panel = document.getElementById('replicationOptions');
    const items = () => panel.querySelectorAll('.replication-item input[type="checkbox"]');

    function syncVisibility() {
        if (master.checked) panel.classList.remove('hidden');
        else panel.classList.add('hidden');
    }

    master.addEventListener('change', syncVisibility);
    syncVisibility();

    document.getElementById('replicationAll').addEventListener('click', () => {
        items().forEach(c => c.checked = true);
    });
    document.getElementById('replicationNone').addEventListener('click', () => {
        items().forEach(c => c.checked = false);
    });
})();
