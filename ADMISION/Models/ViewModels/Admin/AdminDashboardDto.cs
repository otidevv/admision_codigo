using System;
using System.Collections.Generic;

namespace ADMISION.Models.ViewModels.Admin
{
    public class AdminDashboardDto
    {
        public Guid SelectedTermId { get; set; }
        public string SelectedTermName { get; set; } = string.Empty;
        public int TotalPostulants { get; set; }
        public int ActiveCareers { get; set; }
        public int ActiveModalities { get; set; }
        public double AvgAge { get; set; }

        // ── Filtros aplicados (vienen del query string del controller) ──
        public Guid? SelectedModalityId { get; set; }
        public Guid? SelectedTypeModalityId { get; set; }
        public Guid? SelectedCareerId { get; set; }
        public Guid? SelectedTematicAreaId { get; set; }

        // ── Opciones para los selects de filtros (acotadas al término seleccionado) ──
        public DashboardFilterOptions FilterOptions { get; set; } = new();

        public TopicStats Topics { get; set; } = new();
        public GenderStats Gender { get; set; } = new();
        public SchoolStats Schools { get; set; } = new();
        public DisabilityStats Disability { get; set; } = new();
        public AgeGroupStats AgeGroups { get; set; } = new();

        public ChartData ModalitiesChart { get; set; } = new();
        public ChartData CareersChart { get; set; } = new();
        public ChartData RegionsChart { get; set; } = new();
        public ChartData GradeDistribution { get; set; } = new();

        public List<UniversityTransferItem> Traslados { get; set; } = new();
        public List<BankTransferItem> Transfers { get; set; } = new();

        public List<DepartmentMapItem> PeruMap { get; set; } = new();
        public List<CountryMapItem> WorldMap { get; set; } = new();
    }

    public class DashboardFilterOptions
    {
        public List<FilterOption> Modalities { get; set; } = new();
        public List<FilterOptionTyped> TypeModalities { get; set; } = new(); // incluye ModalityId para cascada client-side
        public List<FilterOption> Careers { get; set; } = new();
        public List<FilterOption> TematicAreas { get; set; } = new();
    }

    public class FilterOption
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class FilterOptionTyped : FilterOption
    {
        public Guid ParentId { get; set; }
    }

    public class DepartmentMapItem
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class CountryMapItem
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TopicStats
    {
        public List<TopicStatItem> Items { get; set; } = new();
        public int Total { get; set; }
    }

    public class TopicStatItem
    {
        public string Code { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class GenderStats
    {
        public int Male { get; set; }
        public int Female { get; set; }
        public int Total => Male + Female;
        public double MalePercentage => Total > 0 ? (double)Male / Total * 100 : 0;
        public double FemalePercentage => Total > 0 ? (double)Female / Total * 100 : 0;
    }

    public class SchoolStats
    {
        public int Public { get; set; }
        public int Private { get; set; }
        public int Total => Public + Private;
        public double PublicPercentage => Total > 0 ? (double)Public / Total * 100 : 0;
        public double PrivatePercentage => Total > 0 ? (double)Private / Total * 100 : 0;
    }

    public class DisabilityStats
    {
        public int Visual { get; set; }
        public int Auditory { get; set; }
        public int Motor { get; set; }
        public int Intellectual { get; set; }
        public int Other { get; set; }
        // Unique postulants with any registered disability (not the sum of categories,
        // because a postulant can have several)
        public int TotalUnique { get; set; }
        public int CategorySum => Visual + Auditory + Motor + Intellectual + Other;
        public double VisualPct => CategorySum > 0 ? (double)Visual / CategorySum * 100 : 0;
        public double AuditoryPct => CategorySum > 0 ? (double)Auditory / CategorySum * 100 : 0;
        public double MotorPct => CategorySum > 0 ? (double)Motor / CategorySum * 100 : 0;
        public double IntellectualPct => CategorySum > 0 ? (double)Intellectual / CategorySum * 100 : 0;
        public double OtherPct => CategorySum > 0 ? (double)Other / CategorySum * 100 : 0;
    }

    public class AgeGroupStats
    {
        public int Children { get; set; } // 5-15
        public int Young { get; set; } // 16-25
        public int Adult { get; set; } // 26-45
        public int Senior { get; set; } // 46+
        public int Total => Children + Young + Adult + Senior;
        public double ChildrenPercentage => Total > 0 ? (double)Children / Total * 100 : 0;
        public double YoungPercentage => Total > 0 ? (double)Young / Total * 100 : 0;
        public double AdultPercentage => Total > 0 ? (double)Adult / Total * 100 : 0;
        public double SeniorPercentage => Total > 0 ? (double)Senior / Total * 100 : 0;
        
        // Detailed age labels for bar chart
        public List<int> AgeDistribution { get; set; } = new(); // [16-20, 21-25, etc.]
    }

    public class ChartData
    {
        public List<string> Labels { get; set; } = new();
        public List<int> Values { get; set; } = new();
    }

    public class UniversityTransferItem
    {
        public string University { get; set; } = string.Empty;
        public int External { get; set; }
        public int Internal { get; set; }
        public int Total => External + Internal;
    }

    public class BankTransferItem
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string OperationCode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public double Amount { get; set; }
        public DateTime Date { get; set; }
    }
}
