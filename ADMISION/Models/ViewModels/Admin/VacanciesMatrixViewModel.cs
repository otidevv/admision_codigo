using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.Models.ViewModels.Admin
{
    public class VacanciesMatrixViewModel
    {
        public Guid ModalityId { get; set; }
        public string ModalityName { get; set; } = string.Empty;
        public List<Career> Careers { get; set; } = new();
        public List<TypeModality> TypeModalities { get; set; } = new();
        public List<Vacancies> Vacancies { get; set; } = new();
        
        // Helper to get quantity for a specific cell
        public int GetQuantity(Guid careerId, Guid typeModalityId)
        {
            var vacancy = Vacancies.FirstOrDefault(v => v.CareerId == careerId && v.TypeModalityId == typeModalityId);
            return vacancy?.Quantity ?? 0;
        }

        public int GetTotalForCareer(Guid careerId)
        {
            return Vacancies.Where(v => v.CareerId == careerId).Sum(v => v.Quantity);
        }

        public int GetTotalForTypeModality(Guid typeModalityId)
        {
            return Vacancies.Where(v => v.TypeModalityId == typeModalityId).Sum(v => v.Quantity);
        }

        public int GetGrandTotal()
        {
            return Vacancies.Sum(v => v.Quantity);
        }
    }

    public class VacanciesIndexViewModel
    {
        public Guid? SelectedModalityId { get; set; }
        public List<Modality> Modalities { get; set; } = new();
        public VacanciesMatrixViewModel? Matrix { get; set; }
    }
}
