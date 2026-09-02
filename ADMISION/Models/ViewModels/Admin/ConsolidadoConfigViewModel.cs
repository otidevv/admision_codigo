using ADMISION.ENTITIES.Models.Exam;
using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.Models.ViewModels.Admin;

public class ConsolidadoConfigViewModel
{
    public List<Term> Terms { get; set; } = new();
    public Guid? SelectedTermId { get; set; }
    public List<PostulantTypeConfig> Configurations { get; set; } = new();
    public List<Career> Careers { get; set; } = new();
    public List<Modality> Modalities { get; set; } = new();
    public List<TypeModality> TypeModalities { get; set; } = new();
    public bool IsSuperAdmin { get; set; }
}
