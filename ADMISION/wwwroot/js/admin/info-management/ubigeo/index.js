function handleCsvSelect(input) {
    var icon = document.getElementById('csv-icon');
    var info = document.getElementById('csv-info');
    var placeholder = document.getElementById('csv-placeholder');
    var name = document.getElementById('csv-name');
    var size = document.getElementById('csv-size');
    if (input.files && input.files[0]) {
        var file = input.files[0];
        name.textContent = file.name;
        size.textContent = (file.size / 1024 / 1024).toFixed(2) + ' MB';
        info.classList.remove('hidden');
        if (placeholder) placeholder.classList.add('hidden');
        icon.classList.remove('text-ink-300', 'dark:text-ink-500');
        icon.classList.add('text-primary-500');
    }
}

// ── Gestión Manual ─────────────────────────────────────────────────────
var ManualUbigeo = (function () {
    var state = {
        countryId: '',
        deptId: '',
        provId: '',
        data: [],
        deleteType: '',
        deleteId: ''
    };

    function getToken() {
        var input = document.querySelector('[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function loadUbigeos(countryId) {
        if (!countryId) return;
        state.countryId = countryId;
        state.deptId = '';
        state.provId = '';
        state.data = [];

        document.getElementById('btnAddDept').disabled = true;
        document.getElementById('btnAddProv').disabled = true;
        document.getElementById('btnAddDist').disabled = true;

        fetch('/admin/info-management/ubigeo/GetUbigeos?countryId=' + encodeURIComponent(countryId))
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.error) { showError(data.error); return; }
                state.data = data;
                renderDepartments(data);
                renderProvinces([]);
                renderDistricts([]);
                document.getElementById('btnAddDept').disabled = false;
            })
            .catch(function () { showError('Error al cargar ubigeos'); });
    }

    function renderDepartments(data) {
        var list = document.getElementById('deptList');
        var count = document.getElementById('deptCount');
        if (!data || data.length === 0) {
            list.innerHTML = '<div class="p-6 text-center text-ink-400 text-sm"><i class="ti ti-building text-2xl mb-2 block"></i><p>No hay departamentos registrados</p></div>';
            count.classList.add('hidden');
            return;
        }
        count.classList.remove('hidden');
        count.textContent = data.length;
        list.innerHTML = '';
        data.forEach(function (d) {
            var active = state.deptId === d.id;
            var div = document.createElement('div');
            div.className = 'px-4 py-2.5 flex items-center justify-between gap-2 cursor-pointer transition-colors hover:bg-primary-50/40 dark:hover:bg-primary-500/5' +
                (active ? ' bg-primary-50 dark:bg-primary-500/10 ring-1 ring-inset ring-primary-200 dark:ring-primary-500/30' : '');
            div.dataset.id = d.id;
            div.innerHTML =
                '<div class="flex items-center gap-2 min-w-0 flex-1">' +
                    '<span class="w-7 h-7 rounded bg-primary-50 dark:bg-primary-500/10 text-primary-600 dark:text-primary-300 inline-flex items-center justify-center text-[10px] font-mono font-bold shrink-0">' + d.code + '</span>' +
                    '<span class="text-[13px] font-medium text-ink-800 dark:text-ink-200 truncate">' + escHtml(d.name) + '</span>' +
                '</div>' +
                '<div class="flex items-center gap-1 shrink-0">' +
                    '<button type="button" class="edit-dept" data-id="' + d.id + '" data-name="' + escAttr(d.name) + '" data-code="' + escAttr(d.code) + '" title="Editar"><i class="ti ti-edit text-[11px]"></i></button>' +
                    '<button type="button" class="delete-dept" data-id="' + d.id + '" data-name="' + escAttr(d.name) + '" title="Eliminar"><i class="ti ti-trash text-[11px]"></i></button>' +
                '</div>';
            div.addEventListener('click', function (e) {
                if (e.target.closest('button')) return;
                selectDepartment(d.id);
            });
            list.appendChild(div);
        });
    }

    function renderProvinces(items) {
        var list = document.getElementById('provList');
        var count = document.getElementById('provCount');
        if (!items || items.length === 0) {
            list.innerHTML = '<div class="p-6 text-center text-ink-400 text-sm"><i class="ti ti-building text-2xl mb-2 block"></i><p>' + (state.deptId ? 'No hay provincias registradas' : 'Seleccione un departamento') + '</p></div>';
            count.classList.add('hidden');
            return;
        }
        count.classList.remove('hidden');
        count.textContent = items.length;
        list.innerHTML = '';
        items.forEach(function (p) {
            var active = state.provId === p.id;
            var div = document.createElement('div');
            div.className = 'px-4 py-2.5 flex items-center justify-between gap-2 cursor-pointer transition-colors hover:bg-amber-50/40 dark:hover:bg-amber-500/5' +
                (active ? ' bg-amber-50 dark:bg-amber-500/10 ring-1 ring-inset ring-amber-200 dark:ring-amber-500/30' : '');
            div.dataset.id = p.id;
            div.innerHTML =
                '<div class="flex items-center gap-2 min-w-0 flex-1">' +
                    '<span class="w-7 h-7 rounded bg-amber-50 dark:bg-amber-500/10 text-amber-600 dark:text-amber-300 inline-flex items-center justify-center text-[10px] font-mono font-bold shrink-0">' + p.code + '</span>' +
                    '<span class="text-[13px] font-medium text-ink-800 dark:text-ink-200 truncate">' + escHtml(p.name) + '</span>' +
                '</div>' +
                '<div class="flex items-center gap-1 shrink-0">' +
                    '<button type="button" class="edit-prov" data-id="' + p.id + '" data-name="' + escAttr(p.name) + '" data-code="' + escAttr(p.code) + '" title="Editar"><i class="ti ti-edit text-[11px]"></i></button>' +
                    '<button type="button" class="delete-prov" data-id="' + p.id + '" data-name="' + escAttr(p.name) + '" title="Eliminar"><i class="ti ti-trash text-[11px]"></i></button>' +
                '</div>';
            div.addEventListener('click', function (e) {
                if (e.target.closest('button')) return;
                selectProvince(p.id);
            });
            list.appendChild(div);
        });
    }

    function renderDistricts(items) {
        var list = document.getElementById('distList');
        var count = document.getElementById('distCount');
        if (!items || items.length === 0) {
            list.innerHTML = '<div class="p-6 text-center text-ink-400 text-sm"><i class="ti ti-map-2 text-2xl mb-2 block"></i><p>' + (state.provId ? 'No hay distritos registrados' : 'Seleccione una provincia') + '</p></div>';
            count.classList.add('hidden');
            return;
        }
        count.classList.remove('hidden');
        count.textContent = items.length;
        list.innerHTML = '';
        items.forEach(function (d) {
            var div = document.createElement('div');
            div.className = 'px-4 py-2.5 flex items-center justify-between gap-2 transition-colors hover:bg-emerald-50/40 dark:hover:bg-emerald-500/5';
            div.innerHTML =
                '<div class="flex items-center gap-2 min-w-0 flex-1">' +
                    '<span class="w-7 h-7 rounded bg-emerald-50 dark:bg-emerald-500/10 text-emerald-600 dark:text-emerald-300 inline-flex items-center justify-center text-[10px] font-mono font-bold shrink-0">' + d.code + '</span>' +
                    '<span class="text-[13px] font-medium text-ink-800 dark:text-ink-200 truncate">' + escHtml(d.name) + '</span>' +
                '</div>' +
                '<div class="flex items-center gap-1 shrink-0">' +
                    '<button type="button" class="edit-dist" data-id="' + d.id + '" data-name="' + escAttr(d.name) + '" data-code="' + escAttr(d.code) + '" title="Editar"><i class="ti ti-edit text-[11px]"></i></button>' +
                    '<button type="button" class="delete-dist" data-id="' + d.id + '" data-name="' + escAttr(d.name) + '" title="Eliminar"><i class="ti ti-trash text-[11px]"></i></button>' +
                '</div>';
            list.appendChild(div);
        });
    }

    function selectDepartment(deptId) {
        state.deptId = deptId;
        state.provId = '';
        renderDepartments(state.data);
        var dept = state.data.find(function (d) { return d.id === deptId; });
        renderProvinces(dept ? dept.provinces : []);
        renderDistricts([]);
        document.getElementById('btnAddProv').disabled = false;
        document.getElementById('btnAddDist').disabled = true;
    }

    function selectProvince(provId) {
        state.provId = provId;
        renderProvinces(getCurrentProvinces());
        var dept = state.data.find(function (d) { return d.id === state.deptId; });
        var prov = dept ? dept.provinces.find(function (p) { return p.id === provId; }) : null;
        renderDistricts(prov ? prov.districts : []);
        document.getElementById('btnAddDist').disabled = false;
    }

    function getCurrentProvinces() {
        var dept = state.data.find(function (d) { return d.id === state.deptId; });
        return dept ? dept.provinces : [];
    }

    function openDeptModal(editData) {
        document.getElementById('deptModal-title').textContent = editData ? 'Editar departamento' : 'Nuevo departamento';
        document.getElementById('deptForm').reset();
        document.getElementById('deptId').value = editData ? editData.id : '';
        document.getElementById('deptName').value = editData ? editData.name : '';
        document.getElementById('deptCode').value = editData ? editData.code : '';
        ADM.Modal.open('deptModal');
    }

    function openProvModal(editData) {
        document.getElementById('provModal-title').textContent = editData ? 'Editar provincia' : 'Nueva provincia';
        document.getElementById('provForm').reset();
        document.getElementById('provId').value = editData ? editData.id : '';
        document.getElementById('provDeptId').value = state.deptId;
        document.getElementById('provName').value = editData ? editData.name : '';
        document.getElementById('provCode').value = editData ? editData.code : '';
        ADM.Modal.open('provModal');
    }

    function openDistModal(editData) {
        document.getElementById('distModal-title').textContent = editData ? 'Editar distrito' : 'Nuevo distrito';
        document.getElementById('distForm').reset();
        document.getElementById('distId').value = editData ? editData.id : '';
        document.getElementById('distProvId').value = state.provId;
        document.getElementById('distName').value = editData ? editData.name : '';
        document.getElementById('distCode').value = editData ? editData.code : '';
        ADM.Modal.open('distModal');
    }

    function saveDept() {
        var id = document.getElementById('deptId').value;
        var name = document.getElementById('deptName').value.trim();
        var code = document.getElementById('deptCode').value.trim();
        if (!name || !code) { showError('Complete todos los campos'); return; }
        if (code.length === 1) code = '0' + code;
        if (!/^\d{2}$/.test(code)) { showError('El código debe tener 2 dígitos'); return; }

        var dup = state.data.some(function (d) { return d.code === code && d.id !== id; });
        if (dup) { showError('Ya existe un departamento con el código ' + code); return; }

        var url = id ? '/admin/info-management/ubigeo/Department/Update/' + id : '/admin/info-management/ubigeo/Department/Create';
        var method = id ? 'PUT' : 'POST';
        var body = id ? JSON.stringify({ name: name, code: code }) : JSON.stringify({ name: name, code: code, parentId: state.countryId });

        ajax(url, method, body, function (res) {
            if (!res.success) { showError(res.error || 'Error al guardar'); return; }
            ADM.Modal.close('deptModal');
            loadUbigeos(state.countryId);
        });
    }

    function saveProv() {
        var id = document.getElementById('provId').value;
        var name = document.getElementById('provName').value.trim();
        var code = document.getElementById('provCode').value.trim();
        if (!name || !code) { showError('Complete todos los campos'); return; }
        while (code.length < 4) code = '0' + code;
        if (!/^\d{4}$/.test(code)) { showError('El código debe tener 4 dígitos'); return; }

        var provs = getCurrentProvinces();
        var dup = provs.some(function (p) { return p.code === code && p.id !== id; });
        if (dup) { showError('Ya existe una provincia con el código ' + code); return; }

        var url = id ? '/admin/info-management/ubigeo/Province/Update/' + id : '/admin/info-management/ubigeo/Province/Create';
        var method = id ? 'PUT' : 'POST';
        var body = id ? JSON.stringify({ name: name, code: code }) : JSON.stringify({ name: name, code: code, parentId: document.getElementById('provDeptId').value });

        ajax(url, method, body, function (res) {
            if (!res.success) { showError(res.error || 'Error al guardar'); return; }
            ADM.Modal.close('provModal');
            loadUbigeos(state.countryId);
        });
    }

    function saveDist() {
        var id = document.getElementById('distId').value;
        var name = document.getElementById('distName').value.trim();
        var code = document.getElementById('distCode').value.trim();
        if (!name || !code) { showError('Complete todos los campos'); return; }
        while (code.length < 6) code = '0' + code;
        if (!/^\d{6}$/.test(code)) { showError('El código debe tener 6 dígitos'); return; }

        var dept = state.data.find(function (d) { return d.id === state.deptId; });
        var prov = dept ? dept.provinces.find(function (p) { return p.id === state.provId; }) : null;
        var dists = prov ? prov.districts : [];
        var dup = dists.some(function (d) { return d.code === code && d.id !== id; });
        if (dup) { showError('Ya existe un distrito con el código ' + code); return; }

        var url = id ? '/admin/info-management/ubigeo/District/Update/' + id : '/admin/info-management/ubigeo/District/Create';
        var method = id ? 'PUT' : 'POST';
        var body = id ? JSON.stringify({ name: name, code: code }) : JSON.stringify({ name: name, code: code, parentId: document.getElementById('distProvId').value });

        ajax(url, method, body, function (res) {
            if (!res.success) { showError(res.error || 'Error al guardar'); return; }
            ADM.Modal.close('distModal');
            loadUbigeos(state.countryId);
        });
    }

    function confirmDelete(type, id, name) {
        state.deleteType = type;
        state.deleteId = id;
        var subtitles = { dept: 'Se eliminará el departamento y todas sus provincias y distritos.', prov: 'Se eliminará la provincia y todos sus distritos.', dist: 'Se eliminará el distrito.' };
        document.querySelector('#deleteUbigeoModal .adm-modal__subtitle').textContent = subtitles[type] || '';
        document.querySelector('#deleteUbigeoModal .adm-modal__title').textContent = '¿Eliminar ' + name + '?';
        ADM.Modal.open('deleteUbigeoModal');
    }

    function executeDelete() {
        var routes = { dept: '/admin/info-management/ubigeo/Department/Delete/', prov: '/admin/info-management/ubigeo/Province/Delete/', dist: '/admin/info-management/ubigeo/District/Delete/' };
        var url = (routes[state.deleteType] || '') + state.deleteId;

        ajax(url, 'DELETE', null, function (res) {
            if (!res.success) { showError(res.error || 'Error al eliminar'); return; }
            ADM.Modal.close('deleteUbigeoModal');
            loadUbigeos(state.countryId);
        });
    }

    // ── Helpers ──────────────────────────────────────────────────────
    function ajax(url, method, body, cb) {
        var headers = { 'RequestVerificationToken': getToken() };
        var opts = { method: method, headers: headers };
        if (body) { headers['Content-Type'] = 'application/json'; opts.body = body; }
        fetch(url, opts)
            .then(function (r) { return r.json(); })
            .then(function (res) { cb(res); })
            .catch(function () { showError('Error de conexión'); });
    }

    function showError(msg) {
        if (typeof Swal !== 'undefined') {
            Swal.fire({ icon: 'error', title: 'Error', text: msg, confirmButtonColor: '#f54477', confirmButtonText: 'Aceptar' });
        } else { alert(msg); }
    }

    function escHtml(str) {
        var d = document.createElement('div');
        d.appendChild(document.createTextNode(str));
        return d.innerHTML;
    }

    function escAttr(str) {
        return String(str).replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    // ── Init events (delegated on document) ──────────────────────────
    function init() {
        document.addEventListener('click', function (e) {
            var btn = e.target.closest('button');
            if (!btn) return;

            if (btn.classList.contains('edit-dept')) { e.preventDefault(); openDeptModal({ id: btn.dataset.id, name: btn.dataset.name, code: btn.dataset.code }); }
            else if (btn.classList.contains('delete-dept')) { e.preventDefault(); confirmDelete('dept', btn.dataset.id, btn.dataset.name); }
            else if (btn.classList.contains('edit-prov')) { e.preventDefault(); openProvModal({ id: btn.dataset.id, name: btn.dataset.name, code: btn.dataset.code }); }
            else if (btn.classList.contains('delete-prov')) { e.preventDefault(); confirmDelete('prov', btn.dataset.id, btn.dataset.name); }
            else if (btn.classList.contains('edit-dist')) { e.preventDefault(); openDistModal({ id: btn.dataset.id, name: btn.dataset.name, code: btn.dataset.code }); }
            else if (btn.classList.contains('delete-dist')) { e.preventDefault(); confirmDelete('dist', btn.dataset.id, btn.dataset.name); }
        });

        document.getElementById('confirmDeleteUbigeo').addEventListener('click', executeDelete);
        document.getElementById('deptForm').addEventListener('submit', function (e) { e.preventDefault(); saveDept(); });
        document.getElementById('provForm').addEventListener('submit', function (e) { e.preventDefault(); saveProv(); });
        document.getElementById('distForm').addEventListener('submit', function (e) { e.preventDefault(); saveDist(); });
    }

    return { init: init, loadUbigeos: loadUbigeos };
})();

// ── Page Init ───────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    var countries = window.UbigeoCountries || [];

    function loadStaticToCustom(id, data) {
        var list = document.getElementById('options_' + id);
        if (!list) return;
        list.innerHTML = '';
        data.forEach(function (item) {
            var li = document.createElement('li');
            li.className = 'px-4 py-3 select-option transition-all';
            li.textContent = item.name;
            li.dataset.value = item.id;
            li.onclick = function () { window.customSelectRegistry[id].setValue(item.id, item.name); };
            list.appendChild(li);
        });
    }

    setTimeout(function () {
        var mapped = countries.map(function (c) { return { id: c.id, name: c.name }; });

        if (window.customSelectRegistry['CountryId']) {
            loadStaticToCustom('CountryId', mapped);
            var peru = countries.find(function (c) { return c.name.toUpperCase().includes('PERÚ') || c.name.toUpperCase().includes('PERU'); });
            if (peru) window.customSelectRegistry['CountryId'].setValue(peru.id, peru.name);
        }

        if (window.customSelectRegistry['UbigeoCountryId']) {
            loadStaticToCustom('UbigeoCountryId', mapped);
        }
    }, 500);

    // Manual management
    ManualUbigeo.init();

    // Listen for country changes on the manual selector
    setTimeout(function () {
        var ubigeoCountrySel = document.getElementById('UbigeoCountryId');
        if (ubigeoCountrySel) {
            ubigeoCountrySel.addEventListener('change', function () {
                if (this.value) ManualUbigeo.loadUbigeos(this.value);
            });
        }
    }, 600);
});

window.handleCsvSelect = handleCsvSelect;