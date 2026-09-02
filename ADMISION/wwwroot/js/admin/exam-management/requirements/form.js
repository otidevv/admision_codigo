// Shared form logic for Requirements Create + Edit: extension checkbox sync.
document.addEventListener('DOMContentLoaded', function () {
    const checkboxes = document.querySelectorAll('.extension-checkbox');
    const hiddenInput = document.getElementById('FilePathExtencion');
    function updateHiddenInput() {
        hiddenInput.value = Array.from(checkboxes).filter(i => i.checked).map(i => i.value).join(', ');
    }
    checkboxes.forEach(cb => cb.addEventListener('change', updateHiddenInput));
    if (hiddenInput.value) {
        const currentExts = hiddenInput.value.split(',').map(e => e.trim());
        checkboxes.forEach(cb => { if (currentExts.includes(cb.value)) cb.checked = true; });
    }
});
