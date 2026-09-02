// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// ==================== Toast Notification Helpers ====================
/**
 * Muestra una notificacin toast
 * @param {string} message - Mensaje a mostrar
 * @param {string} type - Tipo: 'success', 'error', 'warning', 'info'
 * @param {number} duration - Duracin en milisegundos (default: 3000)
 */
function showToast(message, type = 'info', duration = 3000) {
    const styles = {
        success: {
            background: "linear-gradient(to right, #00b09b, #96c93d)",
        },
        error: {
            background: "linear-gradient(to right, #ff5f6d, #ffc371)",
        },
        warning: {
            background: "linear-gradient(to right, #f8bb86, #ffc371)",
        },
        info: {
            background: "linear-gradient(to right, #716aca, #a79fe7)",
        },
        primary: {
            background: "linear-gradient(to right, #f54477, #ff6b9d)",
        }
    };

    Toastify({
        text: message,
        duration: duration,
        gravity: "top",
        position: "right",
        style: styles[type] || styles.info,
        stopOnFocus: true,
        onClick: function () {
            this.hideToast();
        }
    }).showToast();
}

// Funciones de conveniencia
function toastSuccess(message, duration = 3000) {
    showToast(message, 'success', duration);
}

function toastError(message, duration = 4000) {
    showToast(message, 'error', duration);
}

function toastWarning(message, duration = 3500) {
    showToast(message, 'warning', duration);
}

function toastInfo(message, duration = 3000) {
    showToast(message, 'info', duration);
}


// ==================== Custom Select Component ====================
function initCustomSelect(selectId, dataUrl) {
    const wrapper = document.querySelector(`[data-select-id="${selectId}"]`);
    if (!wrapper) return;

    const trigger = document.getElementById(`${selectId}_trigger`);
    const dropdown = document.getElementById(`${selectId}_dropdown`);
    const display = document.getElementById(`${selectId}_display`);
    const search = document.getElementById(`${selectId}_search`);
    const optionsList = document.getElementById(`${selectId}_options`);
    const hiddenSelect = document.getElementById(selectId);

    let options = [];
    let selectedValues = [];

    // Toggle dropdown
    trigger.addEventListener('click', () => {
        dropdown.classList.toggle('hidden');
        if (!dropdown.classList.contains('hidden')) {
            search.focus();
        }
    });

    // Close dropdown when clicking outside
    document.addEventListener('click', (e) => {
        if (!wrapper.contains(e.target)) {
            dropdown.classList.add('hidden');
        }
    });

    // Search functionality
    search.addEventListener('input', (e) => {
        const searchTerm = e.target.value.toLowerCase();
        renderOptions(options.filter(opt =>
            opt.text.toLowerCase().includes(searchTerm)
        ));
    });

    // Load data
    if (dataUrl) {
        fetch(dataUrl)
            .then(response => response.json())
            .then(data => {
                options = data;
                renderOptions(options);
            })
            .catch(error => console.error('Error loading select data:', error));
    } else {
        // Load from existing select options
        options = Array.from(hiddenSelect.options).map(opt => ({
            value: opt.value,
            text: opt.text
        }));
        renderOptions(options);
    }

    function renderOptions(optionsToRender) {
        optionsList.innerHTML = '';

        if (optionsToRender.length === 0) {
            optionsList.innerHTML = '<li class="px-4 py-2 text-gray-500 text-sm">No se encontraron resultados</li>';
            return;
        }

        optionsToRender.forEach(option => {
            const li = document.createElement('li');
            li.className = 'px-4 py-2 hover:bg-secondary hover:text-white cursor-pointer transition-colors duration-150';
            li.textContent = option.text;
            li.dataset.value = option.value;

            if (selectedValues.includes(option.value)) {
                li.classList.add('bg-secondary-100', 'text-secondary-700');
            }

            li.addEventListener('click', () => selectOption(option));
            optionsList.appendChild(li);
        });
    }

    function selectOption(option) {
        const isMultiple = hiddenSelect.hasAttribute('multiple');

        if (isMultiple) {
            const index = selectedValues.indexOf(option.value);
            if (index > -1) {
                selectedValues.splice(index, 1);
            } else {
                selectedValues.push(option.value);
            }
        } else {
            selectedValues = [option.value];
            dropdown.classList.add('hidden');
        }

        updateDisplay();
        updateHiddenSelect();
        renderOptions(options);
    }

    function updateDisplay() {
        if (selectedValues.length === 0) {
            display.textContent = trigger.querySelector('span').dataset.placeholder || 'Seleccione...';
            display.classList.add('text-gray-500');
        } else {
            const selectedTexts = options
                .filter(opt => selectedValues.includes(opt.value))
                .map(opt => opt.text);
            display.textContent = selectedTexts.join(', ');
            display.classList.remove('text-gray-500');
        }
    }

    function updateHiddenSelect() {
        Array.from(hiddenSelect.options).forEach(opt => opt.selected = false);
        selectedValues.forEach(value => {
            const option = Array.from(hiddenSelect.options).find(opt => opt.value === value);
            if (option) option.selected = true;
        });
    }
}

// ==================== Custom Dropzone Component ====================
function initCustomDropzone(dropzoneId) {
    const wrapper = document.querySelector(`[data-dropzone-id="${dropzoneId}"]`);
    if (!wrapper) return;

    const dropzone = document.getElementById(dropzoneId);
    let fileInput = document.getElementById(`${dropzoneId}_input`);
    const preview = document.getElementById(`${dropzoneId}_preview`);
    const maxSize = parseInt(dropzone.dataset.maxSize) * 1024 * 1024; // Convert MB to bytes
    const isMultiple = dropzone.dataset.multiple === 'true';
    const acceptedTypes = dropzone.dataset.accepted;

    let files = [];

    // Click to select files
    dropzone.addEventListener('click', () => fileInput.click());

    // Drag and drop events
    dropzone.addEventListener('dragover', (e) => {
        e.preventDefault();
        dropzone.classList.add('border-secondary', 'bg-secondary-50');
    });

    dropzone.addEventListener('dragleave', () => {
        dropzone.classList.remove('border-secondary', 'bg-secondary-50');
    });

    dropzone.addEventListener('drop', (e) => {
        e.preventDefault();
        dropzone.classList.remove('border-secondary', 'bg-secondary-50');
        handleFiles(e.dataTransfer.files);
    });

    // File input change
    fileInput.addEventListener('change', (e) => {
        handleFiles(e.target.files);
    });

    function handleFiles(newFiles) {
        const fileArray = Array.from(newFiles);

        if (!isMultiple) {
            files = [];
        }

        fileArray.forEach(file => {
            // Validate size
            if (file.size > maxSize) {
                Swal.fire({
                    icon: 'error',
                    title: 'Archivo muy grande',
                    text: `El archivo "${file.name}" excede el tamao mximo de ${dropzone.dataset.maxSize} MB`,
                });
                return;
            }

            // Validate type
            if (acceptedTypes !== '*') {
                const isAcceptedType = (file, accepted) => {
                    if (!accepted || accepted === '*') return true;
                    
                    const fileType = file.type ? file.type.toLowerCase() : '';
                    const fileName = file.name ? file.name.toLowerCase() : '';
                    const fileExt = fileName.includes('.') ? '.' + fileName.split('.').pop() : '';
                    
                    const types = accepted.split(',').map(t => t.trim().toLowerCase());
                    
                    for (const type of types) {
                        if (type.startsWith('.')) {
                            // Match extension
                            if (fileExt === type) return true;
                        } else if (type.endsWith('/*')) {
                            // Match MIME wildcard (e.g. image/*)
                            const prefix = type.slice(0, -2);
                            if (fileType.startsWith(prefix + '/')) return true;
                        } else {
                            // Match exact MIME type (e.g. application/pdf)
                            if (fileType === type) return true;
                        }
                    }
                    
                    // Fallback for image/*: allow common image extensions if fileType is generic or blank
                    if (types.some(t => t.startsWith('image/'))) {
                        const commonImageExts = ['.jpg', '.jpeg', '.png', '.gif', '.webp', '.bmp', '.ico', '.heic', '.heif'];
                        if (commonImageExts.includes(fileExt)) return true;
                    }
                    
                    return false;
                };

                if (!isAcceptedType(file, acceptedTypes)) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Tipo de archivo no permitido',
                        text: `El archivo "${file.name}" no es un tipo permitido. Tipos aceptados: ${acceptedTypes}`,
                    });
                    return;
                }
            }

            files.push(file);
        });

        renderPreview();
        updateFileInput();
    }

    function renderPreview() {
        preview.innerHTML = '';

        files.forEach((file, index) => {
            const fileItem = document.createElement('div');
            fileItem.className = 'flex items-center justify-between p-3 bg-white border border-gray-200 rounded-lg hover:shadow-md transition-shadow duration-200';

            const fileInfo = document.createElement('div');
            fileInfo.className = 'flex items-center space-x-3 flex-1';

            const icon = document.createElement('i');
            icon.className = `ti ${getFileIcon(file.name)} text-2xl ${getFileColor(file.name)}`;

            const details = document.createElement('div');
            details.className = 'flex-1';

            const fileName = document.createElement('p');
            fileName.className = 'text-sm font-medium text-gray-900 truncate';
            fileName.textContent = file.name;

            const fileSize = document.createElement('p');
            fileSize.className = 'text-xs text-gray-500';
            fileSize.textContent = formatFileSize(file.size);

            details.appendChild(fileName);
            details.appendChild(fileSize);

            fileInfo.appendChild(icon);
            fileInfo.appendChild(details);

            const removeBtn = document.createElement('button');
            removeBtn.type = 'button';
            removeBtn.className = 'text-red-500 hover:text-red-700 transition-colors duration-150';
            removeBtn.innerHTML = '<i class="ti ti-x"></i>';
            removeBtn.addEventListener('click', () => removeFile(index));

            fileItem.appendChild(fileInfo);
            fileItem.appendChild(removeBtn);
            preview.appendChild(fileItem);
        });
    }

    function removeFile(index) {
        files.splice(index, 1);
        renderPreview();
        updateFileInput();
    }

    function updateFileInput() {
        try {
            const dataTransfer = new DataTransfer();
            files.forEach(file => dataTransfer.items.add(file));
            fileInput.files = dataTransfer.files;
        } catch (_) {
            // DataTransfer not supported (Firefox < 118, old Safari).
            // Replace the input element with a fresh clone; the files array
            // tracks the actual selection and will be re-appended during submit.
            var fresh = fileInput.cloneNode(false);
            fresh.value = '';
            fileInput.parentNode.replaceChild(fresh, fileInput);
            fileInput = fresh;
        }
    }

    function getFileIcon(filename) {
        const ext = filename.split('.').pop().toLowerCase();
        const iconMap = {
            pdf: 'ti-file-type-pdf',
            doc: 'ti-file-type-doc',
            docx: 'ti-file-type-doc',
            xls: 'ti-file-type-xls',
            xlsx: 'ti-file-type-xls',
            ppt: 'ti-file-type-ppt',
            pptx: 'ti-file-type-ppt',
            jpg: 'ti-photo',
            jpeg: 'ti-photo',
            png: 'ti-photo',
            gif: 'ti-photo',
            zip: 'ti-file-zip',
            rar: 'ti-file-zip',
            txt: 'ti-file-text',
        };
        return iconMap[ext] || 'ti-file';
    }

    function getFileColor(filename) {
        const ext = filename.split('.').pop().toLowerCase();
        const colorMap = {
            pdf: 'text-red-500',
            doc: 'text-blue-500',
            docx: 'text-blue-500',
            xls: 'text-green-500',
            xlsx: 'text-green-500',
            ppt: 'text-orange-500',
            pptx: 'text-orange-500',
            jpg: 'text-purple-500',
            jpeg: 'text-purple-500',
            png: 'text-purple-500',
            gif: 'text-purple-500',
            zip: 'text-yellow-600',
            rar: 'text-yellow-600',
        };
        return colorMap[ext] || 'text-gray-500';
    }

    function formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
    }
}
