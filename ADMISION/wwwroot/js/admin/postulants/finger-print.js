const FingerCapture = (function() {
    let currentPostulantId = '';

    function csrfToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    function init(postulantId) {
        currentPostulantId = postulantId;
        loadFingerprints();
        document.getElementById('btnCaptureFingerprint').addEventListener('click', capture);
    }

    function loadFingerprints() {
        if(!currentPostulantId) return;
        const container = document.getElementById('fingerprintGrid');
        
        fetch(`/admin/info-postulant/postulant/postulant-resum/${currentPostulantId}/fingerprints`)
            .then(r => r.json())
            .then(fingers => {
                let html = '';
                
                document.getElementById('fpCountDisplay').textContent = fingers.length;

                for (let i = 0; i < 10; i++) {
                    const fp = fingers[i]; // Mapeo simple secuencial (0 a 9)
                    
                    if(fp) {
                        // Use base64 if present
                        const src = fp.imageBase64 ? `data:image/bmp;base64,${fp.imageBase64}` : null;
                        
                        html += `
                            <div class="relative group aspect-square rounded-2xl bg-white border border-gray-200 overflow-hidden flex items-center justify-center shadow-sm hover:shadow-md transition-all">
                                ${src ? `<img src="${src}" class="w-full h-full object-cover opacity-80" style="filter: contrast(1.2)" />` : `<i class="ti ti-fingerprint text-3xl text-indigo-200"></i>`}
                                <div class="absolute inset-0 bg-gradient-to-t from-black/60 to-transparent flex flex-col justify-end p-2 opacity-0 group-hover:opacity-100 transition-opacity">
                                    <button onclick="FingerCapture.deleteFp('${fp.id}')" class="w-full py-1 bg-red-500 text-white text-[10px] font-bold rounded">Eliminar</button>
                                </div>
                            </div>
                        `;
                    } else {
                        html += `
                            <div class="aspect-square rounded-2xl bg-gray-50 border-2 border-dashed border-gray-100 flex items-center justify-center text-gray-200 cursor-not-allowed">
                                <i class="ti ti-fingerprint text-xl opacity-50"></i>
                            </div>
                        `;
                    }
                }
                container.innerHTML = html;
            });
    }

    function capture() {
        const btn = document.getElementById('btnCaptureFingerprint');
        const originalText = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<i class="ti ti-loader-2 fa-spin mr-2"></i> Capturando desde Lector...';

        Swal.fire({
            title: 'Coloque el dedo',
            text: 'Esperando al lector ZK9500 en localhost:5000...',
            icon: 'info',
            allowEscapeKey: false,
            allowOutsideClick: false,
            didOpen: () => { Swal.showLoading(); }
        });

        // Llamada al BiometricBridge
        fetch('http://localhost:5000/api/biometric/capture')
            .then(r => r.json())
            .then(res => {
                if (res.success) {
                    Swal.fire({
                        title: 'Huella capturada',
                        text: 'Guardando en la base de datos...',
                        timer: 1000,
                        showConfirmButton: false,
                        didOpen: () => { Swal.showLoading(); }
                    });

                    // Guardar en nuestro backend
                    return fetch(`/admin/info-postulant/postulant/postulant-resum/${currentPostulantId}/capture-fingerprint`, {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrfToken() },
                        body: JSON.stringify({ 
                            template: res.template,
                            imageBase64: res.imageBase64
                        })
                    });
                } else {
                    throw new Error(res.message || 'Lector no detectó huella.');
                }
            })
            .then(r => {
                if(r && r.json) return r.json();
                return null;
            })
            .then(res => {
                if (res && res.success) {
                    Swal.fire('Guardado', 'La huella ha sido registrada correctamente.', 'success');
                    loadFingerprints();
                } else if(res) {
                    Swal.fire('Error al guardar', res.message, 'error');
                }
            })
            .catch(err => {
                console.error(err);
                Swal.fire('Fallo Lector', 'No se pudo conectar al lector biométrico. Asegúrese de que el ZK9500 esté conectado y BiometricBridge en ejecución.', 'error');
            })
            .finally(() => {
                btn.disabled = false;
                btn.innerHTML = originalText;
            });
    }

    function deleteFp(fingerId) {
        Swal.fire({
            title: '¿Eliminar huella?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#ef4444',
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar'
        }).then((result) => {
            if (result.isConfirmed) {
                fetch(`/admin/info-postulant/postulant/postulant-resum/${currentPostulantId}/fingerprint/${fingerId}`, {
                    method: 'DELETE',
                    headers: { 'RequestVerificationToken': csrfToken() }
                })
                .then(r => r.json())
                .then(res => {
                    if (res.success) {
                        loadFingerprints();
                    }
                });
            }
        });
    }

    return { init, capture, deleteFp };
})();
