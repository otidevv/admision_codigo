const MAX_MB = 100;
const MAX_BYTES = MAX_MB * 1024 * 1024;

function showFileError(title, text) {
    if (typeof Swal !== 'undefined') {
        Swal.fire({ icon: 'error', title, text, confirmButtonColor: '#f43f5e' });
    } else {
        alert(title + '\n' + text);
    }
}

function validatePdfFile(file) {
    if (!file) return { ok: false, reason: 'Debes seleccionar un archivo PDF.' };
    const isPdf = file.type === 'application/pdf' || /\.pdf$/i.test(file.name);
    if (!isPdf) return { ok: false, reason: 'Solo se permiten archivos PDF.' };
    if (file.size > MAX_BYTES) {
        const mb = (file.size / 1024 / 1024).toFixed(2);
        return { ok: false, reason: `El archivo pesa ${mb} MB y el máximo permitido es ${MAX_MB} MB.` };
    }
    return { ok: true };
}

function handleFileSelect(input) {
    const info = document.getElementById('file-info');
    const name = document.getElementById('file-name');
    const size = document.getElementById('file-size');

    if (!input.files || !input.files[0]) return;
    const file = input.files[0];

    const result = validatePdfFile(file);
    if (!result.ok) {
        showFileError('Archivo no válido', result.reason);
        input.value = '';
        info.classList.add('hidden');
        return;
    }

    name.textContent = file.name;
    size.textContent = (file.size / 1024 / 1024).toFixed(2) + ' MB';
    info.classList.remove('hidden');
}

document.getElementById('examResultForm')?.addEventListener('submit', (e) => {
    const input = document.getElementById('pdfFile');
    const file = input?.files?.[0];
    const result = validatePdfFile(file);
    if (!result.ok) {
        e.preventDefault();
        showFileError('No se puede publicar', result.reason);
    }
});

window.handleFileSelect = handleFileSelect;
