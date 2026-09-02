using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.Models.ViewModels.Public
{
    public class ResultsPublicViewModel
    {
        public List<Term> Terms { get; set; } = new();
        public Term? SelectedTerm { get; set; }
        public List<ResultItem> Items { get; set; } = new();
    }

    public class ResultItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ModalityName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public DateTimeOffset PublishedAt { get; set; }
    }
}
