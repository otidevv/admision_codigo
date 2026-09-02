using ADMISION.ENTITIES.Models.Info;
using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.Models.ViewModels.Public
{
    public class HomeViewModel
    {
        public List<Modality> ActiveExams { get; set; } = new();
        public List<DocumentViewModel> Prospects { get; set; } = new();
        public List<DocumentViewModel> Regulations { get; set; } = new();
        public List<DocumentViewModel> Syllabi { get; set; } = new();
        public List<DocumentViewModel> OtherFiles { get; set; } = new();
        
        public string BannerTitle { get; set; } = string.Empty;
        public string BannerSubtitle { get; set; } = string.Empty;
        public string BannerDescription { get; set; } = string.Empty;
        public string BannerCtaText { get; set; } = string.Empty;
        public string BannerCtaUrl { get; set; } = string.Empty;
        public string BannerImage { get; set; } = string.Empty;

        public List<Banner> Banners { get; set; } = new();
        public List<Career> Careers { get; set; } = new();
        public List<Sponsor> Sponsors { get; set; } = new();
        public List<Announcement> Announcements { get; set; } = new();
    }
}
