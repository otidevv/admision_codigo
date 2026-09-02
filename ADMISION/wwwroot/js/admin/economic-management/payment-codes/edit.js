let nextIndex = window.PaymentCodeEditConfig?.startIndex ?? 0;

function addAssociation() {
    const container = document.getElementById('associationsContainer');
    const template = document.getElementById('rowTemplate');
    const emptyMessage = document.getElementById('emptyMessage');

    const html = template.innerHTML.replace(/INDEX/g, nextIndex);
    container.insertAdjacentHTML('beforeend', html);

    nextIndex++;
    emptyMessage.classList.add('hidden');
}

function removeRow(btn) {
    btn.closest('tr').remove();
    reindexRows();

    const container = document.getElementById('associationsContainer');
    const emptyMessage = document.getElementById('emptyMessage');
    if (container.children.length === 0) {
        emptyMessage.classList.remove('hidden');
    }
}

function reindexRows() {
    const rows = document.querySelectorAll('#associationsContainer tr');
    rows.forEach((row, idx) => {
        row.querySelectorAll('select, input').forEach(el => {
            const name = el.getAttribute('name');
            if (name) {
                el.setAttribute('name', name.replace(/modalities\[\d+\]/, 'modalities[' + idx + ']'));
            }
        });
    });
    nextIndex = rows.length;
}

window.addAssociation = addAssociation;
window.removeRow = removeRow;
