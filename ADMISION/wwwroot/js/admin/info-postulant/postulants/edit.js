$(document).ready(function () {
    const cfg = window.PostulantEditConfig || {};

    if (cfg.state) window.customSelectRegistry['State'].setValue(cfg.state, cfg.state);
    if (cfg.careerId) window.customSelectRegistry['CareerId'].setValue(cfg.careerId, cfg.careerLabel || '');
    if (cfg.modalityId) window.customSelectRegistry['ModalityId'].setValue(cfg.modalityId, cfg.modalityLabel || '');

    const $country = $('#ubCountry');
    const $dep = $('#ubDepartment');
    const $prov = $('#ubProvince');
    const $dist = $('#ubDistrict');

    function fill($el, items, placeholder) {
        $el.empty().append(`<option value="">${placeholder}</option>`);
        (items || []).forEach(i => $el.append(`<option value="${i.id}">${i.name}</option>`));
    }

    $country.on('change', function () {
        const v = $(this).val();
        fill($dep, [], '-- Seleccione departamento --');
        fill($prov, [], '-- Seleccione provincia --');
        fill($dist, [], '-- Seleccione distrito --');
        if (!v) return;
        $.getJSON(`/admin/info-postulant/list/ubigeo/departments/${v}`, data => fill($dep, data, '-- Seleccione departamento --'));
    });
    $dep.on('change', function () {
        const v = $(this).val();
        fill($prov, [], '-- Seleccione provincia --');
        fill($dist, [], '-- Seleccione distrito --');
        if (!v) return;
        $.getJSON(`/admin/info-postulant/list/ubigeo/provinces/${v}`, data => fill($prov, data, '-- Seleccione provincia --'));
    });
    $prov.on('change', function () {
        const v = $(this).val();
        fill($dist, [], '-- Seleccione distrito --');
        if (!v) return;
        $.getJSON(`/admin/info-postulant/list/ubigeo/districts/${v}`, data => fill($dist, data, '-- Seleccione distrito --'));
    });
});
