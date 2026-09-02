// Shared form logic for ExamResults Create and Edit.
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
    if (!file) return { ok: true };
    const isPdf = file.type === 'application/pdf' || /\.pdf$/i.test(file.name);
    if (!isPdf) return { ok: false, reason: 'Solo se permiten archivos PDF.' };
    if (file.size > MAX_BYTES) {
        const mb = (file.size / 1024 / 1024).toFixed(2);
        return { ok: false, reason: `El archivo pesa ${mb} MB y el máximo permitido es ${MAX_MB} MB.` };
    }
    return { ok: true };
}

function handleFileSelect(input) {
    const icon = document.getElementById('file-icon');
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
        icon.classList.add('text-ink-400');
        icon.classList.remove('text-primary-500');
        return;
    }

    name.textContent = file.name;
    size.textContent = (file.size / 1024 / 1024).toFixed(2) + ' MB';
    info.classList.remove('hidden');
    icon.classList.remove('text-ink-400');
    icon.classList.add('text-primary-500');
}

document.getElementById('examResultForm')?.addEventListener('submit', (e) => {
    const input = document.getElementById('pdfFile');
    const file = input?.files?.[0];
    const result = validatePdfFile(file);
    if (!result.ok) {
        e.preventDefault();
        showFileError('No se puede guardar', result.reason);
    }
});

window.handleFileSelect = handleFileSelect;
