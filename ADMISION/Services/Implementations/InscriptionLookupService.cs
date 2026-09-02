using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class InscriptionLookupService : IInscriptionLookupService
    {
        private readonly AppDbContext _context;

        public InscriptionLookupService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<InscriptionFormData> GetFormDataAsync(CancellationToken ct = default)
        {
            var modalities = await _context.Modalities
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .Select(m => new NamedOption(m.Id, m.Name))
                .ToListAsync(ct);

            var typePostulants = await _context.TypePostulantInscriptions
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .Select(t => new NamedOption(t.Id, t.Name))
                .ToListAsync(ct);

            var careers = await _context.Careers
                .AsNoTracking()
                .Include(c => c.Faculty)
                .OrderBy(c => c.Name)
                .Select(c => new CareerOption(c.Id, c.Name, c.Faculty!.Name))
                .ToListAsync(ct);

            var methodPayments = await _context.MethodPayments
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .Select(m => new NamedOption(m.Id, m.Name))
                .ToListAsync(ct);

            var countries = await _context.Countries
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new NamedOption(c.Id, c.Name))
                .ToListAsync(ct);

            var departments = await _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .Select(d => new NamedOption(d.Id, d.Name))
                .ToListAsync(ct);

            var disabilityTypes = await _context.DisabilityTypes
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .Select(t => new NamedOption(t.Id, t.Name))
                .ToListAsync(ct);

            var universities = await _context.Universities
                .AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Select(u => new NamedOption(u.Id, u.Name))
                .ToListAsync(ct);

            var careersAll = await _context.Careers
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new NamedOption(c.Id, c.Name))
                .ToListAsync(ct);

            return new InscriptionFormData(
                modalities, typePostulants, careers, methodPayments,
                countries, departments, disabilityTypes, universities, careersAll);
        }

        public async Task<DateTime> GetExamEndDateAsync(Guid? modalityId, CancellationToken ct = default)
        {
            var now = DateTime.Now;

            if (modalityId.HasValue)
            {
                var modality = await _context.Modalities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.Id == modalityId.Value, ct);
                if (modality != null)
                {
                    return modality.EndDate.ToDateTime(modality.EndTime);
                }
            }

            var activeModalities = await _context.Modalities
                .AsNoTracking()
                .Where(m => m.IsActive)
                .ToListAsync(ct);

            var futureModalities = activeModalities
                .Where(m => m.EndDate.ToDateTime(m.EndTime) >= now)
                .ToList();

            if (futureModalities.Any())
            {
                return futureModalities.Min(t => t.EndDate.ToDateTime(t.EndTime));
            }
            if (activeModalities.Any())
            {
                return activeModalities.Max(t => t.EndDate.ToDateTime(t.EndTime));
            }
            return DateTime.Now;
        }

        public async Task<UserAutofillData?> CheckUserAsync(string docType, string docNumber, CancellationToken ct = default)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.DocumentType == docType && u.Document == docNumber)
                .Select(u => new UserAutofillData(
                    u.Name,
                    u.FirstNameFather,
                    u.FirstNameMother,
                    u.Birthdate,
                    u.Email,
                    u.PhoneNumber,
                    u.Genero,
                    u.Address,
                    u.Postulant.Inscriptions
                        .OrderByDescending(i => i.CreatedAt)
                        .Select(i => i.CountryId)
                        .FirstOrDefault(),
                    u.Postulant.Inscriptions
                        .OrderByDescending(i => i.CreatedAt)
                        .Select(i => i.Distrit.Province.DepartmentId)
                        .FirstOrDefault(),
                    u.Postulant.Inscriptions
                        .OrderByDescending(i => i.CreatedAt)
                        .Select(i => i.Distrit.ProvinceId)
                        .FirstOrDefault(),
                    u.Postulant.Inscriptions
                        .OrderByDescending(i => i.CreatedAt)
                        .Select(i => i.DistritId)
                        .FirstOrDefault(),
                    u.Postulant.Inscriptions
                        .OrderByDescending(i => i.CreatedAt)
                        .Select(i => i.Distrit.Code)
                        .FirstOrDefault(),
                    u.Postulant.Inscriptions
                        .OrderByDescending(i => i.CreatedAt)
                        .Select(i => i.SchoolId)
                        .FirstOrDefault(),
                    u.Postulant.Inscriptions
                        .OrderByDescending(i => i.CreatedAt)
                        .Select(i => i.OtherSchool)
                        .FirstOrDefault(),
                    null,
                    u.Postulant.Inscriptions
                        .OrderByDescending(i => i.CreatedAt)
                        .Select(i => i.School.Distrit.Province.DepartmentId)
                        .FirstOrDefault(),
                    u.Postulant.Inscriptions
                        .OrderByDescending(i => i.CreatedAt)
                        .Select(i => i.School.Distrit.ProvinceId)
                        .FirstOrDefault(),
                    u.Postulant.Inscriptions
                        .OrderByDescending(i => i.CreatedAt)
                        .Select(i => i.School.DistritId)
                        .FirstOrDefault()
                ))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IReadOnlyList<TypeModalityWithKind>> GetTypeModalitiesAsync(Guid modalityId, CancellationToken ct = default)
        {
            var modality = await _context.Modalities
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == modalityId, ct);
            var modalityName = modality?.Name;

            var types = await _context.TypeModalities
                .AsNoTracking()
                .Where(t => t.ModalityId == modalityId && t.IsActive)
                .OrderBy(t => t.Name)
                .Select(t => new { t.Id, t.Name, t.DiscountPercentage })
                .ToListAsync(ct);

            return types.Select(t => new TypeModalityWithKind(
                t.Id, t.Name, t.DiscountPercentage, ClassifyTransferKind(modalityName, t.Name)
            )).ToList();
        }

        public async Task<ModalityDates?> GetModalityInfoAsync(Guid modalityId, CancellationToken ct = default)
        {
            return await _context.Modalities
                .AsNoTracking()
                .Where(m => m.Id == modalityId)
                .Select(m => new ModalityDates(
                    m.Id,
                    m.Name,
                    m.StartDate.ToString("yyyy-MM-dd"),
                    m.EndDate.ToString("yyyy-MM-dd"),
                    m.ExamDate.HasValue ? m.ExamDate.Value.ToString("yyyy-MM-dd") : null,
                    m.ResultsPublicationDate.HasValue ? m.ResultsPublicationDate.Value.ToString("yyyy-MM-dd") : null))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<IReadOnlyList<NamedOption>> GetUniversitiesAsync(CancellationToken ct = default)
        {
            return await _context.Universities
                .AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.Name)
                .Select(u => new NamedOption(u.Id, u.Name))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<NamedOption>> GetCareersListAsync(CancellationToken ct = default)
        {
            return await _context.Careers
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new NamedOption(c.Id, c.Name))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<SchoolOption>> GetSchoolsByDistrictAsync(Guid districtId, CancellationToken ct = default)
        {
            return await _context.Schools
                .AsNoTracking()
                .Where(s => s.DistritId == districtId)
                .OrderBy(s => s.Name)
                .Select(s => new SchoolOption(s.Id, s.Name, s.Management, s.Level))
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<RequirementOption>> GetRequirementsAsync(Guid modalityId, Guid? typeModalityId, Guid? typePostulantId, CancellationToken ct = default)
        {
            // Requirements from Modality/TypeModality (sin incluir tipo postulante)
            var modalityReqs = await _context.ModalityRequisites
                .AsNoTracking()
                .Where(m => m.ModalityId == modalityId && m.TypeModalityId == typeModalityId)
                .Include(m => m.FileRequirementManagement)
                .Select(m => m.FileRequirementManagement!)
                .ToListAsync(ct);

            if (!modalityReqs.Any() && typeModalityId != null)
            {
                modalityReqs = await _context.ModalityRequisites
                    .AsNoTracking()
                    .Where(m => m.ModalityId == modalityId && m.TypeModalityId == null)
                    .Include(m => m.FileRequirementManagement)
                    .Select(m => m.FileRequirementManagement!)
                    .ToListAsync(ct);
            }

            return modalityReqs
                .Where(r => r != null && r.Stage != AppConstants.RequirementStage.Entry)
                .Select(r => new RequirementOption(r.Id, r.Id, r.Name))
                .ToList();
        }

        public async Task<RequirementOption?> GetTypePostulantRequirementAsync(Guid typePostulantId, CancellationToken ct = default)
        {
            var req = await _context.TypePostulantRequisites
                .AsNoTracking()
                .Where(t => t.TypePostulantInscriptionId == typePostulantId)
                .Include(t => t.FileRequirementManagement)
                .Select(t => t.FileRequirementManagement)
                .FirstOrDefaultAsync(ct);
            if (req == null) return null;
            return new RequirementOption(req.Id, req.Id, req.Name);
        }

        public async Task<IReadOnlyList<InscriptionSearchResult>> FindByDocumentAsync(string docType, string docNumber, CancellationToken ct = default)
        {
            var activeTerm = await _context.Terms.AsNoTracking().FirstOrDefaultAsync(t => t.IsActive, ct);
            if (activeTerm == null) return Array.Empty<InscriptionSearchResult>();

            var peruTz = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, peruTz));

            var inscriptions = await _context.Inscriptions
                .AsNoTracking()
                .Include(i => i.Postulant!).ThenInclude(p => p.User)
                .Include(i => i.Career)
                .Include(i => i.Modality)
                .Include(i => i.TypeModality)
                .Include(i => i.Modality!.Term)
                .Include(i => i.FileSubmissions!).ThenInclude(f => f.FileRequirementManagement)
                .Include(i => i.Payments)
                .Include(i => i.Observations!)
                .Where(i => i.Postulant!.User!.DocumentType == docType
                         && i.Postulant!.User!.Document == docNumber
                         && i.Modality!.TermId == activeTerm.Id)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(ct);

            return inscriptions.Select(i =>
            {
                var canDownload = i.State == AppConstants.InscripcionState.Aprobado;
                var isModalityActive = i.Modality!.IsActive && i.Modality.EndDate >= today;

                var files = new List<FileValidationInfo>();

                if (i.FileSubmissions != null)
                {
                    foreach (var fs in i.FileSubmissions.OrderBy(f => f.FileRequirementManagement?.Name))
                    {
                        files.Add(new FileValidationInfo
                        {
                            Name = fs.FileRequirementManagement?.Name ?? "Requisito",
                            Kind = "requirement",
                            IsValidated = fs.IsValidated,
                            Observation = fs.ValidationNote
                        });
                    }
                }

                if (i.Payments != null)
                {
                    foreach (var pay in i.Payments.Where(p => !string.IsNullOrWhiteSpace(p.FilePath)))
                    {
                        files.Add(new FileValidationInfo
                        {
                            Name = $"Comprobante de pago ({pay.OperationCode})",
                            Kind = "payment",
                            IsValidated = pay.IsApproved,
                            Observation = pay.Observation
                        });
                    }
                }

                var observations = new List<ObservationInfo>();
                if (i.Observations != null)
                {
                    foreach (var obs in i.Observations.OrderByDescending(o => o.CreatedAt))
                    {
                        observations.Add(new ObservationInfo
                        {
                            Observation = obs.Observation,
                            CreatedAt = obs.CreatedAt,
                            CreatedBy = obs.CreatedBy
                        });
                    }
                }

                return new InscriptionSearchResult
                {
                    InscriptionId = i.Id,
                    CodePostulant = i.CodePostulant,
                    FullName = i.Postulant!.User!.FullName ?? $"{i.Postulant.User.FirstNameFather} {i.Postulant.User.FirstNameMother}, {i.Postulant.User.Name}",
                    DocumentNumber = i.Postulant.User.Document,
                    DocumentType = i.Postulant.User.DocumentType,
                    CareerName = i.Career!.Name,
                    ModalityName = i.Modality.Name,
                    TypeModalityName = i.TypeModality != null ? i.TypeModality.Name : null,
                    TermName = i.Modality.Term.Name,
                    State = i.State,
                    InscriptionDate = i.CreatedAt.AddHours(-5),
                    CanDownload = canDownload,
                    IsModalityActive = isModalityActive,
                    IsMockExam = i.Modality!.IsMockExam,
                    Files = files,
                    Observations = observations
                };
            }).ToList();
        }

        public async Task<PaymentInfoResult> GetPaymentInfoAsync(Guid modalityId, Guid? typeModalityId, Guid? typePostulantId, CancellationToken ct = default)
        {
            var associations = await _context.PaymentCodesModalities
                .AsNoTracking()
                .Include(p => p.PaymentCode)
                .Where(p => p.IsActive && p.PaymentCode!.IsActive)
                .ToListAsync(ct);

            var bestMatch = associations.FirstOrDefault(p => p.ModalityId == modalityId && p.TypeModalityId == typeModalityId);
            if (bestMatch == null && typeModalityId.HasValue)
                bestMatch = associations.FirstOrDefault(p => p.ModalityId == null && p.TypeModalityId == typeModalityId);
            if (bestMatch == null)
                bestMatch = associations.FirstOrDefault(p => p.ModalityId == modalityId && p.TypeModalityId == null);

            if (bestMatch == null) return PaymentInfoResult.NoPayment();

            decimal modalityDiscount = 0;
            if (typeModalityId.HasValue)
            {
                var type = await _context.TypeModalities.AsNoTracking().FirstOrDefaultAsync(t => t.Id == typeModalityId.Value, ct);
                if (type != null) modalityDiscount = type.DiscountPercentage;
            }

            decimal postulantDiscount = 0;
            if (typePostulantId.HasValue)
            {
                var type = await _context.TypePostulantInscriptions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == typePostulantId.Value, ct);
                if (type != null) postulantDiscount = type.DiscountPercentage;
            }

            var maxDiscount = Math.Max(modalityDiscount, postulantDiscount);
            if (maxDiscount >= 100) return PaymentInfoResult.NoPayment();

            var finalAmount = bestMatch.Amount * (1 - (maxDiscount / 100));

            return new PaymentInfoResult
            {
                RequiresPayment = finalAmount > 0,
                BaseAmount = bestMatch.Amount,
                DiscountPercentage = maxDiscount,
                FinalAmount = finalAmount,
                ConceptDescription = bestMatch.PaymentCode?.Description,
                ConceptCode = bestMatch.PaymentCode?.Code
            };
        }

        public async Task<Dictionary<Guid, ModalityFlags>> GetModalityFlagsAsync(CancellationToken ct = default)
        {
            return await _context.Modalities
                .AsNoTracking()
                .Where(m => m.IsActive)
                .Select(m => new { m.Id, m.RequiresProfilePhoto, m.IsMockExam, m.RequiresSchoolType, m.RequiresEducationalLevel, m.RequiresGrade })
                .ToDictionaryAsync(m => m.Id, m => new ModalityFlags(m.RequiresProfilePhoto, m.IsMockExam, m.RequiresSchoolType, m.RequiresEducationalLevel, m.RequiresGrade), ct);
        }

        public async Task<Dictionary<Guid, List<Guid>>> GetModalityCareerMapAsync(CancellationToken ct = default)
        {
            return await _context.ModalityCareers
                .AsNoTracking()
                .GroupBy(mc => mc.ModalityId)
                .Select(g => new { ModalityId = g.Key, CareerIds = g.Select(mc => mc.CareerId).ToList() })
                .ToDictionaryAsync(k => k.ModalityId, v => v.CareerIds, ct);
        }

        public async Task<Dictionary<Guid, List<Guid>>> GetTypeModalityCareerMapAsync(CancellationToken ct = default)
        {
            return await _context.TypeModalityCareers
                .AsNoTracking()
                .GroupBy(tmc => tmc.TypeModalityId)
                .Select(g => new { TypeModalityId = g.Key, CareerIds = g.Select(tmc => tmc.CareerId).ToList() })
                .ToDictionaryAsync(k => k.TypeModalityId, v => v.CareerIds, ct);
        }

        // Heurística para clasificar tipos de modalidad de traslado.
        // Devuelve "external" | "internal" | "normal".
        private static string ClassifyTransferKind(string? modalityName, string? typeName)
        {
            var combined = ((modalityName ?? string.Empty) + " " + (typeName ?? string.Empty)).ToUpperInvariant();
            combined = combined.Replace('Á', 'A').Replace('É', 'E').Replace('Í', 'I').Replace('Ó', 'O').Replace('Ú', 'U');
            if (!combined.Contains("TRASLADO")) return "normal";
            if (combined.Contains("EXTERNO")) return "external";
            if (combined.Contains("INTERNO")) return "internal";
            return "normal";
        }
    }
}
