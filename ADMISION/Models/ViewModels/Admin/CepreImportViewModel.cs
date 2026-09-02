using ADMISION.ENTITIES.Models.Exam;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Services.Interfaces;

namespace ADMISION.Models.ViewModels.Admin;

public class CepreImportViewModel
{
    public List<Term> Terms { get; set; } = new();
    public Guid? SelectedTermId { get; set; }
    public ImportPreviewResult<CepreImportRow>? Preview { get; set; }
    public bool HasPreview => Preview != null && Preview.Rows.Count > 0;
    public List<ImportBatchDto> ImportHistory { get; set; } = new();
    public List<CepreImportVersion> Versions { get; set; } = new();
    public bool IsSuperAdmin { get; set; }
    public bool HasActiveTurn { get; set; }
    public bool CanImport { get; set; }
}
