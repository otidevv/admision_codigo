using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Services.Interfaces;

namespace ADMISION.Models.ViewModels.Admin;

public class AdmissionImportViewModel
{
    public List<Term> Terms { get; set; } = new();
    public Guid? SelectedTermId { get; set; }
    public List<Modality> Modalities { get; set; } = new();
    public Guid? SelectedModalityId { get; set; }
    public ImportPreviewResult<AdmissionImportRow>? Preview { get; set; }
    public bool HasPreview => Preview != null && Preview.Rows.Count > 0;
    public List<ImportBatchDto> ImportHistory { get; set; } = new();
    public bool IsSuperAdmin { get; set; }
}
