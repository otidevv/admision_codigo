using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Exam;
using ADMISION.ENTITIES.Models.Postulante;
using ADMISION.Models.ViewModels.Reports;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class SiriesReportService : ISiriesReportService
    {
        private readonly AppDbContext _context;

        public SiriesReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SiriesReportViewModel> BuildAsync(SiriesReportFilter filter, CancellationToken ct = default)
        {
            var vm = new SiriesReportViewModel { TermId = filter.TermId };

            if (!filter.TermId.HasValue) return vm;

            var termId = filter.TermId.Value;
            var term = await _context.Terms.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == termId, ct);

            if (term == null) return vm;

            vm.TermName = term.Name;

            var latestVersion = await _context.ConsolidadoIngresantesVersions
                .AsNoTracking()
                .Where(v => v.TermId == termId && v.IsLatest)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(ct);

            // Base: todas las inscripciones del período, excluyendo simulacros.
            var inscriptions = await _context.Inscriptions
                .AsNoTracking()
                .Include(i => i.Modality)
                .Include(i => i.Career)
                .Include(i => i.Postulant).ThenInclude(p => p!.User)
                .Include(i => i.Postulant).ThenInclude(p => p!.Disabilities).ThenInclude(d => d!.DisabilityType)
                .Where(i => i.Modality != null && i.Modality.TermId == termId && !i.Modality.IsMockExam)
                .OrderBy(i => i.Postulant!.User!.FullName)
                .ToListAsync(ct);

            if (inscriptions.Count == 0) return vm;

            var inscriptionIds = inscriptions.Select(i => i.Id).ToList();

            // Registros del consolidado de la última versión activa (por inscripción).
            var consolidadoByInscription = latestVersion == null
                ? new Dictionary<Guid, ConsolidadoIngresantesRecord>()
                : await _context.ConsolidadoIngresantesRecords.AsNoTracking()
                    .Where(r => r.VersionId == latestVersion.Id && r.InscriptionId.HasValue
                        && inscriptionIds.Contains(r.InscriptionId!.Value))
                    .GroupBy(r => r.InscriptionId!.Value)
                    .ToDictionaryAsync(g => g.Key, g => g.First(), ct);

            // Match CEPRE (por inscripción) para el caso CEPRE.
            var cepreMatchByInscription = await _context.CepreMatchRecords.AsNoTracking()
                .Where(m => m.TermId == termId && m.InscriptionId.HasValue
                    && inscriptionIds.Contains(m.InscriptionId!.Value))
                .GroupBy(m => m.InscriptionId!.Value)
                .ToDictionaryAsync(g => g.Key, g => g.First(), ct);

            // Ciclo por DNI a partir del importado CEPRE (filas con condición INGRESO, mayor ciclo).
            var cepreVersionIds = cepreMatchByInscription.Values
                .Select(m => m.CepreVersionId)
                .Distinct()
                .ToList();

            var cicloByDni = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (cepreVersionIds.Count > 0)
            {
                var cepreImports = await _context.CepreImportRecords.AsNoTracking()
                    .Where(r => cepreVersionIds.Contains(r.VersionId) && r.Dni != null)
                    .ToListAsync(ct);

                cicloByDni = cepreImports
                    .Where(r => IsIngreso(r.Estado))
                    .GroupBy(r => r.Dni!.Trim())
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(r => ParseCiclo(r.Ciclo)).First().Ciclo ?? "",
                        StringComparer.OrdinalIgnoreCase);
            }

            // Resultados de examen general (puntaje del registro de importación).
            var admissionByInscription = await _context.AdmissionResultImportRecords.AsNoTracking()
                .Where(r => r.TermId == termId && r.InscriptionId.HasValue
                    && inscriptionIds.Contains(r.InscriptionId!.Value))
                .GroupBy(r => r.InscriptionId!.Value)
                .ToDictionaryAsync(g => g.Key, g => g.First(), ct);

            // Configuración de tipos de postulante (Modalidad de Admisión) del período.
            var configs = await _context.PostulantTypeConfigs.AsNoTracking()
                .Where(c => c.TermId == termId)
                .ToListAsync(ct);

            // Carreras por código para la carrera de ingreso del consolidado.
            var careerByCode = await _context.Careers.AsNoTracking()
                .Where(c => c.IsActive)
                .ToDictionaryAsync(c => c.Code, c => c.Name, StringComparer.OrdinalIgnoreCase, ct);

            var items = new List<SiriesReportItem>(inscriptions.Count);

            foreach (var ins in inscriptions)
            {
                var user = ins.Postulant?.User;
                consolidadoByInscription.TryGetValue(ins.Id, out var cons);
                cepreMatchByInscription.TryGetValue(ins.Id, out var match);
                admissionByInscription.TryGetValue(ins.Id, out var admission);

                var esIngresante = cons != null;
                var config = ResolveConfig(ins, configs);

                var ciclo = string.Empty;
                if (match != null && user != null && !string.IsNullOrWhiteSpace(user.Document))
                    cicloByDni.TryGetValue(user.Document.Trim(), out ciclo);

                // Puntaje: para inscripciones no CEPRE se usa el valor de AdmissionResultImportRecord;
                // para el caso CEPRE se usa la nota final del match CEPRE.
                var esCepre = ins.Modality?.IsCepreExam == true;
                var puntaje = string.Empty;
                if (esCepre)
                {
                    if (match != null && match.NotaFinal.HasValue)
                        puntaje = match.NotaFinal.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (admission != null && !string.IsNullOrWhiteSpace(admission.Puntaje))
                {
                    puntaje = admission.Puntaje.Trim();
                }

                var carreraIngreso = string.Empty;
                if (esIngresante)
                {
                    carreraIngreso = !string.IsNullOrWhiteSpace(cons!.CodigoCarrera)
                        && careerByCode.TryGetValue(cons.CodigoCarrera, out var careerName)
                            ? careerName
                            : ins.Career?.Name ?? "";
                }

                var disabilityNames = ins.Postulant?.Disabilities?
                    .Where(d => d.DisabilityType != null)
                    .Select(d => d.DisabilityType!.Name)
                    .Distinct()
                    .ToList();

                items.Add(new SiriesReportItem
                {
                    TipoDocumento = user?.DocumentType ?? "",
                    NumeroDocumento = user?.Document ?? "",
                    ApellidoPaterno = user?.FirstNameFather ?? "",
                    ApellidoMaterno = user?.FirstNameMother ?? "",
                    Nombres = user?.Name ?? "",
                    Genero = user?.Genero ?? "",
                    FechaNacimiento = user != null ? user.Birthdate.ToString("dd/MM/yyyy") : "",
                    Discapacidad = disabilityNames is { Count: > 0 } ? string.Join(", ", disabilityNames) : "",
                    Periodo = match != null && !string.IsNullOrWhiteSpace(ciclo) ? ciclo : term.Name,
                    CarreraPrimeraOpcion = ins.Career?.Name ?? "",
                    ModalidadAdmision = config?.Description ?? "",
                    Puntaje = puntaje,
                    EsIngresante = esIngresante ? "SI" : "NO",
                    CarreraIngreso = carreraIngreso,
                    CorreoInstitucional = esIngresante && cons != null
                        ? $"{cons.CodigoEstudiante}@unamad.edu.pe"
                        : "",
                    CorreoPersonal = user?.Email ?? "",
                    Celular = user?.PhoneNumber ?? ""
                });
            }

            vm.Items = items;
            vm.TotalPostulantes = items.Count;
            vm.TotalIngresantes = items.Count(i => i.EsIngresante == "SI");
            return vm;
        }

        private static PostulantTypeConfig? ResolveConfig(Inscription ins, List<PostulantTypeConfig> configs)
        {
            var esCepre = ins.Modality?.IsCepreExam == true;

            return configs.FirstOrDefault(c =>
                (c.CareerId.HasValue && esCepre && c.CareerId.Value == ins.CareerId) ||
                (c.ModalityId.HasValue && ins.ModalityId.HasValue && c.ModalityId.Value == ins.ModalityId.Value) ||
                (c.TypeModalityId.HasValue && ins.TypeModalityId.HasValue && c.TypeModalityId.Value == ins.TypeModalityId.Value));
        }

        private static bool IsIngreso(string? estado)
        {
            if (string.IsNullOrWhiteSpace(estado)) return false;
            var normalized = RemoveDiacritics(estado).Trim().ToUpperInvariant();
            return string.Equals(normalized, "INGRESO", StringComparison.Ordinal);
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (var c in normalized)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        private static (int Year, int Period) ParseCiclo(string? ciclo)
        {
            if (string.IsNullOrWhiteSpace(ciclo))
                return (0, 0);

            var parts = ciclo.Trim().Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return (int.TryParse(parts.FirstOrDefault(), out var y) ? y : 0, 0);

            var year = int.TryParse(parts[0], out var yr) ? yr : 0;
            var periodStr = parts[1].Trim().ToUpperInvariant();
            var period = periodStr switch
            {
                "0" => 0,
                "I" => 1,
                "II" => 2,
                "III" => 3,
                "IV" => 4,
                "V" => 5,
                _ => int.TryParse(periodStr, out var p) ? p : 0
            };

            return (year, period);
        }
    }
}
