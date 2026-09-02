(function () {
    'use strict';

    var token = function () {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    };

    // ════════════════════════════════════════════════════════════════
    //  EDIT MODE TOGGLES
    // ════════════════════════════════════════════════════════════════
    document.querySelectorAll('.js-toggle-edit').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var section = btn.dataset.toggleEdit;
            var view = document.getElementById(section + '-view');
            var form = document.getElementById(section + '-form');
            if (view) view.classList.add('hidden');
            if (form) form.classList.remove('hidden');
        });
    });

    document.querySelectorAll('[data-cancel-edit]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var section = btn.dataset.cancelEdit;
            var view = document.getElementById(section + '-view');
            var form = document.getElementById(section + '-form');
            if (form) form.classList.add('hidden');
            if (view) view.classList.remove('hidden');
        });
    });

    // ════════════════════════════════════════════════════════════════
    //  PERSONAL DATA FORM (AJAX POST)
    // ════════════════════════════════════════════════════════════════
    var postulantId = document.querySelector('meta[name="postulant-id"]')?.content
        || window.location.pathname.match(/postulant-resum\/([a-f0-9-]+)/)?.[1];
    var inscriptionId = document.querySelector('meta[name="inscription-id"]')?.content
        || window.location.pathname.match(/inscriptions\/([a-f0-9-]+)\/validate/)?.[1];

    var personalesForm = document.getElementById('personales-form');
    if (personalesForm) {
        personalesForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            var fd = new FormData(personalesForm);
            var submitBtn = personalesForm.querySelector('button[type="submit"]');

            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<i class="ti ti-loader-2 animate-spin text-xs"></i> Guardando...';
            }

            try {
                var r = await fetch('/admin/info-postulant/postulant/postulant-resum/' + postulantId + '/edit-personal-data', {
                    method: 'POST',
                    body: fd,
                    headers: { 'X-Requested-With': 'XMLHttpRequest', 'RequestVerificationToken': token() }
                });
                var result = await r.json();
                if (r.ok && result.success) {
                    Swal.fire({ icon: 'success', title: 'Guardado', text: result.message || 'Datos personales actualizados.', timer: 2500, showConfirmButton: false });
                    setTimeout(function () { location.reload(); }, 1500);
                } else {
                    Swal.fire({ icon: 'error', title: 'Error', text: result.message || 'No se pudieron guardar los datos.', confirmButtonText: 'Entendido' });
                }
            } catch (err) {
                Swal.fire({ icon: 'error', title: 'Error de conexion', text: 'Verifica tu conexion e intenta nuevamente.', confirmButtonText: 'Entendido' });
            } finally {
                if (submitBtn) {
                    submitBtn.disabled = false;
                    submitBtn.innerHTML = '<i class="ti ti-device-floppy text-xs"></i> Guardar';
                }
            }
        });
    }

    // ════════════════════════════════════════════════════════════════
    //  INSCRIPTION DATA FORM (AJAX POST)
    // ════════════════════════════════════════════════════════════════
    var postulacionForm = document.getElementById('postulacion-form');
    if (postulacionForm) {
        postulacionForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            var fd = new FormData(postulacionForm);
            var submitBtn = postulacionForm.querySelector('button[type="submit"]');

            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<i class="ti ti-loader-2 animate-spin text-xs"></i> Guardando...';
            }

            try {
                var r = await fetch('/admin/info-postulant/postulant/postulant-resum/' + postulantId + '/inscriptions/' + inscriptionId + '/edit', {
                    method: 'POST',
                    body: fd,
                    headers: { 'X-Requested-With': 'XMLHttpRequest', 'RequestVerificationToken': token() }
                });
                var result = await r.json();
                if (r.ok && result.success) {
                    Swal.fire({ icon: 'success', title: 'Guardado', text: result.message || 'Datos de inscripcion actualizados.', timer: 2500, showConfirmButton: false });
                    setTimeout(function () { location.reload(); }, 1500);
                } else {
                    var errorMsg = result.message || 'Ocurrio un error al guardar.';
                    if (result.errors) {
                        var detail = Object.entries(result.errors).map(function (e) { return '• ' + e[0] + ': ' + e[1].join(', '); }).join('\n');
                        errorMsg += '\n\n' + detail;
                    }
                    Swal.fire({ icon: 'error', title: 'Error al guardar', text: errorMsg, confirmButtonText: 'Entendido' });
                }
            } catch (err) {
                Swal.fire({ icon: 'error', title: 'Error de conexion', text: 'Verifica tu conexion e intenta nuevamente.', confirmButtonText: 'Entendido' });
            } finally {
                if (submitBtn) {
                    submitBtn.disabled = false;
                    submitBtn.innerHTML = '<i class="ti ti-device-floppy text-xs"></i> Guardar';
                }
            }
        });
    }

    // ════════════════════════════════════════════════════════════════
    //  CASCADING SELECTS — MODALITY → CAREER + TYPE MODALITY
    // ════════════════════════════════════════════════════════════════
    var modalitySel = document.getElementById('ModalityId');
    var typeModSel = document.getElementById('TypeModalityId');
    var careerSel = document.getElementById('CareerId');

    if (modalitySel) {
        modalitySel.addEventListener('change', async function () {
            var v = modalitySel.value;
            if (typeModSel) typeModSel.innerHTML = '<option value="">— Sin tipo especifico —</option>';

            if (careerSel) {
                careerSel.innerHTML = '<option value="">Cargando carreras...</option>';
                if (v) {
                    try {
                        var r = await fetch('/admin/info-postulant/postulant/lookups/careers-by-modality/' + v, { headers: { Accept: 'application/json' } });
                        if (r.ok) {
                            var data = await r.json();
                            careerSel.innerHTML = '<option value="">— Seleccione carrera —</option>';
                            (data || []).forEach(function (c) {
                                var o = document.createElement('option');
                                o.value = c.id; o.textContent = c.name;
                                careerSel.appendChild(o);
                            });
                        }
                    } catch (e) { console.error(e); }
                } else {
                    careerSel.innerHTML = '<option value="">— Seleccione carrera —</option>';
                }
            }

            if (!v) return;
            try {
                var r = await fetch('/admin/info-postulant/postulant/lookups/type-modalities/' + v, { headers: { Accept: 'application/json' } });
                if (!r.ok) return;
                var data = await r.json();
                (data || []).forEach(function (t) {
                    var o = document.createElement('option');
                    o.value = t.id; o.textContent = t.name;
                    typeModSel.appendChild(o);
                });
            } catch (e) { console.error(e); }
        });
    }

    // ════════════════════════════════════════════════════════════════
    //  CASCADING SELECTS — UBIGEO (Residencia)
    // ════════════════════════════════════════════════════════════════
    var ubCountry = document.getElementById('ubCountry');
    var ubDep = document.getElementById('ubDepartment');
    var ubProv = document.getElementById('ubProvince');
    var ubDist = document.getElementById('ubDistrict');

    function fill(sel, items, placeholder) {
        if (!sel) return;
        sel.innerHTML = '<option value="">' + placeholder + '</option>';
        (items || []).forEach(function (i) {
            var o = document.createElement('option'); o.value = i.id; o.textContent = i.name;
            sel.appendChild(o);
        });
    }

    if (ubCountry) {
        ubCountry.addEventListener('change', async function () {
            fill(ubDep, [], '— Seleccione departamento —');
            fill(ubProv, [], '— Seleccione provincia —');
            fill(ubDist, [], '— Seleccione distrito —');
            if (!ubCountry.value) return;
            var r = await fetch('/admin/info-postulant/list/ubigeo/departments/' + ubCountry.value);
            fill(ubDep, await r.json(), '— Seleccione departamento —');
        });
    }
    if (ubDep) {
        ubDep.addEventListener('change', async function () {
            fill(ubProv, [], '— Seleccione provincia —');
            fill(ubDist, [], '— Seleccione distrito —');
            if (!ubDep.value) return;
            var r = await fetch('/admin/info-postulant/list/ubigeo/provinces/' + ubDep.value);
            fill(ubProv, await r.json(), '— Seleccione provincia —');
        });
    }
    if (ubProv) {
        ubProv.addEventListener('change', async function () {
            fill(ubDist, [], '— Seleccione distrito —');
            if (!ubProv.value) return;
            var r = await fetch('/admin/info-postulant/list/ubigeo/districts/' + ubProv.value);
            fill(ubDist, await r.json(), '— Seleccione distrito —');
        });
    }

    // ════════════════════════════════════════════════════════════════
    //  CASCADING SELECTS — COLEGIO (School)
    // ════════════════════════════════════════════════════════════════
    var schDep = document.getElementById('schDepartment');
    var schProv = document.getElementById('schProvince');
    var schDist = document.getElementById('schDistrict');
    var schoolSel = document.getElementById('SchoolId');
    var otherSch = document.getElementById('OtherSchool');
    var schoolTypeSel = document.getElementById('SchoolType');
    var eduLevelSel = document.getElementById('EducationalLevel');
    var gradeSel = document.getElementById('Grade');
    var schoolCache = {};
    var originalSchoolId = schoolSel?.value || '';

    async function loadSchools(districtId) {
        if (!schoolSel) return;
        schoolSel.innerHTML = '<option value="">— Selecciona un colegio —</option>';
        schoolCache = {};
        if (!districtId) {
            if (schoolSel.firstElementChild) schoolSel.firstElementChild.textContent = '— Selecciona primero el distrito —';
            return;
        }
        try {
            var r = await fetch('/admin/info-postulant/postulant/lookups/schools/' + districtId);
            var data = await r.json();
            (data || []).forEach(function (s) {
                var o = document.createElement('option');
                o.value = s.id; o.textContent = s.name;
                schoolSel.appendChild(o);
                schoolCache[s.id] = { management: s.management || '', level: s.level || '' };
            });
            if (data.length === 0 && schoolSel.firstElementChild) {
                schoolSel.firstElementChild.textContent = 'No hay colegios registrados en este distrito';
            }
            if (originalSchoolId && schoolSel.querySelector('option[value="' + originalSchoolId + '"]')) {
                schoolSel.value = originalSchoolId;
            }
        } catch (e) { console.error('schools load', e); }
    }

    function mapManagement(val) {
        if (!val) return '';
        var v = val.trim().toLowerCase();
        if (v === 'publico' || v === 'publica') return 'Publico';
        if (v === 'privado') return 'Privado';
        return '';
    }

    function mapLevel(val) {
        if (!val) return '';
        var v = val.trim().toLowerCase();
        if (v === 'primaria') return 'Primaria';
        if (v === 'secundaria') return 'Secundaria';
        return '';
    }

    function applyGradeRange(level) {
        if (!gradeSel) return;
        var prev = gradeSel.value;
        gradeSel.innerHTML = '<option value="">— Sin especificar —</option>';
        var range = level === 'Primaria' ? [1,2,3,4,5,6] : level === 'Secundaria' ? [1,2,3,4,5] : [];
        range.forEach(function (g) {
            var o = document.createElement('option');
            o.value = g.toString(); o.textContent = g + '°';
            gradeSel.appendChild(o);
        });
        if (range.indexOf(parseInt(prev)) >= 0) gradeSel.value = prev;
        else gradeSel.value = '';
    }

    if (schDep) {
        schDep.addEventListener('change', async function () {
            fill(schProv, [], '— Seleccione provincia —');
            fill(schDist, [], '— Seleccione distrito —');
            await loadSchools(null);
            if (!schDep.value) return;
            var r = await fetch('/admin/info-postulant/postulant/lookups/ubigeo/provinces/' + schDep.value);
            fill(schProv, await r.json(), '— Seleccione provincia —');
        });
    }
    if (schProv) {
        schProv.addEventListener('change', async function () {
            fill(schDist, [], '— Seleccione distrito —');
            await loadSchools(null);
            if (!schProv.value) return;
            var r = await fetch('/admin/info-postulant/postulant/lookups/ubigeo/districts/' + schProv.value);
            fill(schDist, await r.json(), '— Seleccione distrito —');
        });
    }
    if (schDist) {
        schDist.addEventListener('change', async function () {
            await loadSchools(schDist.value);
        });
    }

    // Bootstrap schools on load
    if (schDist && schDist.value) loadSchools(schDist.value);

    if (otherSch) {
        otherSch.addEventListener('input', function () {
            if (otherSch.value.trim().length > 0 && schoolSel) schoolSel.value = '';
        });
    }
    if (schoolSel) {
        schoolSel.addEventListener('change', function () {
            if (schoolSel.value && otherSch) otherSch.value = '';
            var info = schoolCache[schoolSel.value];
            if (info) {
                if (schoolTypeSel) schoolTypeSel.value = mapManagement(info.management);
                if (eduLevelSel) { eduLevelSel.value = mapLevel(info.level); applyGradeRange(eduLevelSel.value); }
            }
        });
    }
    if (eduLevelSel) {
        eduLevelSel.addEventListener('change', function () { applyGradeRange(this.value); });
    }

    // ════════════════════════════════════════════════════════════════
    //  FILE VALIDATION — TOGGLE, NOTES, REPLACE
    // ════════════════════════════════════════════════════════════════
    async function postValidation(url, isValidated, note) {
        try {
            var r = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token() },
                body: JSON.stringify({ isValidated: isValidated, note: note })
            });
            if (!r.ok) throw new Error('HTTP ' + r.status);
            return await r.json();
        } catch (e) {
            console.error('[validation] toggle error', e);
            Swal.fire({ icon: 'error', title: 'No se pudo guardar', text: 'Revisa la conexion e intenta nuevamente.' });
            return null;
        }
    }

    // Toggle validacion
    document.querySelectorAll('.js-validate-toggle').forEach(function (chk) {
        chk.addEventListener('change', async function () {
            var row = chk.closest('tr');
            var url = row.dataset.toggleUrl;
            var note = row.querySelector('.js-add-note')?.dataset.existingNote || null;
            var result = await postValidation(url, chk.checked, note);
            if (result?.success) {
                row.dataset.validated = chk.checked ? 'true' : 'false';
                if (window.Toastify) {
                    Toastify({
                        text: chk.checked ? 'Archivo marcado como validado.' : 'Validacion revertida.',
                        duration: 2000, gravity: 'top', position: 'right',
                        style: { background: chk.checked ? '#10b981' : '#6b7280' }
                    }).showToast();
                }
                if (result.stateChanged) {
                    var msg = result.newState === 'Aprobado'
                        ? 'Todos los archivos validados — inscripcion marcada como APROBADA.'
                        : 'Inscripcion volvio a estado "' + result.newState + '" (faltan archivos por validar).';
                    Swal.fire({
                        icon: result.newState === 'Aprobado' ? 'success' : 'info',
                        title: 'Estado actualizado',
                        text: msg,
                        timer: 2500,
                        showConfirmButton: false
                    }).then(function () { location.reload(); });
                }
            } else {
                chk.checked = !chk.checked;
            }
        });
    });

    // Observaciones
    document.querySelectorAll('.js-add-note').forEach(function (btn) {
        btn.addEventListener('click', async function () {
            var row = btn.closest('tr');
            var url = row.dataset.toggleUrl;
            var isValidated = row.dataset.validated === 'true';
            var existing = btn.dataset.existingNote || '';
            var resultSwal = await Swal.fire({
                title: 'Observacion del archivo',
                input: 'textarea',
                inputLabel: 'Motivo de rechazo o nota interna',
                inputValue: existing,
                inputPlaceholder: 'Ej: documento ilegible, pertenece a otra modalidad...',
                showCancelButton: true,
                confirmButtonText: 'Guardar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#f54477',
                cancelButtonColor: '#6b7280',
                reverseButtons: true
            });
            if (!resultSwal.isConfirmed) return;
            var result = await postValidation(url, isValidated, resultSwal.value || null);
            if (result?.success) {
                btn.dataset.existingNote = resultSwal.value || '';
                location.reload();
            }
        });
    });

    // ════════════════════════════════════════════════════════════════
    //  EDIT PAYMENT — CONSOLIDATED MODAL
    // ════════════════════════════════════════════════════════════════
    var editModal = document.getElementById('editPaymentModal');
    var editOpCode = document.getElementById('editPaymentOpCode');
    var editFileInput = document.getElementById('editPaymentFileInput');
    var editFilePreview = document.getElementById('editPaymentFilePreview');
    var editFileName = document.getElementById('editPaymentFileName');
    var editFileRemove = document.getElementById('editPaymentFileRemove');
    var editFileLabel = document.getElementById('editPaymentFileLabel');
    var editFileInfo = document.getElementById('editPaymentFileInfo');
    var editAssocBadge = document.getElementById('editPaymentAssocBadge');
    var editAssocEmpty = document.getElementById('editPaymentAssocEmpty');
    var editAssocCurrent = document.getElementById('editPaymentAssocCurrent');
    var editAssocSerial = document.getElementById('editPaymentAssocSerial');
    var editAssocName = document.getElementById('editPaymentAssocName');
    var editAssocSearch = document.getElementById('editPaymentAssocSearch');
    var editAssocLoading = document.getElementById('editPaymentAssocLoading');
    var editAssocEmptyList = document.getElementById('editPaymentAssocEmptyList');
    var editAssocList = document.getElementById('editPaymentAssocList');
    var editSearchBtn = document.getElementById('editPaymentSearchExternal');
    var editDisassociateBtn = document.getElementById('editPaymentDisassociate');
    var editCancelSearch = document.getElementById('editPaymentCancelSearch');
    var editConfirmAssoc = document.getElementById('editPaymentConfirmAssoc');
    var editSaveBtn = document.getElementById('editPaymentSaveBtn');
    var editTitle = document.getElementById('editPaymentTitle');
    var editSubtitle = document.getElementById('editPaymentSubtitle');

    var editState = { paymentId: null, editUrl: null, selectedVoucherId: null, currentHasExternal: false, disassociate: false };

    function resetEditModal() {
        editState.paymentId = null;
        editState.editUrl = null;
        editState.selectedVoucherId = null;
        editState.currentHasExternal = false;
        editState.disassociate = false;
        if (editOpCode) editOpCode.value = '';
        if (editFileInput) editFileInput.value = '';
        if (editFilePreview) editFilePreview.classList.add('hidden');
        if (editFileLabel) editFileLabel.textContent = 'Seleccionar archivo (opcional)';
        if (editFileInfo) editFileInfo.textContent = '';
        if (editAssocBadge) { editAssocBadge.textContent = 'Sin asociar'; editAssocBadge.className = 'badge b-gray text-[9px]'; }
        if (editAssocEmpty) editAssocEmpty.classList.remove('hidden');
        if (editAssocCurrent) editAssocCurrent.classList.add('hidden');
        if (editAssocSearch) editAssocSearch.classList.add('hidden');
        if (editAssocLoading) editAssocLoading.classList.add('hidden');
        if (editAssocEmptyList) editAssocEmptyList.classList.add('hidden');
        if (editAssocList) { editAssocList.classList.add('hidden'); editAssocList.innerHTML = ''; }
        if (editConfirmAssoc) editConfirmAssoc.disabled = true;
        if (editSaveBtn) { editSaveBtn.disabled = false; editSaveBtn.innerHTML = '<i class="ti ti-device-floppy text-xs"></i> Guardar cambios'; }
    }

    function populateEditModal(row) {
        resetEditModal();
        editState.paymentId = row.dataset.paymentId;
        editState.editUrl = row.dataset.editUrl;
        editState.currentHasExternal = row.dataset.hasExternal === 'true';

        var opCode = row.dataset.operationCode || '';
        var fileName = row.querySelector('.text-\\[10\\.5px\\]')?.textContent?.trim() || '';
        var label = row.querySelector('.font-semibold')?.textContent?.trim() || 'Comprobante';

        if (editTitle) editTitle.textContent = label;
        if (editSubtitle) editSubtitle.textContent = 'ID: ' + editState.paymentId;
        if (editOpCode) editOpCode.value = opCode;
        if (editFileInfo) editFileInfo.textContent = fileName ? 'Archivo actual: ' + fileName : 'Sin archivo actual';

        if (editState.currentHasExternal) {
            showCurrentAssociation(row);
        } else {
            showEmptyAssociation();
        }
    }

    function showCurrentAssociation(row) {
        if (editAssocEmpty) editAssocEmpty.classList.add('hidden');
        if (editAssocSearch) editAssocSearch.classList.add('hidden');
        if (editAssocCurrent) editAssocCurrent.classList.remove('hidden');
        if (editAssocBadge) { editAssocBadge.textContent = 'Asociado'; editAssocBadge.className = 'badge b-teal text-[9px]'; }
        if (editAssocSerial) editAssocSerial.textContent = 'Voucher externo vinculado';
        if (editAssocName) editAssocName.textContent = 'Se mantendra la asociacion actual al guardar.';
    }

    function showEmptyAssociation() {
        if (editAssocCurrent) editAssocCurrent.classList.add('hidden');
        if (editAssocSearch) editAssocSearch.classList.add('hidden');
        if (editAssocEmpty) editAssocEmpty.classList.remove('hidden');
        if (editAssocBadge) { editAssocBadge.textContent = 'Sin asociar'; editAssocBadge.className = 'badge b-gray text-[9px]'; }
    }

    // Open edit modal
    document.querySelectorAll('.js-edit-payment').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var row = btn.closest('tr');
            populateEditModal(row);
            window.ADM?.Modal?.open('editPaymentModal');
        });
    });

    // File selection
    if (editFileInput) {
        editFileInput.addEventListener('change', function () {
            var file = editFileInput.files?.[0];
            if (file) {
                if (editFilePreview) editFilePreview.classList.remove('hidden');
                if (editFileName) editFileName.textContent = file.name + ' (' + (file.size / 1024).toFixed(0) + ' KB)';
                if (editFileLabel) editFileLabel.textContent = 'Cambiar archivo';
            }
        });
    }
    if (editFileRemove) {
        editFileRemove.addEventListener('click', function () {
            if (editFileInput) editFileInput.value = '';
            if (editFilePreview) editFilePreview.classList.add('hidden');
            if (editFileLabel) editFileLabel.textContent = 'Seleccionar archivo (opcional)';
        });
    }

    // Search external vouchers
    if (editSearchBtn) {
        editSearchBtn.addEventListener('click', async function () {
            if (editAssocEmpty) editAssocEmpty.classList.add('hidden');
            if (editAssocSearch) editAssocSearch.classList.remove('hidden');
            if (editAssocLoading) editAssocLoading.classList.remove('hidden');
            if (editAssocEmptyList) editAssocEmptyList.classList.add('hidden');
            if (editAssocList) { editAssocList.classList.add('hidden'); editAssocList.innerHTML = ''; }
            editState.selectedVoucherId = null;
            if (editConfirmAssoc) editConfirmAssoc.disabled = true;

            try {
                var r = await fetch('/admin/info-postulant/postulant/postulant-resum/' + postulantId + '/external-payments/unassociated', { headers: { Accept: 'application/json' } });
                var vouchers = await r.json();
                if (editAssocLoading) editAssocLoading.classList.add('hidden');

                if (!vouchers || vouchers.length === 0) {
                    if (editAssocEmptyList) editAssocEmptyList.classList.remove('hidden');
                    return;
                }

                var html = '';
                vouchers.forEach(function (v) {
                    var total = 0;
                    if (v.payments) v.payments.forEach(function (p) { total += p.total || 0; });
                    var queried = v.queriedAt ? new Date(v.queriedAt).toLocaleString('es-PE') : '';
                    var payCount = v.payments ? v.payments.length : 0;
                    html += '<label class="flex items-center gap-3 rounded-lg ring-1 ring-ink-200/60 dark:ring-ink-700/60 p-3 cursor-pointer hover:bg-primary-50/40 dark:hover:bg-primary-500/5 transition-colors" data-voucher-id="' + v.id + '">' +
                        '<input type="radio" name="editSelectVoucher" value="' + v.id + '" class="accent-primary-600 shrink-0">' +
                        '<div class="min-w-0 flex-1">' +
                        '<div class="flex items-center gap-2">' +
                        '<span class="text-[13px] font-bold text-ink-900 dark:text-ink-100 font-mono">' + (v.serialVoucher || '') + '</span>' +
                        '<span class="badge b-teal text-[9px]">' + payCount + ' pago(s)</span>' +
                        '</div>' +
                        '<p class="text-[11px] text-ink-500">' + (v.fullName || '') + '</p>' +
                        '<div class="flex items-center gap-3 mt-1 text-[10px] text-ink-400">' +
                        '<span class="font-mono">S/ ' + total.toFixed(2) + '</span>' +
                        '<span><i class="ti ti-clock mr-0.5"></i> ' + queried + '</span>' +
                        '</div></div></label>';
                });

                if (editAssocList) { editAssocList.innerHTML = html; editAssocList.classList.remove('hidden'); }

                editAssocList.querySelectorAll('label[data-voucher-id]').forEach(function (label) {
                    label.addEventListener('click', function () {
                        editState.selectedVoucherId = label.dataset.voucherId;
                        editState.disassociate = false;
                        if (editConfirmAssoc) editConfirmAssoc.disabled = false;
                        editAssocList.querySelectorAll('label').forEach(function (l) {
                            l.classList.toggle('ring-primary-300', l === label);
                            l.classList.toggle('bg-primary-50/60', l === label);
                        });
                    });
                });
            } catch (e) {
                console.error('[edit-payment] load unassociated error', e);
                if (editAssocLoading) editAssocLoading.classList.add('hidden');
                Swal.fire({ icon: 'error', title: 'Error', text: 'No se pudieron cargar los vouchers externos.' });
            }
        });
    }

    // Confirm association from search
    if (editConfirmAssoc) {
        editConfirmAssoc.addEventListener('click', function () {
            if (!editState.selectedVoucherId) return;
            editState.disassociate = false;
            if (editAssocSearch) editAssocSearch.classList.add('hidden');
            if (editAssocEmpty) editAssocEmpty.classList.add('hidden');
            if (editAssocCurrent) editAssocCurrent.classList.remove('hidden');
            if (editAssocBadge) { editAssocBadge.textContent = 'Asociado (nuevo)'; editAssocBadge.className = 'badge b-teal text-[9px]'; }
            if (editAssocSerial) editAssocSerial.textContent = 'Voucher seleccionado';
            if (editAssocName) editAssocName.textContent = 'Se asociara al guardar los cambios.';
        });
    }

    // Cancel search
    if (editCancelSearch) {
        editCancelSearch.addEventListener('click', function () {
            editState.selectedVoucherId = null;
            if (editState.currentHasExternal) {
                showCurrentAssociation();
            } else {
                showEmptyAssociation();
            }
        });
    }

    // Disassociate current
    if (editDisassociateBtn) {
        editDisassociateBtn.addEventListener('click', function () {
            editState.disassociate = true;
            editState.selectedVoucherId = null;
            editState.currentHasExternal = false;
            showEmptyAssociation();
            if (editAssocEmpty) {
                editAssocEmpty.innerHTML = '<p class="text-[11px] text-ink-400 mb-2">La asociacion se eliminara al guardar.</p>' +
                    '<button type="button" id="editPaymentUndoDisassociate" class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-md bg-amber-50 dark:bg-amber-500/10 text-amber-700 dark:text-amber-300 text-[11px] font-semibold hover:bg-amber-100 dark:hover:bg-amber-500/20 transition-colors">' +
                    '<i class="ti ti-arrow-back text-[10px]"></i> Revertir</button>';
                var undoBtn = document.getElementById('editPaymentUndoDisassociate');
                if (undoBtn) {
                    undoBtn.addEventListener('click', function () {
                        editState.disassociate = false;
                        editState.currentHasExternal = true;
                        showCurrentAssociation();
                    });
                }
            }
        });
    }

    // Save button — submit all changes
    if (editSaveBtn) {
        editSaveBtn.addEventListener('click', async function () {
            if (!editState.editUrl) return;
            editSaveBtn.disabled = true;
            editSaveBtn.innerHTML = '<i class="ti ti-loader-2 animate-spin text-xs"></i> Guardando...';

            var fd = new FormData();
            fd.append('__RequestVerificationToken', token());
            if (editOpCode && editOpCode.value.trim()) fd.append('OperationCode', editOpCode.value.trim());
            if (editFileInput && editFileInput.files?.[0]) fd.append('NewFile', editFileInput.files[0]);
            if (editState.disassociate) fd.append('Disassociate', 'true');
            if (editState.selectedVoucherId) fd.append('ExternalPaymentVoucherId', editState.selectedVoucherId);

            try {
                var r = await fetch(editState.editUrl, {
                    method: 'POST',
                    body: fd
                });
                var data = await r.json().catch(function () { return ({}); });
                window.ADM?.Modal?.close('editPaymentModal');
                if (r.ok && data.success) {
                    Swal.fire({ icon: 'success', title: 'Guardado', text: data.message || 'Comprobante actualizado.', timer: 2000, showConfirmButton: false })
                        .then(function () { location.reload(); });
                } else {
                    Swal.fire({ icon: 'error', title: 'Error', text: data.message || 'No se pudo guardar. (HTTP ' + r.status + ')' });
                }
            } catch (e) {
                console.error('[edit-payment] save error', e);
                window.ADM?.Modal?.close('editPaymentModal');
                Swal.fire({ icon: 'error', title: 'Error de conexion', text: 'Revisa tu conexion e intenta nuevamente.' });
            } finally {
                editSaveBtn.disabled = false;
                editSaveBtn.innerHTML = '<i class="ti ti-device-floppy text-xs"></i> Guardar cambios';
            }
        });
    }

    // ════════════════════════════════════════════════════════════════
    //  REPLACE FILE (requirement files only — payments use edit modal)
    // ════════════════════════════════════════════════════════════════
    var fileInput = document.getElementById('replaceFileInput');
    document.querySelectorAll('.js-replace-file').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var row = btn.closest('tr');
            var field = row.querySelector('.font-semibold')?.textContent?.trim() || '';

            Swal.fire({
                title: '¿Reemplazar archivo?',
                html: 'Se reemplazara <strong>' + field + '</strong>.<br>La observacion actual se eliminara.',
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Si, reemplazar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#f54477',
                cancelButtonColor: '#6b7280',
                reverseButtons: true
            }).then(function (sw) {
                if (!sw.isConfirmed) return;
                fileInput.dataset.replaceUrl = row.dataset.replaceUrl;
                fileInput.click();
            });
        });
    });

    if (fileInput) {
        fileInput.addEventListener('change', async function () {
            var file = fileInput.files?.[0];
            if (!file) return;
            var replaceUrl = fileInput.dataset.replaceUrl;
            if (!replaceUrl) return;

            var formData = new FormData();
            formData.append('newFile', file);
            formData.append('__RequestVerificationToken', token());

            try {
                var r = await fetch(replaceUrl, {
                    method: 'POST',
                    body: formData,
                    headers: { 'RequestVerificationToken': token() }
                });
                if (!r.ok) {
                    var err = await r.json().catch(function () { return ({}); });
                    throw new Error(err.message || 'HTTP ' + r.status);
                }
                var result = await r.json();
                if (result.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Archivo reemplazado',
                        text: 'El archivo se actualizo correctamente.',
                        timer: 2000,
                        showConfirmButton: false
                    }).then(function () { location.reload(); });
                }
            } catch (e) {
                console.error('[validation] replace error', e);
                Swal.fire({ icon: 'error', title: 'Error al reemplazar', text: e.message || 'No se pudo reemplazar el archivo.' });
            } finally {
                fileInput.value = '';
            }
        });
    }

    // ════════════════════════════════════════════════════════════════
    //  UPLOAD PENDING REQUIREMENT FILES (SuperAdmin only)
    // ════════════════════════════════════════════════════════════════
    var uploadBtn = document.getElementById('uploadRequirementsBtn');
    var uploadError = document.getElementById('uploadRequirementsError');
    var uploadForm = document.getElementById('uploadRequirementsForm');

    if (uploadBtn && uploadForm) {
        uploadForm.addEventListener('change', function (e) {
            var input = e.target;
            if (!input.matches || !input.matches('input[type="file"]')) return;
            var label = input.closest('label') ? input.closest('label').querySelector('[data-upload-label]') : null;
            if (label && input.files && input.files[0]) {
                label.textContent = input.files[0].name;
                label.classList.remove('text-ink-500');
                label.classList.add('text-primary-600');
            }
        });

        uploadBtn.addEventListener('click', async function () {
            if (uploadError) {
                uploadError.classList.add('hidden');
                uploadError.textContent = '';
            }

            var inputs = Array.prototype.slice.call(uploadForm.querySelectorAll('input[type="file"]'))
                .filter(function (inp) { return inp.files && inp.files.length > 0; });

            if (inputs.length === 0) {
                if (uploadError) {
                    uploadError.textContent = 'Seleccione al menos un archivo para subir.';
                    uploadError.classList.remove('hidden');
                }
                return;
            }

            uploadBtn.disabled = true;
            var originalLabel = uploadBtn.innerHTML;
            uploadBtn.innerHTML = '<i class="ti ti-loader-2 animate-spin text-xs"></i> Subiendo...';

            try {
                var errors = [];
                var uploaded = 0;
                for (var i = 0; i < inputs.length; i++) {
                    var input = inputs[i];
                    var fd = new FormData();
                    fd.append('newFile', input.files[0]);
                    fd.append('requirementId', input.dataset.requirementId);
                    fd.append('__RequestVerificationToken', token());

                    var uploadUrl = '/admin/info-postulant/postulant/postulant-resum/' + postulantId +
                        '/inscriptions/' + inscriptionId + '/file/upload';
                    var r = await fetch(uploadUrl, {
                        method: 'POST',
                        body: fd,
                        headers: { 'RequestVerificationToken': token() }
                    });
                    var data = await r.json().catch(function () { return ({}); });
                    if (!r.ok) {
                        errors.push((input.dataset.requirementName || 'Requisito') + ': ' + (data.message || 'HTTP ' + r.status));
                    } else {
                        uploaded++;
                    }
                }

                if (errors.length === 0) {
                    Swal.fire({
                        icon: 'success',
                        title: uploaded + ' archivo(s) subido(s)',
                        text: 'Los archivos se registraron correctamente.',
                        timer: 2000,
                        showConfirmButton: false
                    }).then(function () { location.reload(); });
                } else {
                    if (uploadError) {
                        uploadError.innerHTML = errors.join('<br>');
                        uploadError.classList.remove('hidden');
                    }
                    uploadBtn.disabled = false;
                    uploadBtn.innerHTML = originalLabel;
                    Swal.fire({ icon: 'error', title: 'Error al subir archivos', text: errors.join(' | ') });
                }
            } catch (e) {
                console.error('[validation] upload error', e);
                if (uploadError) {
                    uploadError.textContent = e.message || 'No se pudo subir los archivos.';
                    uploadError.classList.remove('hidden');
                }
                uploadBtn.disabled = false;
                uploadBtn.innerHTML = originalLabel;
                Swal.fire({ icon: 'error', title: 'Error al subir archivos', text: e.message || 'Ocurri\u00F3 un error inesperado.' });
            }
        });
    }

})();
