// Shared file picker for Syllabi Create + Edit.
function handleFileSelect(input) {
    const icon = document.getElementById('file-icon');
    const info = document.getElementById('file-info');
    const placeholder = document.getElementById('file-placeholder');
    const name = document.getElementById('file-name');
    const size = document.getElementById('file-size');
    if (input.files && input.files[0]) {
        const file = input.files[0];
        name.textContent = file.name;
        size.textContent = (file.size / 1024 / 1024).toFixed(2) + ' MB';
        info.classList.remove('hidden');
        if (placeholder) placeholder.classList.add('hidden');
        icon.classList.remove('text-ink-300', 'dark:text-ink-500', 'ti-cloud-upload');
        icon.classList.add('text-primary-500', 'ti-file-check');
    }
}

window.handleFileSelect = handleFileSelect;
