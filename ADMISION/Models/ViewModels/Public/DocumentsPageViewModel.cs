namespace ADMISION.Models.ViewModels.Public
{
    public class DocumentsPageViewModel
    {
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "ti ti-folder-open";
        public bool AccentPrimary { get; set; } = true;
        public List<DocumentViewModel> Items { get; set; } = new();
    }
}
