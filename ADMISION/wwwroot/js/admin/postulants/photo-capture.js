const PhotoCapture = (function() {
    let stream = null;
    let currentPostulantId = '';
    let v = null, c = null, ctx = null, frame = null;

    function csrfToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    function init(postulantId) {
        currentPostulantId = postulantId;
        v = document.getElementById('webcam');
        c = document.getElementById('captureCanvas');
        if(c) ctx = c.getContext('2d');
        frame = document.getElementById('selectionFrame');
        
        loadGallery();
        setupListeners();
    }

    function open() {
        document.getElementById('captureModal').classList.remove('hidden');
        navigator.mediaDevices.getUserMedia({ video: { width: 1280, height: 720, facingMode: 'user' } })
            .then(s => {
                stream = s;
                if(v) v.srcObject = s;
            })
            .catch(err => {
                alert('No se pudo acceder a la cámara: ' + err.message);
                close();
            });
        applyFilters();
    }

    function close() {
        if (stream) {
            stream.getTracks().forEach(t => t.stop());
        }
        document.getElementById('captureModal').classList.add('hidden');
    }

    function applyFilters() {
        const briRange = document.getElementById('briRange');
        if(!briRange) return;

        const b = briRange.value;
        const cont = document.getElementById('conRange').value;
        const s = document.getElementById('satRange').value;
        
        document.getElementById('briVal').textContent = b + '%';
        document.getElementById('conVal').textContent = cont + '%';
        document.getElementById('satVal').textContent = s + '%';
        
        if(v) v.style.filter = `brightness(${b}%) contrast(${cont}%) saturate(${s}%)`;
    }

    function resetFilters() {
        document.getElementById('briRange').value = 100;
        document.getElementById('conRange').value = 100;
        document.getElementById('satRange').value = 100;
        applyFilters();
    }

    function capture() {
        if(!v || !c || !frame) return;
        const container = v.parentElement;
        
        const dw = v.offsetWidth;
        const dh = v.offsetHeight;
        const vw = v.videoWidth;
        const vh = v.videoHeight;
        
        const fx = frame.offsetLeft;
        const fy = frame.offsetTop;
        const fw = frame.offsetWidth;
        const fh = frame.offsetHeight;
        
        const displayRatio = dw / dh;
        const videoRatio = vw / vh;
        
        let scale, offsetLeft = 0, offsetTop = 0;
        
        if (videoRatio > displayRatio) {
            scale = vh / dh;
            offsetLeft = (vw - (dw * scale)) / 2;
        } else {
            scale = vw / dw;
            offsetTop = (vh - (dh * scale)) / 2;
        }
        
        const sx = (fx * scale) + offsetLeft;
        const sy = (fy * scale) + offsetTop;
        const sw = fw * scale;
        const sh = fh * scale;

        const size = 600;
        c.width = size;
        c.height = size;

        ctx.filter = v.style.filter;
        ctx.drawImage(v, sx, sy, sw, sh, 0, 0, size, size);

        const dataUrl = c.toDataURL('image/jpeg', 0.9);
        save(dataUrl);
    }

    function save(image) {
        const btn = document.getElementById('btnCapture');
        if (btn) {
            btn.disabled = true;
            btn.innerHTML = '<i class="ti ti-loader-2 fa-spin"></i> GUARDANDO...';
        }

        fetch(`/admin/info-postulant/postulant/postulant-resum/${currentPostulantId}/capture-photo`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrfToken() },
            body: JSON.stringify({ image })
        })
        .then(r => {
            if (!r.ok) {
                return r.json().then(err => { throw new Error(err.message || 'Error del servidor'); });
            }
            return r.json();
        })
        .then(res => {
            Swal.close();
            if (res.success) {
                const img = document.getElementById('primaryPhotoImg');
                if (img) {
                   img.src = '/' + res.photoUrl;
                   img.onerror = function() { this.style.display = 'none'; };
                } else {
                   const container = document.getElementById('primaryPhotoContainer');
                   if (container) {
                       container.innerHTML = `<img id="primaryPhotoImg" src="/${res.photoUrl}" alt="foto postulante" class="w-full h-full object-cover" onerror="this.style.display='none'" />`;
                   }
                }
                loadGallery();
                close();
            } else {
                Swal.fire('Error', res.message || 'No se pudo guardar la foto.', 'error');
            }
        })
        .catch(err => {
            Swal.close();
            Swal.fire('Error', err.message || 'Error de conexión al guardar la foto.', 'error');
        })
        .finally(() => {
            if (btn) {
                btn.disabled = false;
                btn.innerHTML = '<i class="ti ti-camera"></i> CAPTURAR FOTO';
            }
        });
    }

    function loadGallery() {
        if(!currentPostulantId) return;
        const gallery = document.getElementById('photoGallery');
        fetch(`/admin/info-postulant/postulant/postulant-resum/${currentPostulantId}/photos`)
            .then(r => r.json())
            .then(photos => {
                gallery.innerHTML = photos.length ? '' : '<div class="col-span-full py-12 text-center text-slate-300">Sin fotos cargadas.</div>';
                let primaryPhotoId = null;
                photos.forEach(p => {
                    if (p.isPrimary) primaryPhotoId = p.id;
                    const div = document.createElement('div');
                    div.className = `relative group aspect-square rounded-2xl overflow-hidden border-2 transition-all ${p.isPrimary ? 'border-primary shadow-lg ring-4 ring-primary/10' : 'border-gray-50 hover:border-primary/40'}`;
                    div.innerHTML = `
                        <img src="/${p.photoUrl}" class="w-full h-full object-cover" />
                        ${p.isPrimary ? '<div class="absolute top-2 right-2 w-5 h-5 bg-primary text-white rounded-full flex items-center justify-center text-[8px] shadow-sm z-10"><i class="ti ti-check"></i></div>' : ''}
                        <div class="absolute inset-0 bg-primary/60 opacity-0 group-hover:opacity-100 flex flex-col items-center justify-center gap-2 transition-all p-2">
                            ${p.isPrimary ? '' : `<button type="button" data-action="set-primary" data-photo-id="${p.id}" class="w-full py-1.5 bg-white text-primary text-[10px] font-bold rounded uppercase tracking-wider hover:bg-primary-50">Usar foto</button>`}
                            <button type="button" data-action="delete" data-photo-id="${p.id}" class="w-full py-1.5 bg-rose-500 text-white text-[10px] font-bold rounded uppercase tracking-wider hover:bg-rose-600 inline-flex items-center justify-center gap-1">
                                <i class="ti ti-trash"></i> Eliminar
                            </button>
                        </div>
                    `;
                    gallery.appendChild(div);
                });

                // Sync primary photo ID to the delete button in the card
                const delBtn = document.getElementById('btnDeletePrimaryPhoto');
                if (delBtn) {
                    if (primaryPhotoId) {
                        delBtn.dataset.photoId = primaryPhotoId;
                        delBtn.classList.remove('hidden');
                    } else {
                        delBtn.classList.add('hidden');
                    }
                }

                gallery.querySelectorAll('[data-action="set-primary"]').forEach(btn => {
                    btn.addEventListener('click', (e) => {
                        e.stopPropagation();
                        setPrimary(btn.dataset.photoId);
                    });
                });
                gallery.querySelectorAll('[data-action="delete"]').forEach(btn => {
                    btn.addEventListener('click', (e) => {
                        e.stopPropagation();
                        deletePhoto(btn.dataset.photoId);
                    });
                });
            })
            .catch(() => {
                gallery.innerHTML = '<div class="col-span-full py-12 text-center text-rose-300">Error al cargar galería.</div>';
            });
    }

    function setPrimary(photoId) {
        fetch(`/admin/info-postulant/postulant/postulant-resum/${currentPostulantId}/set-primary-photo/${photoId}`, {
            method: 'POST',
            headers: { 'RequestVerificationToken': csrfToken() }
        })
        .then(r => r.json())
        .then(res => {
            if (res.success) {
                loadGallery();
                location.reload();
            }
        });
    }

    function deletePhoto(photoId) {
        Swal.fire({
            title: '¿Eliminar foto?',
            text: 'La foto se eliminará del expediente y no podrá recuperarse.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#ef4444',
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar'
        }).then((result) => {
            if (!result.isConfirmed) return;

            fetch(`/admin/info-postulant/postulant/postulant-resum/${currentPostulantId}/photo/${photoId}`, {
                method: 'DELETE',
                headers: { 'RequestVerificationToken': csrfToken() }
            })
            .then(r => r.json().then(body => ({ ok: r.ok, body })))
            .then(({ ok, body }) => {
                if (!ok || !body.success) {
                    Swal.fire('Error', body.message || 'No se pudo eliminar la foto.', 'error');
                    return;
                }

                if (body.deletedPrimary) {
                    const container = document.getElementById('primaryPhotoContainer');
                    if (container) {
                        if (body.newPrimaryPhotoUrl) {
                            container.innerHTML = `<img id="primaryPhotoImg" src="/${body.newPrimaryPhotoUrl}" alt="foto postulante" class="w-full h-full object-cover" />`;
                        } else {
                            container.innerHTML = `
                                <i class="ti ti-user text-6xl"></i>
                                <p class="text-[10px] mt-2 uppercase font-bold tracking-[0.16em]">Sin capturar</p>
                            `;
                        }
                    }
                }

                loadGallery();
                Swal.fire({
                    icon: 'success',
                    title: 'Foto eliminada',
                    timer: 1200,
                    showConfirmButton: false
                });
            })
            .catch(() => Swal.fire('Error', 'Ocurrió un problema al eliminar la foto.', 'error'));
        });
    }

    function setupListeners() {
        const briRange = document.getElementById('briRange');
        if(briRange) briRange.oninput = applyFilters;
        
        const conRange = document.getElementById('conRange');
        if(conRange) conRange.oninput = applyFilters;
        
        const satRange = document.getElementById('satRange');
        if(satRange) satRange.oninput = applyFilters;
        
        const btnCapture = document.getElementById('btnCapture');
        if(btnCapture) btnCapture.onclick = capture;

        if(!frame) return;
        let isDragging = false;
        let isResizing = false;
        let activeHandle = null;
        let startX, startY, startLeft, startTop, startWidth, startHeight;

        frame.addEventListener('mousedown', (e) => {
            if (e.target.dataset.handle) {
                isResizing = true;
                activeHandle = e.target.dataset.handle;
            } else {
                isDragging = true;
            }
            
            startX = e.clientX;
            startY = e.clientY;
            startLeft = frame.offsetLeft;
            startTop = frame.offsetTop;
            startWidth = frame.offsetWidth;
            startHeight = frame.offsetHeight;
            
            document.addEventListener('mousemove', handleMouseMove);
            document.addEventListener('mouseup', handleMouseUp);
        });

        function handleMouseMove(e) {
            const container = v.parentElement;
            const cw = container.offsetWidth;
            const ch = container.offsetHeight;

            if (isDragging) {
                let left = startLeft + (e.clientX - startX);
                let top = startTop + (e.clientY - startY);

                left = Math.max(0, Math.min(left, cw - frame.offsetWidth));
                top = Math.max(0, Math.min(top, ch - frame.offsetHeight));

                frame.style.left = left + 'px';
                frame.style.top = top + 'px';
            } else if (isResizing) {
                const dx = e.clientX - startX;
                const dy = e.clientY - startY;
                let newWidth = startWidth;
                let newHeight = startHeight;
                let newLeft = startLeft;
                let newTop = startTop;

                if (activeHandle === 'se') {
                    newWidth = startWidth + dx;
                    newHeight = startHeight + dy;
                } else if (activeHandle === 'sw') {
                    newWidth = startWidth - dx;
                    newHeight = startHeight + dy;
                    newLeft = startLeft + dx;
                } else if (activeHandle === 'nw') {
                    newWidth = startWidth - dx;
                    newHeight = startHeight - dy;
                    newLeft = startLeft + dx;
                    newTop = startTop + dy;
                } else if (activeHandle === 'ne') {
                    newWidth = startWidth + dx;
                    newHeight = startHeight - dy;
                    newTop = startTop + dy;
                }

                const min = 100;
                if (newWidth < min) { newWidth = min; newLeft = frame.offsetLeft; }
                if (newHeight < min) { newHeight = min; newTop = frame.offsetTop; }
                
                if (newLeft < 0) { newWidth += newLeft; newLeft = 0; }
                if (newTop < 0) { newHeight += newTop; newTop = 0; }
                if (newLeft + newWidth > cw) { newWidth = cw - newLeft; }
                if (newTop + newHeight > ch) { newHeight = ch - newTop; }

                frame.style.width = newWidth + 'px';
                frame.style.height = newHeight + 'px';
                frame.style.left = newLeft + 'px';
                frame.style.top = newTop + 'px';
            }
        }

        function handleMouseUp() {
            isDragging = false;
            isResizing = false;
            activeHandle = null;
            document.removeEventListener('mousemove', handleMouseMove);
            document.removeEventListener('mouseup', handleMouseUp);
        }
    }

    function deletePrimaryPhoto() {
        const btn = document.getElementById('btnDeletePrimaryPhoto');
        if (!btn || !btn.dataset.photoId) {
            Swal.fire('Aviso', 'No hay foto principal para eliminar.', 'info');
            return;
        }
        deletePhoto(btn.dataset.photoId);
    }

    function uploadFromFile(input) {
        if (!input || !input.files || !input.files[0]) return;
        const file = input.files[0];
        if (!file.type.startsWith('image/')) {
            Swal.fire('Formato no válido', 'Selecciona un archivo de imagen.', 'warning');
            return;
        }
        Swal.fire({
            title: 'Subiendo foto...',
            text: 'Procesando imagen, espere un momento.',
            allowOutsideClick: false,
            didOpen: () => Swal.showLoading()
        });
        const reader = new FileReader();
        reader.onload = function (e) {
            save(e.target.result);
            input.value = '';
        };
        reader.onerror = function () {
            Swal.close();
            Swal.fire('Error', 'No se pudo leer el archivo.', 'error');
        };
        reader.readAsDataURL(file);
    }

    return { init, open, close, resetFilters, setPrimary, capture, deletePhoto, deletePrimaryPhoto, uploadFromFile };
})();
