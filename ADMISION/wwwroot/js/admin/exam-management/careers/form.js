// Shared logic for Careers/Create and Careers/Edit forms (banner+logo+gallery).
function previewImage(input, previewId, emptyId) {
    if (!input.files || !input.files[0]) return;
    const url = URL.createObjectURL(input.files[0]);
    const preview = document.getElementById(previewId);
    const empty = document.getElementById(emptyId);
    if (preview) {
        preview.src = url;
        preview.classList.remove('hidden');
    }
    if (empty) empty.classList.add('hidden');
}

function showPickedName(input, labelId) {
    const el = document.getElementById(labelId);
    if (el && input.files && input.files[0]) {
        el.textContent = input.files[0].name;
    }
}

const galleryState = { files: [] };

function onGalleryPicked(input) {
    const incoming = Array.from(input.files || []);
    for (const f of incoming) {
        if (!f.type.startsWith('image/')) continue;
        galleryState.files.push(f);
    }
    syncGalleryInput();
    renderGalleryPreview();
}

function syncGalleryInput() {
    const input = document.getElementById('galleryFiles');
    if (!input) return;
    const dt = new DataTransfer();
    galleryState.files.forEach(f => dt.items.add(f));
    input.files = dt.files;
}

function removeFromGallery(index) {
    galleryState.files.splice(index, 1);
    syncGalleryInput();
    renderGalleryPreview();
}

function renderGalleryPreview() {
    const grid = document.getElementById('galleryPreview');
    const empty = document.getElementById('galleryEmpty');
    if (!grid) return;
    grid.innerHTML = '';
    if (galleryState.files.length === 0) {
        if (empty) empty.classList.remove('hidden');
        return;
    }
    if (empty) empty.classList.add('hidden');
    galleryState.files.forEach((file, i) => {
        const url = URL.createObjectURL(file);
        const card = document.createElement('div');
        card.className = 'relative group rounded-md overflow-hidden ring-1 ring-ink-200 dark:ring-ink-700 bg-ink-50 aspect-square';
        card.innerHTML = `
            <img src="${url}" class="w-full h-full object-cover" alt="" />
            <button type="button" onclick="removeFromGallery(${i})"
                    class="absolute top-2 right-2 w-7 h-7 rounded-full bg-rose-500 hover:bg-rose-600 text-white text-xs flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity shadow-md">
                <i class="ti ti-x"></i>
            </button>
            <span class="absolute bottom-1 left-1 text-[10px] font-semibold text-white bg-black/60 px-1.5 py-0.5 rounded-md">${i + 1}</span>
        `;
        grid.appendChild(card);
    });
}

function csrfToken() {
    const el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : '';
}

document.querySelectorAll('.js-delete-image-btn').forEach(btn => {
    btn.addEventListener('click', function () {
        const wrapper = this.closest('[data-delete-url]');
        if (!wrapper) return;
        const deleteUrl = wrapper.dataset.deleteUrl;
        Swal.fire({
            title: '¿Eliminar imagen?',
            text: 'Esta acción no se puede deshacer.',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#f43f5e',
            cancelButtonColor: '#8b93a5',
            confirmButtonText: 'Sí, eliminar',
            cancelButtonText: 'Cancelar',
            reverseButtons: true
        }).then((result) => {
            if (result.isConfirmed) {
                fetch(deleteUrl, {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': csrfToken() }
                }).then(response => {
                    if (response.ok) location.reload();
                    else alert('Error al eliminar la imagen');
                });
            }
        });
    });
});

window.previewImage = previewImage;
window.showPickedName = showPickedName;
window.onGalleryPicked = onGalleryPicked;
window.removeFromGallery = removeFromGallery;
