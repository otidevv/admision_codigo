using System.Globalization;
using System.Text;
using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.ENTITIES.Models.Postulante;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<Term>> GetTermsAsync(CancellationToken ct = default)
        {
            return await _context.Terms
                .AsNoTracking()
                .OrderByDescending(t => t.Year).ThenByDescending(t => t.Number)
                .ToListAsync(ct);
        }

        public async Task<AdminDashboardDto> BuildDashboardAsync(
            Guid termId,
            Guid? modalityId = null,
            Guid? typeModalityId = null,
            Guid? careerId = null,
            Guid? tematicAreaId = null,
            CancellationToken ct = default)
        {
            var term = await _context.Terms.AsNoTracking().FirstOrDefaultAsync(t => t.Id == termId, ct);
            if (term == null) return new AdminDashboardDto();

            // Si se filtra por área temática, primero resolvemos las carreras
            // mapeadas a esa área en este término (relación N:M en TematicAreaCareer).
            HashSet<Guid>? careersInTematicArea = null;
            if (tematicAreaId.HasValue)
            {
                careersInTematicArea = (await _context.TematicAreaCareers.AsNoTracking()
                    .Where(tac => tac.TermId == term.Id && tac.TematicAreaId == tematicAreaId.Value)
                    .Select(tac => tac.CareerId)
                    .ToListAsync(ct))
                    .ToHashSet();
            }

            // Inscripciones del término con todos los includes que necesita el dashboard.
            // Los filtros opcionales se aplican como predicados encadenados.
            var query = _context.Inscriptions
                .AsNoTracking()
                .Include(i => i.Postulant).ThenInclude(p => p!.User)
                .Include(i => i.Career).ThenInclude(c => c!.Faculty)
                .Include(i => i.Modality)
                .Include(i => i.TypeModality)
                .Include(i => i.School)
                .Include(i => i.Country)
                .Include(i => i.SourceUniversity)
                .Include(i => i.SourceCareer)
                .Include(i => i.Distrit).ThenInclude(d => d!.Province).ThenInclude(p => p!.Department)
                .Where(i => i.Modality != null && i.Modality.TermId == term.Id && i.State == AppConstants.InscripcionState.Aprobado);

            if (modalityId.HasValue)      query = query.Where(i => i.ModalityId == modalityId.Value);
            if (typeModalityId.HasValue)  query = query.Where(i => i.TypeModalityId == typeModalityId.Value);
            if (careerId.HasValue)        query = query.Where(i => i.CareerId == careerId.Value);
            if (careersInTematicArea != null)
                query = query.Where(i => careersInTematicArea.Contains(i.CareerId));

            var inscriptions = await query.ToListAsync(ct);

            var dto = new AdminDashboardDto
            {
                SelectedTermId = term.Id,
                SelectedTermName = term.Name,
                SelectedModalityId = modalityId,
                SelectedTypeModalityId = typeModalityId,
                SelectedCareerId = careerId,
                SelectedTematicAreaId = tematicAreaId,
                TotalPostulants = inscriptions.Count,
                ActiveCareers = inscriptions.Select(i => i.CareerId).Distinct().Count(),
                ActiveModalities = inscriptions.Select(i => i.ModalityId).Distinct().Count()
            };

            await PopulateFilterOptionsAsync(dto, term.Id, ct);

            // Edad promedio.
            var withUser = inscriptions.Where(i => i.Postulant?.User != null).ToList();
            dto.AvgAge = withUser.Any()
                ? withUser.Average(i => (DateTime.Now - i.Postulant!.User!.Birthdate.DateTime).TotalDays / 365.25)
                : 0;

            await PopulateTopicsAsync(dto, term.Id, inscriptions, ct);
            PopulateGender(dto, inscriptions);
            PopulateSchools(dto, inscriptions);
            PopulateAgeGroups(dto, inscriptions);
            PopulateGrades(dto, inscriptions);
            await PopulateDisabilityAsync(dto, inscriptions, ct);
            PopulateCharts(dto, inscriptions);
            PopulateMaps(dto, inscriptions);
            PopulateTraslados(dto, inscriptions);
            await PopulateRecentPaymentsAsync(dto, ct);

            return dto;
        }

        // ───────── Áreas temáticas (carreras agrupadas por área del término) ─────────
        private async Task PopulateTopicsAsync(AdminDashboardDto dto, Guid termId, List<Inscription> inscriptions, CancellationToken ct)
        {
            var termTopics = await _context.TematicAreaCareers
                .AsNoTracking()
                .Include(tac => tac.TematicArea)
                .Where(tac => tac.TermId == termId)
                .ToListAsync(ct);

            var areas = termTopics.Select(tac => tac.TematicArea).DistinctBy(ta => ta!.Id).ToList();
            dto.Topics.Total = inscriptions.Count;

            foreach (var area in areas)
            {
                if (area == null) continue;

                var careerIdsInArea = termTopics
                    .Where(tac => tac.TematicAreaId == area.Id)
                    .Select(tac => tac.CareerId)
                    .ToList();

                var count = inscriptions.Count(i => careerIdsInArea.Contains(i.CareerId));

                dto.Topics.Items.Add(new TopicStatItem
                {
                    Code = area.Code,
                    Count = count,
                    Percentage = dto.Topics.Total > 0 ? (double)count / dto.Topics.Total * 100 : 0
                });
            }
        }

        private static void PopulateGender(AdminDashboardDto dto, List<Inscription> inscriptions)
        {
            dto.Gender.Male = inscriptions.Count(i => i.Postulant?.User?.Genero == "M");
            dto.Gender.Female = inscriptions.Count(i => i.Postulant?.User?.Genero == "F");
        }

        private static void PopulateSchools(AdminDashboardDto dto, List<Inscription> inscriptions)
        {
            dto.Schools.Public = inscriptions.Count(i => IsPublicSchool(i));
            dto.Schools.Private = inscriptions.Count(i => IsPrivateSchool(i));
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static bool IsPublicSchool(Inscription i)
        {
            var st = RemoveDiacritics(i.SchoolType ?? "").ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(st))
                return st == "PUBLICO";
            return RemoveDiacritics(i.School?.Management ?? "").ToUpperInvariant() == "PUBLICO"
                   || (i.School?.Modality?.ToUpperInvariant().Contains("PUB") == true);
        }

        private static bool IsPrivateSchool(Inscription i)
        {
            var st = RemoveDiacritics(i.SchoolType ?? "").ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(st))
                return st == "PRIVADO";
            return RemoveDiacritics(i.School?.Management ?? "").ToUpperInvariant() == "PRIVADO"
                   || (i.School?.Modality?.ToUpperInvariant().Contains("PRI") == true);
        }

        private static void PopulateAgeGroups(AdminDashboardDto dto, List<Inscription> inscriptions)
        {
            foreach (var ins in inscriptions)
            {
                if (ins.Postulant?.User == null) continue;
                var age = (DateTime.Now - ins.Postulant.User.Birthdate.DateTime).TotalDays / 365.25;
                if (age <= 15) dto.AgeGroups.Children++;
                else if (age <= 25) dto.AgeGroups.Young++;
                else if (age <= 45) dto.AgeGroups.Adult++;
                else dto.AgeGroups.Senior++;
            }
        }

        // ───────── Grade distribution (solo inscripciones con grado) ─────────
        private static void PopulateGrades(AdminDashboardDto dto, List<Inscription> inscriptions)
        {
            var gradeGroups = inscriptions
                .Where(i => !string.IsNullOrWhiteSpace(i.EducationalLevel) && !string.IsNullOrWhiteSpace(i.Grade))
                .GroupBy(i =>
                {
                    var level = i.EducationalLevel.ToUpperInvariant() switch
                    {
                        "PRIMARIA" => "Prim.",
                        "SECUNDARIA" => "Sec.",
                        _ => "Otra"
                    };
                    return $"{i.Grade}° {level}";
                })
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderBy(x => x.Label)
                .ToList();

            dto.GradeDistribution.Labels = gradeGroups.Select(x => x.Label).ToList();
            dto.GradeDistribution.Values = gradeGroups.Select(x => x.Count).ToList();
        }

        // ───────── Discapacidad (grouping por keywords del nombre del tipo) ─────────
        private async Task PopulateDisabilityAsync(AdminDashboardDto dto, List<Inscription> inscriptions, CancellationToken ct)
        {
            var postulantIds = inscriptions
                .Where(i => i.PostulantId != Guid.Empty)
                .Select(i => i.PostulantId)
                .Distinct()
                .ToList();

            var disabilityRows = await _context.PostulantDisabilities
                .AsNoTracking()
                .Where(pd => postulantIds.Contains(pd.PostulantId))
                .Select(pd => new { pd.PostulantId, TypeName = pd.DisabilityType!.Name })
                .ToListAsync(ct);

            // Categorización dinámica: cada DisabilityType del catálogo se asigna
            // a UNA sola de las 4 categorías del gráfico. Se evalúa en orden de
            // especificidad para evitar dobles conteos (ej. "Sordoceguera" entra
            // a Auditiva por SORDO antes de mirar CEGUERA en Visual).
            static string? Categorize(string name)
            {
                var n = name.ToUpperInvariant();
                // Auditiva (incluye sordoceguera por su componente auditivo).
                if (n.Contains("AUDITI") || n.Contains("SORDO") || n.Contains("HIPOACUS")) return "AUDITORY";
                // Visual.
                if (n.Contains("VISUAL") || n.Contains("CEGUERA") || n.Contains("CIEGO") || n.Contains("BAJA VISI")) return "VISUAL";
                // Motora / Física (incluye talla baja, estenosis y enfermedades raras).
                if (n.Contains("MOTRI") || n.Contains("MOTOR") || n.Contains("FISIC") || n.Contains("FÍSIC")
                    || n.Contains("PARALI") || n.Contains("PARÁLI") || n.Contains("HEMIPL")
                    || n.Contains("TALLA") || n.Contains("ESTENOSIS") || n.Contains("VÁLVULA") || n.Contains("VALVULA")
                    || n.Contains("ENFERMEDAD") || n.Contains("RARAS") || n.Contains("CONGÉNIT") || n.Contains("CONGENIT"))
                    return "MOTOR";
                // Intelectual / cognitiva (autismo y derivados, trastornos mentales/aprendizaje, alta capacidad).
                if (n.Contains("INTELECT") || n.Contains("MENTAL") || n.Contains("AUTIS") || n.Contains("ASPERGER")
                    || n.Contains("DÉFICIT") || n.Contains("DEFICIT") || n.Contains("APRENDIZ") || n.Contains("COMPORTA")
                    || n.Contains("ESPECTRO") || n.Contains("NEURODESARRO") || n.Contains("COGNIT")
                    || n.Contains("TALENTO") || n.Contains("SUPERDOTAC") || n.Contains("ALTA CAPACID"))
                    return "INTELLECTUAL";
                // Multidiscapacidad → cuenta en todas las que apliquen; al no poder
                // distinguir, se asigna por defecto a Motora (criterio del SUNEDU
                // que la trata como física combinada).
                if (n.Contains("MULTIDISCAPACID")) return "MOTOR";
                return "OTHER"; // "Otros", "No Cuenta con Información" y catch-all.
            }

            var unique = disabilityRows.Select(r => r.PostulantId).Distinct().Count();
            var categorized = disabilityRows
                .Select(r => Categorize(r.TypeName))
                .GroupBy(c => c!)
                .ToDictionary(g => g.Key, g => g.Count());

            dto.Disability.Visual = categorized.GetValueOrDefault("VISUAL");
            dto.Disability.Auditory = categorized.GetValueOrDefault("AUDITORY");
            dto.Disability.Motor = categorized.GetValueOrDefault("MOTOR");
            dto.Disability.Intellectual = categorized.GetValueOrDefault("INTELLECTUAL");
            dto.Disability.Other = categorized.GetValueOrDefault("OTHER");
            dto.Disability.TotalUnique = unique;
        }

        // ───────── Charts (modalidades, carreras top-15, regiones) ─────────
        private static void PopulateCharts(AdminDashboardDto dto, List<Inscription> inscriptions)
        {
            var modGroups = inscriptions.GroupBy(i => i.Modality?.Name ?? "Otra")
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
            dto.ModalitiesChart.Labels = modGroups.Select(x => x.Label).ToList();
            dto.ModalitiesChart.Values = modGroups.Select(x => x.Count).ToList();

            var carGroups = inscriptions.GroupBy(i => i.Career?.Name ?? "Otra")
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(15)
                .ToList();
            dto.CareersChart.Labels = carGroups.Select(x => x.Label).ToList();
            dto.CareersChart.Values = carGroups.Select(x => x.Count).ToList();

            var regGroups = inscriptions.GroupBy(i => i.Distrit?.Province?.Department?.Name ?? "Otras")
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();
            dto.RegionsChart.Labels = regGroups.Select(x => x.Label).ToList();
            dto.RegionsChart.Values = regGroups.Select(x => x.Count).ToList();
        }

        // ───────── Mapas (Perú departamentos + mundo países) ─────────
        private static void PopulateMaps(AdminDashboardDto dto, List<Inscription> inscriptions)
        {
            dto.PeruMap = inscriptions
                .Where(i => i.Distrit?.Province?.Department != null)
                .GroupBy(i => new
                {
                    i.Distrit!.Province!.Department!.Name,
                    i.Distrit.Province.Department.Code
                })
                .Select(g => new DepartmentMapItem
                {
                    Name = g.Key.Name,
                    Code = g.Key.Code,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            dto.WorldMap = inscriptions
                .Where(i => i.Country != null
                            && !string.Equals(i.Country.Code, "PE", StringComparison.OrdinalIgnoreCase))
                .GroupBy(i => new { i.Country!.Name, i.Country.Code })
                .Select(g => new CountryMapItem
                {
                    Name = g.Key.Name,
                    Code = g.Key.Code,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .ToList();
        }

        // ───────── Traslados externos / internos ─────────
        private static void PopulateTraslados(AdminDashboardDto dto, List<Inscription> inscriptions)
        {
            static bool IsExternal(Inscription i)
            {
                var combined = $"{i.Modality?.Name} {i.TypeModality?.Name}".ToUpperInvariant();
                return combined.Contains("TRASLADO") && combined.Contains("EXTERNO");
            }
            static bool IsInternal(Inscription i)
            {
                var combined = $"{i.Modality?.Name} {i.TypeModality?.Name}".ToUpperInvariant();
                return combined.Contains("TRASLADO") && combined.Contains("INTERNO");
            }

            var externalRows = inscriptions
                .Where(IsExternal)
                .GroupBy(i => i.SourceUniversity?.Name
                           ?? (string.IsNullOrWhiteSpace(i.School?.Name) ? "Sin institución" : i.School!.Name))
                .Select(g => new UniversityTransferItem
                {
                    University = g.Key,
                    External = g.Count(),
                    Internal = 0
                });

            var internalRows = inscriptions
                .Where(IsInternal)
                .GroupBy(i => i.SourceCareer?.Name
                           ?? (string.IsNullOrWhiteSpace(i.Career?.Name) ? "Sin carrera" : i.Career!.Name + " (misma)"))
                .Select(g => new UniversityTransferItem
                {
                    University = g.Key,
                    External = 0,
                    Internal = g.Count()
                });

            dto.Traslados = externalRows
                .Concat(internalRows)
                .OrderByDescending(x => x.Total)
                .Take(10)
                .ToList();
        }

        // ───────── Opciones de filtros (modalidades, tipos, carreras, áreas temáticas) ─────────
        // Se acotan al término seleccionado. Las TypeModalities cargan también ModalityId
        // para permitir cascada client-side cuando el usuario elige una modalidad.
        private async Task PopulateFilterOptionsAsync(AdminDashboardDto dto, Guid termId, CancellationToken ct)
        {
            // Modalidades activas del término.
            var modalities = await _context.Modalities.AsNoTracking()
                .Where(m => m.TermId == termId)
                .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name)
                .Select(m => new { m.Id, m.Name })
                .ToListAsync(ct);

            dto.FilterOptions.Modalities = modalities
                .Select(m => new FilterOption { Id = m.Id, Name = m.Name })
                .ToList();

            var modalityIds = modalities.Select(m => m.Id).ToList();

            // TypeModalities cuyas modalidades pertenecen al término.
            dto.FilterOptions.TypeModalities = await _context.TypeModalities.AsNoTracking()
                .Where(tm => modalityIds.Contains(tm.ModalityId))
                .OrderBy(tm => tm.Name)
                .Select(tm => new FilterOptionTyped
                {
                    Id = tm.Id,
                    Name = tm.Name,
                    ParentId = tm.ModalityId
                })
                .ToListAsync(ct);

            // Carreras vinculadas al término vía TematicAreaCareer (universo real
            // de carreras disponibles en ese proceso de admisión).
            dto.FilterOptions.Careers = await _context.TematicAreaCareers.AsNoTracking()
                .Where(tac => tac.TermId == termId && tac.Career != null)
                .Select(tac => new FilterOption { Id = tac.Career!.Id, Name = tac.Career.Name })
                .Distinct()
                .OrderBy(c => c.Name)
                .ToListAsync(ct);

            // Áreas temáticas configuradas en el término.
            dto.FilterOptions.TematicAreas = await _context.TematicAreaCareers.AsNoTracking()
                .Where(tac => tac.TermId == termId && tac.TematicArea != null)
                .Select(tac => new FilterOption { Id = tac.TematicArea!.Id, Name = "Área " + tac.TematicArea.Code })
                .Distinct()
                .OrderBy(a => a.Name)
                .ToListAsync(ct);
        }

        // ───────── Pagos recientes ─────────
        private async Task PopulateRecentPaymentsAsync(AdminDashboardDto dto, CancellationToken ct)
        {
            var payments = await _context.Payments
                .AsNoTracking()
                .Include(p => p.Inscription).ThenInclude(i => i!.Postulant).ThenInclude(pos => pos!.User)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .ToListAsync(ct);

            dto.Transfers = payments.Select(p => new BankTransferItem
            {
                FullName = p.Inscription?.Postulant?.User?.FullName ?? "N/A",
                Email = p.Inscription?.Postulant?.User?.Email ?? "N/A",
                Dni = p.Inscription?.Postulant?.User?.Document ?? "N/A",
                OperationCode = p.OperationCode ?? "N/A",
                BankName = "Banco de la Nación",
                Amount = (double)p.Amount,
                Date = p.CreatedAt.DateTime
            }).ToList();
        }
    }
}
