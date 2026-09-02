(function () {
    let currentInscriptionId = null;
    let qrScannerInstance = null;

    $(() => {
        $('#searchInput').on('keypress', function (e) {
            if (e.which === 13) searchPostulant();
        });
        $('#btnSearch').click(searchPostulant);
        $('#btnVerifyBio').click(verifyBiometric);
        $('#btnSaveManual').click(saveManual);
        $('#btnToggleScanner').click(toggleScanner);
    });

    const token = $('input[name="__RequestVerificationToken"]').val();

    function searchPostulant() {
        const code = $('#searchInput').val().trim();
        if (!code) return;

        const btn = $('#btnSearch');
        btn.prop('disabled', true).html('<i class="ti ti-loader-2 fa-spin"></i> Buscando...');
        $('#emptyState').show();
        $('#loadedState').hide();

        $.get('/admin/info-postulant/attendance/search?code=' + encodeURIComponent(code))
            .done((res) => {
                if (res.success) {
                    const ins = res.inscription;
                    currentInscriptionId = ins.id;

                    $('#postulantCode').text(ins.code);
                    $('#postulantName').text(ins.fullName);
                    $('#postulantCareer').text(ins.careerName);
                    $('#postulantDni').text(ins.document);
                    $('#fingerprintsCount').text(ins.fingerprintsCount);
                    $('#verifiedTermName').text(ins.termName);

                    let photoHtml = '';
                    if (ins.photoUrl) {
                        photoHtml = `<img src="/${ins.photoUrl}" class="w-full h-full object-cover" />`;
                    } else {
                        photoHtml = `<i class="ti ti-user text-5xl"></i>`;
                    }
                    $('#postulantPhoto div').html(photoHtml);

                    if (res.attendance) {
                        $('#alertAlreadyVerified').removeClass('hidden');
                        $('#alertNotApproved').addClass('hidden');
                        $('#actionButtons').addClass('hidden');
                        $('#verifiedDate').text(res.attendance.verifiedAt + ' (' + res.attendance.verifiedBy + ')');
                        Swal.fire({
                            icon: 'info',
                            title: 'Asistencia ya registrada',
                            text: 'El postulante ' + ins.code + ' ya validó su asistencia el ' + res.attendance.verifiedAt + '.',
                            confirmButtonColor: '#6366f1',
                            confirmButtonText: 'Entendido'
                        });
                    } else if (ins.state !== 'Aprobado') {
                        $('#alertAlreadyVerified').addClass('hidden');
                        $('#alertNotApproved').removeClass('hidden');
                        $('#actionButtons').addClass('hidden');
                        $('#notApprovedState').text(ins.state);
                    } else {
                        $('#alertAlreadyVerified').addClass('hidden');
                        $('#alertNotApproved').addClass('hidden');
                        $('#actionButtons').removeClass('hidden');

                        if (ins.fingerprintsCount === 0) {
                            $('#btnVerifyBio')
                                .prop('disabled', true)
                                .attr('title', 'Postulante no tiene huellas');
                            $('#fingerprintsCountBadge').removeClass('b-violet').addClass('b-red');
                        } else {
                            $('#btnVerifyBio')
                                .prop('disabled', false)
                                .removeAttr('title');
                            $('#fingerprintsCountBadge').removeClass('b-red').addClass('b-violet');
                        }
                    }

                    $('#emptyState').hide();
                    $('#loadedState').show();
                }
            })
            .fail((err) => {
                Swal.fire('Error', err.responseJSON?.message || 'Error al buscar postulante', 'error');
            })
            .always(() => {
                btn.prop('disabled', false).html('<i class="ti ti-search text-xs"></i> Buscar expediente');
                $('#searchInput').focus().select();
            });
    }

    function verifyBiometric() {
        if (!currentInscriptionId) return;

        const btn = $('#btnVerifyBio');
        const originalContent = btn.html();
        btn.prop('disabled', true).html('<i class="ti ti-loader-2 fa-spin text-base"></i> OBTENIENDO PLANTILLAS...');

        // Step 1: Fetch stored templates from server
        $.get('/admin/info-postulant/attendance/' + currentInscriptionId + '/verify-templates')
            .done((tplRes) => {
                if (!tplRes.success || !tplRes.templates || tplRes.templates.length === 0) {
                    Swal.fire({
                        title: 'Sin huellas',
                        text: 'El postulante no tiene huellas registradas. Use registro manual.',
                        icon: 'warning',
                        confirmButtonColor: '#f43f5e',
                    });
                    btn.prop('disabled', false).html(originalContent);
                    return;
                }

                btn.html('<i class="ti ti-loader-2 fa-spin text-base"></i> VERIFICANDO LECTOR LOCAL...');

                Swal.fire({
                    title: 'Coloque su dedo en el lector',
                    text: 'Esperando captura biométrica local (15s)...',
                    icon: 'info',
                    allowOutsideClick: false,
                    didOpen: () => { Swal.showLoading(); }
                });

                // Step 2: Call local BiometricBridge directly (same localhost:5000 as capture)
                $.ajax({
                    url: 'http://localhost:5000/api/biometric/verify',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify({ StoredTemplates: tplRes.templates })
                })
                    .done((verifyRes) => {
                        if (verifyRes.matched) {
                            // Step 3: Record result on server
                            $.ajax({
                                url: '/admin/info-postulant/attendance/record-local-verify',
                                type: 'POST',
                                contentType: 'application/json',
                                data: JSON.stringify({
                                    inscriptionId: currentInscriptionId,
                                    score: verifyRes.score
                                }),
                                headers: { "RequestVerificationToken": token }
                            })
                                .done((recRes) => {
                                    Swal.fire({
                                        title: '¡Verificado!',
                                        text: 'Match exitoso (Score: ' + recRes.score + '). Acceso permitido.',
                                        icon: 'success',
                                        timer: 2000,
                                        showConfirmButton: false
                                    });
                                    searchPostulant();
                                })
                                .fail((recErr) => {
                                    Swal.fire({
                                        title: 'Error al registrar',
                                        text: recErr.responseJSON?.message || 'No se pudo registrar la asistencia en el servidor.',
                                        icon: 'error',
                                        confirmButtonColor: '#f43f5e',
                                    });
                                });
                        } else {
                            Swal.fire({
                                title: 'Huella no coincide',
                                text: 'La huella no coincide con los registros del postulante (Score: ' + (verifyRes.score || 0) + ').',
                                icon: 'error',
                                confirmButtonColor: '#f43f5e',
                            });
                        }
                    })
                    .fail((verifyErr) => {
                        const body = verifyErr.responseJSON?.message || verifyErr.responseText || 'Error de conexión con el lector biométrico local.';
                        Swal.fire({
                            title: 'Error del Lector Biométrico',
                            html: '<p class="text-sm leading-relaxed mb-2">No se pudo verificar la huella contra el lector local.</p>' +
                                  '<p class="text-[11px] text-ink-400 font-mono">' + body + '</p>',
                            icon: 'error',
                            confirmButtonColor: '#f43f5e',
                            confirmButtonText: 'Entendido'
                        });
                    })
                    .always(() => {
                        btn.prop('disabled', false).html(originalContent);
                    });
            })
            .fail(() => {
                Swal.fire({
                    title: 'Error de conexión',
                    text: 'No se pudieron obtener las plantillas del servidor.',
                    icon: 'error',
                    confirmButtonColor: '#f43f5e',
                });
                btn.prop('disabled', false).html(originalContent);
            });
    }

    function showManualModal() {
        $('#manualReason').val('');
        $('#manualModal').removeClass('hidden');
    }

    function hideManualModal() {
        $('#manualModal').addClass('hidden');
    }

    function saveManual() {
        const reason = $('#manualReason').val().trim();
        if (!reason) {
            alert("Por favor ingrese un motivo.");
            $('#manualReason').focus();
            return;
        }

        const btn = $('#btnSaveManual');
        const originalContent = btn.html();
        btn.prop('disabled', true).html('<i class="ti ti-loader-2 fa-spin text-xs"></i> Guardando...');

        $.ajax({
            url: '/admin/info-postulant/attendance/manual',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ inscriptionId: currentInscriptionId, notes: reason }),
            headers: { "RequestVerificationToken": token }
        })
            .done((res) => {
                Swal.fire('Registrado', res.message, 'success');
                hideManualModal();
                searchPostulant();
            })
            .fail((err) => {
                Swal.fire('Error', err.responseJSON?.message || 'Error al guardar', 'error');
            })
            .always(() => {
                btn.prop('disabled', false).html(originalContent);
            });
    }

    function registerByQr(code) {
        if (!code) return;

        $.ajax({
            url: '/admin/info-postulant/attendance/qr-register',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ code: code }),
            headers: { "RequestVerificationToken": token }
        })
            .done((res) => {
                if (res.notApproved) {
                    Swal.fire({
                        icon: 'warning',
                        title: 'Inscripción no aprobada',
                        text: res.message,
                        confirmButtonColor: '#f59e0b',
                        confirmButtonText: 'Entendido'
                    });
                    return;
                }
                if (res.success) {
                    const ins = res.inscription;
                    currentInscriptionId = ins.id;

                    $('#postulantCode').text(ins.code);
                    $('#postulantName').text(ins.fullName);
                    $('#postulantCareer').text(ins.careerName);
                    $('#postulantDni').text(ins.document);
                    $('#verifiedTermName').text(ins.termName);
                    $('#emptyState').hide();
                    $('#loadedState').show();

                    if (res.alreadyRegistered) {
                        $('#alertAlreadyVerified').removeClass('hidden');
                        $('#actionButtons').addClass('hidden');
                        $('#verifiedDate').text(res.attendance.verifiedAt + ' (' + res.attendance.verifiedBy + ')');
                        Swal.fire({
                            icon: 'info',
                            title: 'Asistencia ya registrada',
                            text: 'El postulante ' + ins.code + ' ya registró su asistencia.',
                            confirmButtonColor: '#6366f1',
                            confirmButtonText: 'Entendido'
                        });
                    } else {
                        $('#alertAlreadyVerified').addClass('hidden');
                        $('#actionButtons').addClass('hidden');
                        Swal.fire({
                            icon: 'success',
                            title: '¡Asistencia registrada!',
                            text: res.message,
                            timer: 2500,
                            showConfirmButton: false
                        });
                    }
                }
            })
            .fail((err) => {
                const statusText = err.status + ' ' + err.statusText;
                const body = err.responseJSON?.message || err.responseText || 'Error al registrar asistencia.';
                Swal.fire({
                    title: 'Error',
                    text: statusText + '\n' + body,
                    icon: 'error',
                    confirmButtonColor: '#f43f5e',
                });
            });
    }

    function toggleScanner() {
        if (qrScannerInstance) {
            qrScannerInstance.stop().then(() => {
                qrScannerInstance = null;
                $('#qrScannerContainer').addClass('hidden');
                $('#btnToggleScanner').html('<i class="ti ti-camera text-xs"></i> Escanear QR');
            });
            return;
        }

        if (!window.Html5Qrcode) {
            Swal.fire({
                title: 'Error',
                text: 'La librería de escaneo no está cargada. Verifique su conexión a internet.',
                icon: 'error',
                confirmButtonColor: '#f43f5e',
            });
            return;
        }

        $('#qrScannerContainer').removeClass('hidden');
        $('#btnToggleScanner').html('<i class="ti ti-camera text-xs"></i> Detener cámara');

        qrScannerInstance = new Html5Qrcode("qrScanner");
        qrScannerInstance.start(
            { facingMode: "environment" },
            { fps: 10, qrbox: { width: 250, height: 250 } },
            (decodedText) => {
                // Detener escáner al encontrar código
                qrScannerInstance.stop().then(() => {
                    qrScannerInstance = null;
                    $('#qrScannerContainer').addClass('hidden');
                    $('#btnToggleScanner').html('<i class="ti ti-camera text-xs"></i> Escanear QR');
                });

                registerByQr(decodedText);
            }
        ).catch((err) => {
            Swal.fire('Error de cámara', err || 'No se pudo acceder a la cámara.', 'error');
            qrScannerInstance = null;
            $('#qrScannerContainer').addClass('hidden');
            $('#btnToggleScanner').html('<i class="ti ti-camera text-xs"></i> Escanear QR');
        });
    }

    window.showManualModal = showManualModal;
    window.hideManualModal = hideManualModal;
})();
