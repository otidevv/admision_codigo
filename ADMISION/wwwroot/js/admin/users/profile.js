(function () {
    const cfg = window.UserProfileConfig || {};
    const monthLabels = cfg.monthLabels || [];
    const monthData = cfg.monthData || [];
    const dayData = cfg.dayData || [];
    const daysInMonth = cfg.daysInMonth || 0;
    const dayLabels = Array.from({ length: daysInMonth }, function (_, i) { return (i + 1).toString(); });

    const baseOpts = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: { display: false },
            tooltip: { backgroundColor: '#1e293b', titleColor: '#fff', bodyColor: '#fff' }
        },
        scales: {
            y: { beginAtZero: true, ticks: { precision: 0, color: '#94a3b8', font: { size: 10 } }, grid: { color: '#f1f5f9' } },
            x: { ticks: { color: '#94a3b8', font: { size: 10 } }, grid: { display: false } }
        }
    };

    new Chart(document.getElementById('chartMonth'), {
        type: 'bar',
        data: {
            labels: monthLabels,
            datasets: [{
                label: 'Logins',
                data: monthData,
                backgroundColor: 'rgba(245, 68, 119, 0.75)',
                borderColor: '#f54477',
                borderWidth: 1,
                borderRadius: 6
            }]
        },
        options: baseOpts
    });

    new Chart(document.getElementById('chartDay'), {
        type: 'bar',
        data: {
            labels: dayLabels,
            datasets: [{
                label: 'Logins',
                data: dayData,
                backgroundColor: 'rgba(113, 106, 202, 0.7)',
                borderColor: '#716aca',
                borderWidth: 1,
                borderRadius: 4
            }]
        },
        options: baseOpts
    });

    document.querySelectorAll('.tab-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            const target = btn.dataset.tab;
            document.querySelectorAll('.tab-panel').forEach(p => p.classList.add('hidden'));
            document.getElementById('tab-' + target)?.classList.remove('hidden');
        });
    });
})();
