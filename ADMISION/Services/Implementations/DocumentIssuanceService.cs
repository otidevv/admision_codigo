using System.Globalization;
using System.IO.Compression;
using System.Text;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Exam;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class DocumentIssuanceService : IDocumentIssuanceService
    {
        private readonly AppDbContext _context;
        private readonly IDocumentService _documents;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<DocumentIssuanceService> _logger;

        public DocumentIssuanceService(AppDbContext context, IDocumentService documents, IWebHostEnvironment env, ILogger<DocumentIssuanceService> logger)
        {
            _context = context;
            _documents = documents;
            _env = env;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ConsolidadoRow>> GetIngresantesAsync(Guid versionId, CancellationToken ct = default)
        {
            return await LoadOrderedRowsAsync(versionId, ct);
        }

        public async Task<ConsolidadoRow?> GetIngresanteByIdAsync(Guid recordId, CancellationToken ct = default)
        {
            var versionId = await _context.ConsolidadoIngresantesRecords
                .AsNoTracking()
                .Where(r => r.Id == recordId)
                .Select(r => r.VersionId)
                .FirstOrDefaultAsync(ct);

            if (versionId == Guid.Empty) return null;

            var rows = await LoadOrderedRowsAsync(versionId, ct);
            return rows.FirstOrDefault(r => r.Id == recordId);
        }

        /// <summary>
        /// Lista las filas del consolidado ordenadas por nombre de carrera (A-Z) y luego por
        /// código de estudiante ascendente, asignando un numeral secuencial (1..N) que se usa
        /// tanto en la vista como en el nombre del PDF emitido.
        /// </summary>
        private async Task<IReadOnlyList<ConsolidadoRow>> LoadOrderedRowsAsync(Guid versionId, CancellationToken ct)
        {
            var records = await _context.ConsolidadoIngresantesRecords
                .AsNoTracking()
                .Where(r => r.VersionId == versionId)
                .ToListAsync(ct);

            var version = await _context.ConsolidadoIngresantesVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == versionId, ct);

            string? termName = version != null
                ? await _context.Terms.AsNoTracking()
                    .Where(t => t.Id == version.TermId)
                    .Select(t => t.Name)
                    .FirstOrDefaultAsync(ct)
                : null;

            var careerCodes = records
                .Select(r => r.CodigoCarrera ?? "")
                .Where(c => c.Length > 0)
                .Distinct()
                .ToList();

            var careerNames = careerCodes.Count > 0
                ? await _context.Careers.AsNoTracking()
                    .Where(c => careerCodes.Contains(c.Code))
                    .Select(c => new { c.Code, c.Name })
                    .ToDictionaryAsync(c => c.Code, c => c.Name, ct)
                : new Dictionary<string, string>();

            return records
                .OrderBy(r => ResolveCareerName(r.CodigoCarrera ?? "", careerNames), StringComparer.OrdinalIgnoreCase)
                .ThenBy(r => r.CodigoEstudiante ?? "", StringComparer.OrdinalIgnoreCase)
                .Select((r, i) => new ConsolidadoRow
                {
                    Id = r.Id,
                    InscriptionId = r.InscriptionId,
                    Nro = i + 1,
                    CodigoEstudiante = r.CodigoEstudiante ?? "",
                    Nombres = r.Nombres ?? "",
                    Paterno = r.Paterno ?? "",
                    Materno = r.Materno ?? "",
                    DNI = r.DNI ?? "",
                    CodigoCarrera = r.CodigoCarrera ?? "",
                    CareerName = ResolveCareerName(r.CodigoCarrera ?? "", careerNames),
                    SegundaCarrera = r.SegundaCarrera,
                    Email = r.Email,
                    Celular = r.Celular,
                    Sexo = r.Sexo,
                    TipoPostulante = r.TipoPostulante,
                    TermId = version?.TermId ?? Guid.Empty,
                    TermName = termName
                })
                .ToList();
        }

        private static string ResolveCareerName(string careerCode, Dictionary<string, string> careerNames)
        {
            return careerNames.TryGetValue(careerCode, out var name) ? name : careerCode;
        }

        public async Task<DocumentIssueResult> IssueIndividualAsync(Guid recordId, bool watermark, string? actor, CancellationToken ct = default)
        {
            var row = await GetIngresanteByIdAsync(recordId, ct);
            if (row == null) return new DocumentIssueResult { NotFound = true };

            var model = await BuildModelAsync(row, watermark, ct);
            var result = await _documents.GenerateConstanciaIngresoPdfAsync(model, new DocumentOptions
            {
                FileName = BuildFileName(row)
            }, actor);

            return new DocumentIssueResult
            {
                Success = true,
                PdfBytes = result.PdfBytes,
                FileName = result.FileName
            };
        }

        public async Task<DocumentIssueResult> IssueBulkAsync(List<Guid> recordIds, bool watermark, string? actor, CancellationToken ct = default)
        {
            if (recordIds == null || recordIds.Count == 0)
                return new DocumentIssueResult { NotFound = true };

            var found = await _context.ConsolidadoIngresantesRecords
                .AsNoTracking()
                .Where(r => recordIds.Contains(r.Id))
                .Select(r => new { r.Id, r.VersionId })
                .ToDictionaryAsync(r => r.Id, r => r.VersionId, ct);

            var versionCache = new Dictionary<Guid, IReadOnlyList<ConsolidadoRow>>();
            var rows = new List<ConsolidadoRow>();

            foreach (var id in recordIds)
            {
                if (!found.TryGetValue(id, out var versionId)) continue;

                if (!versionCache.TryGetValue(versionId, out var ordered))
                {
                    ordered = await LoadOrderedRowsAsync(versionId, ct);
                    versionCache[versionId] = ordered;
                }

                var row = ordered.FirstOrDefault(r => r.Id == id);
                if (row != null) rows.Add(row);
            }

            if (rows.Count == 0)
                return new DocumentIssueResult { NotFound = true };

            var zipBuffer = new MemoryStream();
            int errors = 0;
            using (var archive = new ZipArchive(zipBuffer, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var row in rows)
                {
                    try
                    {
                        var model = await BuildModelAsync(row, watermark, ct);
                        var result = await _documents.GenerateConstanciaIngresoPdfAsync(model, new DocumentOptions
                        {
                            FileName = BuildFileName(row)
                        }, actor);

                        var entry = archive.CreateEntry(result.FileName, CompressionLevel.Optimal);
                        await using var entryStream = entry.Open();
                        await entryStream.WriteAsync(result.PdfBytes, ct);
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        _logger.LogError(ex,
                            "Error emitiendo constancia para registro {RecordId} ({FullName})",
                            row.Id, row.FullName);
                    }
                }

                if (errors > 0)
                {
                    var errEntry = archive.CreateEntry("_errores.txt", CompressionLevel.Optimal);
                    await using var errStream = errEntry.Open();
                    await using var sw = new StreamWriter(errStream);
                    await sw.WriteAsync($"Se generaron {rows.Count - errors} de {rows.Count} documentos. {errors} fallaron — revisar logs del servidor.");
                }
            }

            zipBuffer.Position = 0;
            var termName = rows.FirstOrDefault()?.TermName ?? "Term";

            return new DocumentIssueResult
            {
                Success = true,
                ZipBytes = zipBuffer.ToArray(),
                FileName = $"ingresantes_{Sanitize(termName)}.zip",
                TotalCount = rows.Count,
                SuccessCount = rows.Count - errors,
                ErrorCount = errors
            };
        }

        private async Task<ConstanciaIngresoModel> BuildModelAsync(ConsolidadoRow row, bool watermark, CancellationToken ct)
        {
            string careerName = string.IsNullOrEmpty(row.CareerName) ? row.CodigoCarrera : row.CareerName;

            var (modalityName, termName) = await ResolveModalityAsync(row, ct);

            return new ConstanciaIngresoModel
            {
                FullName = row.Paterno + " " + row.Materno + ", " + row.Nombres,
                DocumentType = "DNI",
                DocumentNumber = row.DNI,
                PostulantCode = row.CodigoEstudiante,
                CareerName = careerName,
                ModalityName = modalityName,
                TermName = termName,
                InstitutionName = "Universidad Nacional Amazónica de Madre de Dios",
                WatermarkText = watermark ? "VISTA PREVIA" : null
            };
        }

        private async Task<(string ModalityName, string TermName)> ResolveModalityAsync(ConsolidadoRow row, CancellationToken ct)
        {
            string modalityName = "—";
            string termName = row.TermName ?? "";

            if (int.TryParse(row.TipoPostulante, out var tipoIndex))
            {
                var config = await _context.PostulantTypeConfigs
                    .AsNoTracking()
                    .Where(c => c.TermId == row.TermId && c.Index == tipoIndex)
                    .Include(c => c.Modality)
                    .Include(c => c.TypeModality)
                    .FirstOrDefaultAsync(ct);

                if (config != null)
                {
                    var modality = config.Modality;
                    var typeModality = config.TypeModality;

                    bool modalityIsCepre = modality != null &&
                        (modality.IsCepreExam || modality.Name.Contains("CEPRE", StringComparison.OrdinalIgnoreCase));
                    bool typeModalityIsCepre = typeModality != null &&
                        typeModality.Name.Contains("CEPRE", StringComparison.OrdinalIgnoreCase);

                    if (modalityIsCepre || typeModalityIsCepre)
                    {
                        modalityName = "CENTRO PREUNIVERSITARIO";
                    }
                    else if (modality != null)
                    {
                        modalityName = typeModality != null
                            ? $"{modality.Name} · {typeModality.Name}"
                            : modality.Name;
                    }
                    else if (typeModality != null)
                    {
                        modalityName = typeModality.Name;
                    }
                }
            }

            if (modalityName == "—" && row.InscriptionId.HasValue)
            {
                var inscription = await _context.Inscriptions
                    .AsNoTracking()
                    .Where(i => i.Id == row.InscriptionId.Value)
                    .Include(i => i.Modality)
                    .FirstOrDefaultAsync(ct);

                if (inscription?.Modality != null)
                {
                    var modality = inscription.Modality;
                    if (modality.IsCepreExam || modality.Name.Contains("CEPRE", StringComparison.OrdinalIgnoreCase))
                    {
                        modalityName = "CENTRO PREUNIVERSITARIO";
                    }
                    else
                    {
                        modalityName = modality.Description;
                    }
                    termName = modality.Term?.Name ?? termName;
                }
            }

            return (modalityName, termName);
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "x";
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
                else if (ch == ' ' || ch == '_' || ch == '-') sb.Append('_');
            }
            var clean = sb.ToString().Trim('_');
            return string.IsNullOrEmpty(clean) ? "x" : clean;
        }

        private static string BuildFileName(ConsolidadoRow row)
        {
            var names = SanitizeFileName($"{row.Nombres} {row.Paterno} {row.Materno}".Trim());
            return $"{row.Nro}_{row.DNI}_{names}_(ingresante)";
        }

        private static string SanitizeFileName(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "x";

            s = s.Normalize(NormalizationForm.FormD);
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                    continue;
                sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);
            }

            var clean = sb.ToString().Trim();
            return string.IsNullOrEmpty(clean) ? "x" : clean;
        }

        private byte[]? TryReadImageBytes(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            if (path.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return null;
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) return null;
            if (path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return null;

            try
            {
                var trimmed = path.TrimStart('~').TrimStart('/', '\\');
                string full;

                if (trimmed.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
                {
                    full = Path.Combine(_env.ContentRootPath, "Templates", "Documents",
                        trimmed.Replace('/', Path.DirectorySeparatorChar));
                }
                else
                {
                    var webRoot = string.IsNullOrEmpty(_env.WebRootPath)
                        ? Path.Combine(_env.ContentRootPath, "wwwroot")
                        : _env.WebRootPath;
                    full = Path.Combine(webRoot, trimmed.Replace('/', Path.DirectorySeparatorChar));
                }

                return File.Exists(full) ? File.ReadAllBytes(full) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
