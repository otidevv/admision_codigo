using ADMISION.ENTITIES.Models.Exam;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.ENTITIES.Models.Users;

namespace ADMISION.Models.ViewModels.Admin;

public class CepreTurnosViewModel
{
    public List<Term> Terms { get; set; } = new();
    public List<Users> SupportUsers { get; set; } = new();
    public List<CepreTurn> Turns { get; set; } = new();
    public Guid? SelectedTermId { get; set; }
    public Guid? SelectedUserId { get; set; }
    public DateTimeOffset? TurnStartDate { get; set; }
    public DateTimeOffset? TurnEndDate { get; set; }
}
