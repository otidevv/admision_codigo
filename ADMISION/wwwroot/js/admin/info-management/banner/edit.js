function previewImage(input, type) {
    var previewId = type === 'h' ? 'preview-h' : 'preview-v';
    var placeholderId = type === 'h' ? 'preview-placeholder-h' : 'preview-placeholder-v';
    var preview = document.getElementById(previewId);
    var placeholder = document.getElementById(placeholderId);

    if (input.files && input.files[0]) {
        var reader = new FileReader();
        reader.onload = function (e) {
            preview.src = e.target.result;
            preview.classList.remove('hidden');
            if (placeholder) placeholder.classList.add('hidden');
        };
        reader.readAsDataURL(input.files[0]);
    } else {
        preview.src = '#';
        preview.classList.add('hidden');
        if (placeholder) placeholder.classList.remove('hidden');
    }
}

window.previewImage = previewImage;