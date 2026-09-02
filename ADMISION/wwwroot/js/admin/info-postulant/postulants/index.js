(function () {
    $(document).ready(function () {
        // Al acceder, el periodo activo (o último registrado) ya viene preseleccionado
        // y la grilla carga filtrada por él. Solo se cargan las cascadas de ese periodo.
        const initialTerm = $('#filterTerm').val();
        if (initialTerm) {
            window.customSelectRegistry['filterModality']?.load('/admin/info-postulant/list/GetModalitiesByTerm/' + initialTerm);
            window.customSelectRegistry['filterArea']?.load('/admin/info-postulant/list/GetAreasByTerm/' + initialTerm);
        }

        $('#filterFaculty').change(function () {
            const facultyId = $(this).val();
            const careerSelect = window.customSelectRegistry['filterCareer'];

            if (facultyId) {
                careerSelect.clear();
                careerSelect.load('/admin/info-postulant/list/GetCareersByFaculty/' + facultyId);
            } else {
                careerSelect.clear();
            }
            refreshData();
        });

        $('#filterTerm').change(function () {
            const termId = $(this).val();
            const modalitySelect = window.customSelectRegistry['filterModality'];
            const areaSelect = window.customSelectRegistry['filterArea'];

            if (termId) {
                modalitySelect.clear();
                areaSelect.clear();
                modalitySelect.load('/admin/info-postulant/list/GetModalitiesByTerm/' + termId);
                areaSelect.load('/admin/info-postulant/list/GetAreasByTerm/' + termId);
            } else {
                modalitySelect.clear();
                areaSelect.clear();
            }
            refreshData();
        });

        $('#filterArea, #filterModality, #filterCareer, #filterState').change(refreshData);

        let searchTimeout;
        $('#filterSearch').on('input', function () {
            const val = $(this).val();
            clearTimeout(searchTimeout);

            if (val.length >= 3 || val.length === 0) {
                searchTimeout = setTimeout(refreshData, 500);
            }
        });

        document.addEventListener('dt:action', function (e) {
            const { key, row } = e.detail;
            if (key === 'view') {
                console.log('Ver expediente:', row.postulantId);
            }
        });
    });

    function refreshData() {
        const params = {
            areaId: $('#filterArea').val(),
            termId: $('#filterTerm').val(),
            facultyId: $('#filterFaculty').val(),
            careerId: $('#filterCareer').val(),
            modalityId: $('#filterModality').val(),
            state: $('#filterState').val(),
            search: $('#filterSearch').val()
        };
        DT.filter('postulantsTable', params);
    }

    window.refreshData = refreshData;
})();
