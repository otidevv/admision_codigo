(function () {
    let typingTimer;
    const doneTypingInterval = 400;

    $(document).ready(function () {
        $('#filterDepartment').on('change', function () {
            const deptId = $(this).val();

            if (window.customSelectRegistry['filterProvince']) {
                window.customSelectRegistry['filterProvince'].clear();
            }
            if (window.customSelectRegistry['filterDistrict']) {
                window.customSelectRegistry['filterDistrict'].clear();
            }

            if (deptId) {
                window.customSelectRegistry['filterProvince']?.load('/admin/colegios/GetProvinces/' + deptId);
            }

            refreshData();
        });

        $('#filterProvince').on('change', function () {
            const provId = $(this).val();

            if (window.customSelectRegistry['filterDistrict']) {
                window.customSelectRegistry['filterDistrict'].clear();
            }

            if (provId) {
                window.customSelectRegistry['filterDistrict']?.load('/admin/colegios/GetDistricts/' + provId);
            }

            refreshData();
        });

        $('#filterDistrict').on('change', refreshData);

        $('#filterName').on('input', function () {
            clearTimeout(typingTimer);
            typingTimer = setTimeout(refreshData, doneTypingInterval);
        });

        document.addEventListener('dt:action', function (e) {
            const { key, row } = e.detail;
            if (key === 'delete') {
                Swal.fire({
                    title: '¿Eliminar Institución Educativa?',
                    text: `Estás a punto de eliminar "${row.name}". Esta acción no se puede deshacer.`,
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#f43f5e',
                    cancelButtonColor: '#8b93a5',
                    confirmButtonText: 'Sí, eliminar',
                    cancelButtonText: 'Cancelar',
                    reverseButtons: true
                }).then((result) => {
                    if (result.isConfirmed) {
                        console.log('Eliminando:', row.id);
                    }
                });
            }
        });
    });

    function refreshData() {
        const params = {
            departmentId: $('#filterDepartment').val() || null,
            provinceId: $('#filterProvince').val() || null,
            districtId: $('#filterDistrict').val() || null,
            name: $('#filterName').val() || null
        };
        DT.filter('schoolsTable', params);
    }

    window.refreshData = refreshData;
})();
