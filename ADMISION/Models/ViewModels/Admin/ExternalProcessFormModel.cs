using ADMISION.Services.Interfaces;

namespace ADMISION.Models.ViewModels.Admin
{
    public class ExternalProcessFormModel
    {
        public Guid? ScoringProfileId { get; set; }
        public string? Titulo { get; set; }
        public IReadOnlyList<ScoringProfileListItem> Profiles { get; set; } = Array.Empty<ScoringProfileListItem>();
        public ExternalProcessReport? Report { get; set; }
    }

    public class ExternalProcessReport
    {
        public string Titulo { get; set; } = "";
        public string ProfileName { get; set; } = "";
        public ExternalScoringResult Data { get; set; } = new();
    }
}
