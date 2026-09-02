using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.EconomicManagement;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Models.ViewModels.Public;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class PublicPortalService : IPublicPortalService
    {
        private readonly AppDbContext _context;
        private readonly IExamService _exams;
        private readonly IBannerService _banners;
        private readonly ISponsorService _sponsors;
        private readonly IAnnouncementService _announcements;

        public PublicPortalService(AppDbContext context, IExamService exams, IBannerService banners, ISponsorService sponsors, IAnnouncementService announcements)
        {
            _context = context;
            _exams = exams;
            _banners = banners;
            _sponsors = sponsors;
            _announcements = announcements;
        }

        public async Task<HomeViewModel> GetHomeAsync(CancellationToken ct = default)
        {
            var activeExams = await _exams.GetActiveExamsAsync();
            var activeBanners = await _banners.GetActiveBannersAsync();
            var activeSponsors = await _sponsors.GetActiveSponsorsAsync(ct);
            var activeAnnouncements = await _announcements.GetActiveAnnouncementsAsync(ct);
            var (prospects, regulations, syllabi, otherFiles) = await GetPublicDocumentsAsync(ct);

            var careers = await _context.Careers
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(ct);

            return new HomeViewModel
            {
                ActiveExams = activeExams,
                Prospects = prospects,
                Regulations = regulations,
                Syllabi = syllabi,
                OtherFiles = otherFiles,
                BannerTitle = "Bienvenido al Portal de Admisión",
                BannerSubtitle = "Tu futuro comienza aquí",
                BannerDescription = "Descubre las oportunidades que tenemos para ti y da el primer paso hacia tu carrera profesional.",
                BannerCtaText = "Ver Exámenes",
                BannerCtaUrl = "/exam",
                Banners = activeBanners,
                Careers = careers,
                Sponsors = activeSponsors,
                Announcements = activeAnnouncements
            };
        }

        public async Task<DocumentsPageViewModel?> GetDocumentsPageAsync(string category, CancellationToken ct = default)
        {
            var (prospects, regulations, syllabi, otherFiles) = await GetPublicDocumentsAsync(ct);

            return (category ?? string.Empty).ToLowerInvariant() switch
            {
                "prospectos" or "prospecto" => new DocumentsPageViewModel
                {
                    Slug = "prospectos",
                    Title = "Prospectos",
                    Subtitle = "Prospecto Universitario",
                    Description = "Versión digital del Prospecto Universitario vigente y de procesos de admisión anteriores.",
                    Icon = "ti ti-school",
                    AccentPrimary = true,
                    Items = prospects
                },
                "reglamento" or "reglamentos" => new DocumentsPageViewModel
                {
                    Slug = "reglamento",
                    Title = "Reglamento",
                    Subtitle = "Normativa oficial",
                    Description = "Reglamento General de Admisión vigente de la Universidad Nacional Amazónica de Madre de Dios.",
                    Icon = "ti ti-gavel",
                    AccentPrimary = false,
                    Items = regulations
                },
                "temarios" or "temario" => new DocumentsPageViewModel
                {
                    Slug = "temarios",
                    Title = "Temarios",
                    Subtitle = "Contenido del examen",
                    Description = "Temas que necesitas conocer para los exámenes de admisión: Lenguaje, Literatura, Aritmética, Física, Química y más.",
                    Icon = "ti ti-book",
                    AccentPrimary = true,
                    Items = syllabi
                },
                "otros" or "otros-documentos" => new DocumentsPageViewModel
                {
                    Slug = "otros",
                    Title = "Requisitos y Formatos por Modalidad de Ingreso",
                    Subtitle = "Formatos y archivos complementarios",
                    Description = "Consulta los requisitos y descarga los formatos según tu modalidad de postulación.",
                    Icon = "ti ti-folder-open",
                    AccentPrimary = false,
                    Items = otherFiles
                },
                _ => null
            };
        }

        public async Task<ResultsPublicViewModel> GetResultsAsync(Guid? termId, CancellationToken ct = default)
        {
            var terms = await _context.Terms
                .AsNoTracking()
                .OrderByDescending(t => t.Year).ThenByDescending(t => t.Number)
                .ToListAsync(ct);

            var selected = termId.HasValue
                ? terms.FirstOrDefault(t => t.Id == termId.Value)
                : terms.FirstOrDefault(t => t.IsActive) ?? terms.FirstOrDefault();

            var vm = new ResultsPublicViewModel { Terms = terms, SelectedTerm = selected };

            if (selected != null)
            {
                vm.Items = await _context.ExamResults
                    .AsNoTracking()
                    .Include(r => r.Modality)
                    .Where(r => r.IsActive && r.TermId == selected.Id && !string.IsNullOrEmpty(r.FileUrl))
                    .OrderByDescending(r => r.PublishedAt ?? r.CreatedAt)
                    .Select(r => new ResultItem
                    {
                        Id = r.Id,
                        Title = r.Name,
                        Description = r.Description,
                        ModalityName = r.Modality != null ? r.Modality.Name : "",
                        FileUrl = r.FileUrl,
                        PublishedAt = r.PublishedAt ?? r.CreatedAt
                    })
                    .ToListAsync(ct);
            }

            return vm;
        }

        public async Task<VacanciesPublicViewModel> GetVacanciesAsync(Guid? termId, CancellationToken ct = default)
        {
            var selected = await _context.Terms.AsNoTracking()
                .FirstOrDefaultAsync(t => t.IsActive, ct);
            var resolvacancies = await _context.Configs.AsNoTracking()
                .Where(r => r.Key == ConfigGeneral.ResolVacancies)
                .Select(r => r.Value)
                .FirstOrDefaultAsync(ct);

            var vm = new VacanciesPublicViewModel { SelectedTerm = selected };
            if (selected == null) return vm;

            var modalities = await _context.Modalities
                .AsNoTracking()
                .Where(m => m.TermId == selected.Id && !m.IsMockExam)
                .OrderBy(m => m.DisplayOrder)
                .ThenBy(m => m.Name)
                .ToListAsync(ct);

            var typeModalities = await _context.TypeModalities
                .AsNoTracking()
                .Where(tm => tm.Modality != null && tm.Modality.TermId == selected.Id)
                .OrderBy(tm => tm.Name)
                .ToListAsync(ct);

            var typesByModality = typeModalities
                .GroupBy(tm => tm.ModalityId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var modalityGroups = new List<ModalityColumnGroup>();
            var columns = new List<VacancyColumn>();
            foreach (var modality in modalities)
            {
                if (typesByModality.TryGetValue(modality.Id, out var types) && types.Count > 0)
                {
                    modalityGroups.Add(new ModalityColumnGroup
                    {
                        ModalityId = modality.Id,
                        ModalityName = modality.Name,
                        ColumnCount = types.Count,
                        HasSubHeaders = true
                    });
                    foreach (var tm in types)
                    {
                        columns.Add(new VacancyColumn
                        {
                            ModalityId = modality.Id,
                            TypeModalityId = tm.Id,
                            Header = tm.Name
                        });
                    }
                }
                else
                {
                    modalityGroups.Add(new ModalityColumnGroup
                    {
                        ModalityId = modality.Id,
                        ModalityName = modality.Name,
                        ColumnCount = 1,
                        HasSubHeaders = false
                    });
                    columns.Add(new VacancyColumn
                    {
                        ModalityId = modality.Id,
                        TypeModalityId = null,
                        Header = modality.Name
                    });
                }
            }

            var rawVacancies = await _context.Vacancies
                .AsNoTracking()
                .Include(v => v.Career).ThenInclude(c => c!.Faculty)
                .Where(v => v.Modality != null && v.Modality.TermId == selected.Id)
                .ToListAsync(ct);

            var activeModalityIds = rawVacancies
                .Where(v => v.Quantity > 0)
                .Select(v => v.ModalityId)
                .Distinct()
                .ToHashSet();

            modalityGroups = modalityGroups
                .Where(mg => activeModalityIds.Contains(mg.ModalityId))
                .ToList();

            columns = columns
                .Where(c => activeModalityIds.Contains(c.ModalityId))
                .ToList();

            vm.ResolVacancies = resolvacancies ?? "";

            vm.ModalityGroups = modalityGroups;
            vm.Columns = columns;

            vm.Faculties = rawVacancies
                .Where(v => v.Career?.Faculty != null)
                .GroupBy(v => v.Career!.Faculty!.Id)
                .Select(fg =>
                {
                    var faculty = fg.First().Career!.Faculty!;
                    return new FacultyVacancies
                    {
                        Id = faculty.Id,
                        Name = faculty.Name,
                        Icon = "ti ti-book",
                        Careers = fg
                            .GroupBy(v => v.Career!.Id)
                            .Select(cg =>
                            {
                                var career = cg.First().Career!;
                                var values = columns.Select(col =>
                                    cg.Where(v => v.ModalityId == col.ModalityId
                                                  && v.TypeModalityId == col.TypeModalityId)
                                      .Sum(v => v.Quantity)
                                ).ToList();
                                return new CareerVacancyRow
                                {
                                    CareerName = career.Name,
                                    Values = values,
                                    Total = values.Sum()
                                };
                            })
                            .OrderBy(r => r.CareerName)
                            .ToList()
                    };
                })
                .OrderBy(f => f.Name)
                .ToList();

            vm.TotalVacancies = vm.Faculties.Sum(f => f.Careers.Sum(c => c.Total));
            return vm;
        }

        public async Task<CareersPublicViewModel> GetCareersAsync(CancellationToken ct = default)
        {
            var faculties = await _context.Faculties
                .AsNoTracking()
                .Include(f => f.Careers)
                .OrderBy(f => f.Name)
                .ToListAsync(ct);

            return new CareersPublicViewModel
            {
                Faculties = faculties.Select(f => new FacultyCareers
                {
                    Id = f.Id,
                    Name = f.Name,
                    Careers = (f.Careers ?? new List<Career>())
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.DisplayOrder)
                        .ThenBy(c => c.Name)
                        .ToList()
                })
                .Where(f => f.Careers.Any())
                .ToList()
            };
        }

        public async Task<CareerDetailResult?> GetCareerDetailAsync(Guid id, CancellationToken ct = default)
        {
            var career = await _context.Careers
                .AsNoTracking()
                .Include(c => c.Faculty)
                .Include(c => c.Images!.OrderBy(i => i.DisplayOrder))
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive, ct);

            if (career == null) return null;

            var latestTerm = await _context.Terms
                .AsNoTracking()
                .OrderByDescending(t => t.Year).ThenByDescending(t => t.Number)
                .FirstOrDefaultAsync(ct);

            var totalVacancies = 0;
            if (latestTerm != null)
            {
                totalVacancies = await _context.Vacancies
                    .AsNoTracking()
                    .Where(v => v.CareerId == career.Id
                                && v.Modality != null
                                && v.Modality.TermId == latestTerm.Id)
                    .SumAsync(v => (int?)v.Quantity, ct) ?? 0;
            }

            return new CareerDetailResult(career, latestTerm, totalVacancies);
        }

        public async Task<ScheduleViewModel> GetScheduleAsync(Guid? termId, CancellationToken ct = default)
        {
            var selected = await _context.Terms.AsNoTracking()
                .FirstOrDefaultAsync(t => t.IsActive, ct);

            var vm = new ScheduleViewModel { SelectedTerm = selected };
            if (selected == null) return vm;

            var events = await _context.ScheduleEvents
                .AsNoTracking()
                .Where(e => e.TermId == selected.Id && e.IsActive)
                .OrderBy(e => e.DisplayOrder)
                .ThenBy(e => e.StartDate)
                .ToListAsync(ct);

            var groups = new List<SchedulePhaseGroup>();
            for (int i = 0; i < AppConstants.SchedulePhase.Order.Length; i++)
            {
                var key = AppConstants.SchedulePhase.Order[i];
                var items = events.Where(e => e.Phase == key).ToList();
                if (!items.Any()) continue;

                groups.Add(new SchedulePhaseGroup
                {
                    PhaseKey = key,
                    Label = AppConstants.SchedulePhase.Labels.TryGetValue(key, out var l) ? l : key,
                    Icon = AppConstants.SchedulePhase.Icons.TryGetValue(key, out var ic) ? ic : "ti ti-circle",
                    Accent = i % 2 == 0,
                    Location = items.FirstOrDefault()?.Location ?? string.Empty,
                    Items = items
                });
            }
            vm.Phases = groups;
            return vm;
        }

        public async Task<ModalityPublicViewModel> GetModalityAsync(Guid? termId, CancellationToken ct = default)
        {
            var selected = await _context.Terms.AsNoTracking()
                .FirstOrDefaultAsync(t => t.IsActive, ct);

            var vm = new ModalityPublicViewModel { SelectedTerm = selected };
            if (selected == null) return vm;

            var modalities = await _context.Modalities
                .AsNoTracking()
                .Where(m => m.TermId == selected.Id)
                .OrderBy(m => m.DisplayOrder)
                .ThenBy(m => m.Name)
                .ToListAsync(ct);

            var modalityIds = modalities.Select(m => m.Id).ToList();

            var requisites = await _context.ModalityRequisites
                .AsNoTracking()
                .Include(mr => mr.FileRequirementManagement)
                .Where(mr => modalityIds.Contains(mr.ModalityId))
                .ToListAsync(ct);

            var typeModalities = await _context.TypeModalities
                .AsNoTracking()
                .Where(t => t.IsActive && modalityIds.Contains(t.ModalityId))
                .OrderBy(t => t.Name)
                .ToListAsync(ct);

            var paymentAssociations = await _context.PaymentCodesModalities
                .AsNoTracking()
                .Include(pcm => pcm.PaymentCode)
                .Include(pcm => pcm.TypeModality)
                .Where(pcm => pcm.IsActive
                              && pcm.PaymentCode != null
                              && pcm.PaymentCode.IsActive
                              && pcm.PaymentCode.TermId == selected.Id
                              && pcm.ModalityId != null
                              && modalityIds.Contains(pcm.ModalityId.Value))
                .ToListAsync(ct);

            var publicInfos = await _context.PublicInfos
                .AsNoTracking()
                .Where(p => p.IsActive
                            && p.TermId == selected.Id
                            && p.ModalityId != null
                            && modalityIds.Contains(p.ModalityId.Value))
                .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Title)
                .ToListAsync(ct);

            ModalityCard BuildCard(Modality m, TypeModality? t)
            {
                var reqs = requisites
                    .Where(r => r.ModalityId == m.Id
                                && r.FileRequirementManagement != null
                                && (r.TypeModalityId == null
                                    || (t != null && r.TypeModalityId == t.Id)))
                    .Select(r => r.FileRequirementManagement!)
                    .DistinctBy(r => r.Id)
                    .ToList();

                var modalityPayments = paymentAssociations
                    .Where(p => p.ModalityId == m.Id)
                    .ToList();

                IEnumerable<PaymentCodeModality> filtered;
                if (t != null)
                {
                    var typePayments = modalityPayments.Where(p => p.TypeModalityId == t.Id).ToList();
                    filtered = typePayments.Count != 0
                        ? typePayments
                        : modalityPayments.Where(p => p.TypeModalityId == null);
                }
                else
                {
                    filtered = modalityPayments.Where(p => p.TypeModalityId == null);
                }

                var payments = filtered
                    .OrderBy(p => p.PaymentCode?.Description)
                    .Select(p => new PaymentRequirement
                    {
                        Code = p.PaymentCode?.Code ?? string.Empty,
                        Concept = string.IsNullOrWhiteSpace(p.PaymentCode?.Description)
                            ? "Derecho de inscripción"
                            : p.PaymentCode!.Description,
                        Amount = p.Amount
                    })
                    .ToList();

                var infos = publicInfos
                    .Where(p => p.ModalityId == m.Id)
                    .Select(p => new PublicInfoBlock
                    {
                        Title = p.Title,
                        Description = p.Description,
                        Url = p.Url
                    })
                    .ToList();

                var typeDesc = t != null && !string.IsNullOrWhiteSpace(t.Description) ? t.Description : null;
                var modalityDesc = !string.IsNullOrWhiteSpace(m.PublicSummary) ? m.PublicSummary : m.Description;
                var summary = typeDesc ?? modalityDesc ?? string.Empty;

                return new ModalityCard
                {
                    Id = t?.Id ?? m.Id,
                    Slug = Slugify(t != null ? $"{m.Name}-{t.Name}" : m.Name),
                    TypeName = t?.Name ?? string.Empty,
                    ModalityName = m.Name,
                    Summary = summary,
                    IconKey = string.IsNullOrWhiteSpace(m.IconKey) ? "ti ti-file-text" : m.IconKey,
                    Badge = m.Badge,
                    IsActive = m.IsActive,
                    DiscountPercentage = t?.DiscountPercentage ?? 0m,
                    ExamDate = m.ExamDate,
                    ResultsPublicationDate = m.ResultsPublicationDate,
                    PostulationRequirements = reqs
                        .Where(r => r.Stage == AppConstants.RequirementStage.Postulation
                                 || r.Stage == AppConstants.RequirementStage.Both)
                        .Select(r => r.Name).ToList(),
                    EntryRequirements = reqs
                        .Where(r => r.Stage == AppConstants.RequirementStage.Entry
                                 || r.Stage == AppConstants.RequirementStage.Both)
                        .Select(r => r.Name).ToList(),
                    PaymentRequirements = payments,
                    InfoBlocks = infos
                };
            }

            var cards = new List<ModalityCard>();
            foreach (var m in modalities)
            {
                var myTypes = typeModalities.Where(t => t.ModalityId == m.Id).ToList();
                if (myTypes.Any())
                {
                    foreach (var t in myTypes)
                        cards.Add(BuildCard(m, t));
                }
                else
                {
                    cards.Add(BuildCard(m, null));
                }
            }

            vm.Modalities = cards;
            return vm;
        }

        // ========== Helpers ==========

        private async Task<(
            List<DocumentViewModel> Prospects,
            List<DocumentViewModel> Regulations,
            List<DocumentViewModel> Syllabi,
            List<DocumentViewModel> OtherFiles)> GetPublicDocumentsAsync(CancellationToken ct)
        {
            var prospects = await _context.Prospects
                .AsNoTracking()
                .Include(p => p.Term)
                .Where(p => p.IsActive && !string.IsNullOrEmpty(p.FileUrl))
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new DocumentViewModel
                {
                    Title = p.Name,
                    Description = p.Description,
                    FileUrl = p.FileUrl,
                    FileName = p.FileName,
                    FileType = p.FileType,
                    FileSize = p.FileSize,
                    Kind = "Prospecto",
                    Badge = p.Term != null ? p.Term.Name : string.Empty
                })
                .ToListAsync(ct);

            var rawOtherFiles = await _context.OtherFiles
                .AsNoTracking()
                .Where(f => f.IsActive && !string.IsNullOrEmpty(f.FileUrl))
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new { f.Name, f.Description, f.FileUrl, f.FileName, f.FileType, f.FileSize, f.Category })
                .ToListAsync(ct);

            static DocumentViewModel Map(dynamic f, string kind) => new DocumentViewModel
            {
                Title = f.Name,
                Description = f.Description,
                FileUrl = f.FileUrl,
                FileName = f.FileName,
                FileType = f.FileType,
                FileSize = f.FileSize,
                Kind = kind,
                Badge = string.Empty
            };

            var regulations = rawOtherFiles
                .Where(f => f.Category == AppConstants.OtherFileCategory.Reglamento)
                .Select(f => Map(f, "Reglamento"))
                .ToList();

            var syllabi = rawOtherFiles
                .Where(f => f.Category == AppConstants.OtherFileCategory.Temario)
                .Select(f => Map(f, "Temario"))
                .ToList();

            var others = rawOtherFiles
                .Where(f => f.Category != AppConstants.OtherFileCategory.Reglamento
                         && f.Category != AppConstants.OtherFileCategory.Temario)
                .Select(f => Map(f, "Archivo"))
                .ToList();

            return (prospects, regulations, syllabi, others);
        }

        private static string Slugify(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return Guid.NewGuid().ToString("N")[..8];
            var s = input.ToLowerInvariant();
            var sb = new System.Text.StringBuilder();
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
                else if (ch == ' ' || ch == '-' || ch == '_') sb.Append('-');
            }
            var slug = sb.ToString().Trim('-');
            while (slug.Contains("--")) slug = slug.Replace("--", "-");
            return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
        }
    }
}
