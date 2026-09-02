(function () {
    const cfg = window.SchoolCreateConfig || {};
    const provincesUrl = cfg.provincesUrl || '';
    const districtsUrl = cfg.districtsUrl || '';

    function waitFor(id, cb, attempts) {
        attempts = attempts || 0;
        if (window.customSelectRegistry && window.customSelectRegistry[id]) cb();
        else if (attempts < 40) setTimeout(() => waitFor(id, cb, attempts + 1), 50);
    }

    waitFor('departmentSelect', () => {
        document.getElementById('departmentSelect').addEventListener('change', function () {
            const deptId = this.value;
            window.customSelectRegistry['provinceSelect']?.clear();
            window.customSelectRegistry['DistritId']?.clear();
            if (deptId) {
                window.customSelectRegistry['provinceSelect']?.load(provincesUrl + '/' + deptId);
            }
        });
    });

    waitFor('provinceSelect', () => {
        document.getElementById('provinceSelect').addEventListener('change', function () {
            const provId = this.value;
            window.customSelectRegistry['DistritId']?.clear();
            if (provId) {
                window.customSelectRegistry['DistritId']?.load(districtsUrl + '/' + provId);
            }
        });
    });
})();
