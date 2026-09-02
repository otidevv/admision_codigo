// Shared form logic for Prospect Create + Edit: PDF file picker validation.
$(document).ready(function () {
    if (typeof initCustomSelect === 'function') {
        initCustomSelect('TermId');
    }
});

function handleFileSelect(input) {
    const icon = document.getElementById('file-icon');
    const info = document.getElementById('file-info');
    const name = document.getElementById('file-name');
    const size = document.getElementById('file-size');
    const error = document.getElementById('file-error');

    if (input.files && input.files[0]) {
        const file = input.files[0];
        if (file.type !== 'application/pdf') {
            error.classList.remove('hidden');
            input.value = '';
            info.classList.add('hidden');
            icon.classList.remove('text-primary-500');
            icon.classList.add('text-ink-300');
            return;
        }

        error.classList.add('hidden');
        name.textContent = file.name;
        size.textContent = (file.size / 1024 / 1024).toFixed(2) + ' MB';
        info.classList.remove('hidden');
        icon.classList.remove('text-ink-300');
        icon.classList.add('text-primary-500');
    }
}

window.handleFileSelect = handleFileSelect;
