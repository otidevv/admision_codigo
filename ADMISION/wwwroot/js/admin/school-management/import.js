function handleFileSelect(input) {
    const info = document.getElementById('file-info');
    const name = document.getElementById('file-name');
    const size = document.getElementById('file-size');

    if (!input.files || !input.files[0]) return;
    const file = input.files[0];

    name.textContent = file.name;
    size.textContent = (file.size / 1024 / 1024).toFixed(2) + ' MB';
    info.classList.remove('hidden');
}

window.handleFileSelect = handleFileSelect;
