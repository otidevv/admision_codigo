$(document).ready(function () {
    const cfg = window.TypePostulantRequisitesCreateConfig || {};
    const selectedTypeId = cfg.selectedTypeId || '';
    const typesJson = cfg.types || [];

    if (selectedTypeId) {
        setTimeout(() => {
            if (window.customSelectRegistry['TypePostulantInscriptionId']) {
                const type = typesJson.find(t => t.id === selectedTypeId);
                if (type) window.customSelectRegistry['TypePostulantInscriptionId'].setValue(type.id, type.name);
            }
        }, 200);
    }
});
