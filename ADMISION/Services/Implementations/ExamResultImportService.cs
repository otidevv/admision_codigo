using System.Globalization;
using System.Text;
using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Exam;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.ENTITIES.Models.Postulante;
using ADMISION.ENTITIES.Models.Users;
using ADMISION.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class ExamResultImportService : IExamResultImportService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ExamResultImportService> _logger;

        public ExamResultImportService(AppDbContext context, ILogger<ExamResultImportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ══════════════════════════════════════════════════════════════
        //  ADMISION RESULTS (regular format) - unchanged
        // ══════════════════════════════════════════════════════════════

        public async Task<ImportPreviewResult<AdmissionImportRow>> PreviewAdmissionAsync(
            Stream excelStream, string fileName, Guid termId, Guid modalityId, CancellationToken ct = default)
        {
            var result = new ImportPreviewResult<AdmissionImportRow>();

            var existingImports = await _context.AdmissionResultImportRecords
                .AsNoTracking()
                .CountAsync(r => r.TermId == termId, ct);
            if (existingImports > 0)
            {
                result.Errors.Add($"Ya existen {existingImports} registros importados para este período. Use 'Deshacer importación' antes de importar de nuevo.");
            }

            var rows = ParseAdmissionExcel(excelStream);
            result.TotalRows = rows.Count;

            var careersByName = await _context.Careers
                .AsNoTracking()
                .Where(c => c.IsActive)
                .ToDictionaryAsync(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase, ct);

            var careersByCode = await _context.Careers
                .AsNoTracking()
                .Where(c => c.IsActive)
                .ToDictionaryAsync(c => c.Code, c => c.Id, StringComparer.OrdinalIgnoreCase, ct);

            var inscriptions = (await _context.Inscriptions
                .AsNoTracking()
                .Include(i => i.Modality)
                .Where(i => i.ModalityId == modalityId && i.Modality != null && i.Modality.TermId == termId)
                .Select(i => new { i.CodePostulant, i.Id })
                .ToListAsync(ct))
                .Where(i => !string.IsNullOrEmpty(i.CodePostulant))
                .GroupBy(i => i.CodePostulant!)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.Codigo))
                {
                    row.IsValid = false;
                    row.ValidationError = "Código vacío";
                }
                else if (!inscriptions.ContainsKey(row.Codigo))
                {
                    row.IsValid = false;
                    row.ValidationError = $"Inscripción con código '{row.Codigo}' no encontrada en la modalidad seleccionada del período";
                }
                else if (!string.IsNullOrWhiteSpace(row.CarreraProfesional)
                         && !careersByName.ContainsKey(row.CarreraProfesional)
                         && !careersByCode.ContainsKey(row.CarreraProfesional))
                {
                    row.IsValid = false;
                    row.ValidationError = $"Carrera '{row.CarreraProfesional}' no encontrada (por nombre ni código)";
                }

                if (row.IsValid) result.ValidRows++;
                else result.InvalidRows++;
            }

            result.Rows = rows;
            return result;
        }

        public async Task<ImportCommitResult> ImportAdmissionAsync(
            List<AdmissionImportRow> rows, Guid termId, Guid modalityId, string actor, CancellationToken ct = default)
        {
            var result = new ImportCommitResult();
            var validRows = rows.Where(r => r.IsValid).ToList();
            var now = DateTimeOffset.UtcNow;

            var careersByName = await _context.Careers
                .AsNoTracking()
                .Where(c => c.IsActive)
                .ToDictionaryAsync(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase, ct);

            var careersByCode = await _context.Careers
                .AsNoTracking()
                .Where(c => c.IsActive)
                .ToDictionaryAsync(c => c.Code, c => c.Id, StringComparer.OrdinalIgnoreCase, ct);

            var inscriptions = (await _context.Inscriptions
                .Include(i => i.Modality)
                .Where(i => i.ModalityId == modalityId && i.Modality != null && i.Modality.TermId == termId)
                .ToListAsync(ct))
                .Where(i => !string.IsNullOrEmpty(i.CodePostulant))
                .GroupBy(i => i.CodePostulant!)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var importRecords = new List<AdmissionResultImportRecord>();

            foreach (var row in validRows)
            {
                try
                {
                    if (!inscriptions.TryGetValue(row.Codigo!, out var inscription))
                    {
                        result.Skipped++;
                        continue;
                    }

                    var careerId = !string.IsNullOrWhiteSpace(row.CarreraProfesional)
                        && (careersByName.TryGetValue(row.CarreraProfesional, out var cid)
                            || careersByCode.TryGetValue(row.CarreraProfesional, out cid))
                        ? cid : inscription.CareerId;

                    var correctas = ParseInt(row.Correctas);
                    var blancas = ParseInt(row.Blancas);
                    var puntaje = ParseDecimal(row.Puntaje);
                    var nota = ParseDecimal(row.Nota);
                    var esIngresante = string.Equals(row.Condicion?.Trim(), "INGRESÓ", StringComparison.OrdinalIgnoreCase);

                    inscription.GradeAdmission = nota ?? puntaje;
                    inscription.IsAdmission = esIngresante;
                    inscription.InscriptionOrder = esIngresante ? row.Nro : null;
                    if (string.Equals(inscription.State, AppConstants.InscripcionState.Pendiente, StringComparison.OrdinalIgnoreCase))
                        inscription.State = AppConstants.InscripcionState.Aprobado;
                    inscription.UpdatedAt = now;
                    inscription.UpdatedBy = actor;

                    var record = new AdmissionResultImportRecord
                    {
                        Id = Guid.NewGuid(),
                        TermId = termId,
                        InscriptionId = inscription.Id,
                        Nro = row.Nro,
                        Codigo = row.Codigo,
                        ApellidosNombres = row.ApellidosNombres,
                        CarreraProfesional = row.CarreraProfesional,
                        Grupo = row.Grupo,
                        Correctas = row.Correctas,
                        Blancas = row.Blancas,
                        Puntaje = row.Puntaje,
                        Nota = row.Nota,
                        Condicion = row.Condicion,
                        IsValid = true,
                        CreatedAt = now,
                        CreatedBy = actor
                    };
                    importRecords.Add(record);

                    var examRecord = new ExamScoreRecord
                    {
                        Id = Guid.NewGuid(),
                        InscriptionId = inscription.Id,
                        Correctas = correctas ?? 0,
                        Blancas = blancas ?? 0,
                        Puntaje = puntaje ?? 0,
                        Nota = nota,
                        EsIngresante = esIngresante,
                        Source = "AdmissionImport",
                        CreatedAt = now,
                        CreatedBy = actor
                    };
                    record.ExamResultId = examRecord.Id;
                    _context.ExamScoreRecords.Add(examRecord);

                    result.Imported++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing admission row {Nro}", row.Nro);
                    result.Errors.Add($"Fila {row.Nro}: {ex.Message}");
                    result.Skipped++;
                }
            }

            if (importRecords.Count > 0)
            {
                _context.AdmissionResultImportRecords.AddRange(importRecords);
                await _context.SaveChangesAsync(ct);
            }

            return result;
        }

        // ══════════════════════════════════════════════════════════════
        //  CEPRE IMPORT (solo CepreImportRecord con versionado)
        // ══════════════════════════════════════════════════════════════

        public async Task<ImportPreviewResult<CepreImportRow>> PreviewCepreAsync(
            Stream excelStream, string fileName, Guid termId, CancellationToken ct = default)
        {
            var result = new ImportPreviewResult<CepreImportRow>();

            var rows = ParseCepreExcel(excelStream);
            result.TotalRows = rows.Count;

            var careersByCode = await _context.Careers.AsNoTracking()
                .Where(c => c.IsActive)
                .ToDictionaryAsync(c => c.Code, c => c.Id, StringComparer.OrdinalIgnoreCase, ct);

            foreach (var row in rows)
            {
                var dni = (row.Dni ?? "").Trim();
                if (string.IsNullOrEmpty(dni))
                {
                    row.IsValid = false;
                    row.ValidationError = "DNI vacío";
                }
                else if (dni.Length != 8 || !dni.All(char.IsDigit))
                {
                    row.IsValid = false;
                    row.ValidationError = "DNI debe tener 8 dígitos";
                }
                else if (string.IsNullOrWhiteSpace(row.TDocumento))
                {
                    row.IsValid = false;
                    row.ValidationError = "Tipo de documento vacío";
                }
                else if (string.IsNullOrWhiteSpace(row.CodigoCarrera))
                {
                    row.IsValid = false;
                    row.ValidationError = "Código de carrera vacío";
                }
                else if (!careersByCode.ContainsKey(row.CodigoCarrera))
                {
                    row.IsValid = false;
                    row.ValidationError = $"Carrera con código '{row.CodigoCarrera}' no encontrada";
                }
                else if (row.Puntaje01 == null)
                {
                    row.IsValid = false;
                    row.ValidationError = "PUNTAJE 01 debe ser numérico";
                }
                else if (row.Puntaje02 == null)
                {
                    row.IsValid = false;
                    row.ValidationError = "PUNTAJE 02 debe ser numérico";
                }
                else if (row.Puntaje03 == null)
                {
                    row.IsValid = false;
                    row.ValidationError = "PUNTAJE 03 debe ser numérico";
                }
                else if (row.Puntaje == null)
                {
                    row.IsValid = false;
                    row.ValidationError = "PUNTAJE debe ser numérico";
                }

                if (row.IsValid) result.ValidRows++;
                else result.InvalidRows++;
            }

            result.Rows = rows;
            return result;
        }

        public async Task<ImportCommitResult> ImportCepreAsync(
            List<CepreImportRow> rows, Guid termId, string actor, CancellationToken ct = default)
        {
            var result = new ImportCommitResult();
            var validRows = rows.Where(r => r.IsValid).ToList();
            var now = DateTimeOffset.UtcNow;

            var latestVersion = await _context.CepreImportVersions
                .Where(v => v.TermId == termId && v.IsLatest)
                .FirstOrDefaultAsync(ct);

            int nextVersionNumber = 1;
            if (latestVersion != null)
            {
                nextVersionNumber = latestVersion.VersionNumber + 1;
                latestVersion.IsLatest = false;
            }

            var newVersion = new CepreImportVersion
            {
                Id = Guid.NewGuid(),
                TermId = termId,
                VersionNumber = nextVersionNumber,
                IsLatest = true,
                RecordCount = validRows.Count,
                FileName = null,
                CreatedAt = now,
                CreatedBy = actor
            };
            _context.CepreImportVersions.Add(newVersion);

            var importRecords = new List<CepreImportRecord>();

            foreach (var row in validRows)
            {
                try
                {
                    var record = new CepreImportRecord
                    {
                        Id = Guid.NewGuid(),
                        TermId = termId,
                        VersionId = newVersion.Id,
                        Nro = row.Nro,
                        Ciclo = row.Ciclo,
                        Codigo = row.Codigo,
                        Dni = row.Dni,
                        TDocumento = row.TDocumento,
                        Apaterno = row.Apaterno,
                        Amaterno = row.Amaterno,
                        Nombres = row.Nombres,
                        ApellidosNombres = row.ApellidosNombres,
                        Sexo = row.Sexo,
                        FechaNacimiento = row.FechaNacimiento,
                        Direccion = row.Direccion,
                        EstadoCivil = row.EstadoCivil,
                        AnioEgreso = row.AnioEgreso,
                        Correo = row.Correo,
                        Celular = row.Celular,
                        Colegio = row.Colegio,
                        NombreColegio = row.NombreColegio,
                        UbigeoColegio = row.UbigeoColegio,
                        DireccionColegio = row.DireccionColegio,
                        Ubigeo = row.Ubigeo,
                        Departamento = row.Departamento,
                        Provincia = row.Provincia,
                        Distrito = row.Distrito,
                        LugarNacimiento = row.LugarNacimiento,
                        Modalidad = row.Modalidad,
                        CodigoCarrera = row.CodigoCarrera,
                        CarreraProfesional = row.CarreraProfesional,
                        Grupo = row.Grupo,
                        ModalidadPago = row.ModalidadPago,
                        Monto = row.Monto,
                        Puntaje01 = row.Puntaje01,
                        Nota01 = row.Nota01,
                        Puntaje02 = row.Puntaje02,
                        Nota02 = row.Nota02,
                        Puntaje03 = row.Puntaje03,
                        Nota03 = row.Nota03,
                        NotaFinal = row.NotaFinal,
                        Puntaje = row.Puntaje,
                        Estado = row.Estado,
                        IsValid = true,
                        CreatedAt = now,
                        CreatedBy = actor
                    };
                    importRecords.Add(record);
                    result.Imported++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing CEPRE row {Nro}", row.Nro);
                    result.Errors.Add($"Fila {row.Nro}: {ex.Message}");
                    result.Skipped++;
                }
            }

            if (importRecords.Count > 0)
                _context.CepreImportRecords.AddRange(importRecords);

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving CEPRE import batch");
                result.Errors.Add($"Error al guardar el lote: {ex.InnerException?.Message ?? ex.Message}");
            }

            return result;
        }

        // ══════════════════════════════════════════════════════════════
        //  CEPRE MATCH — match CepreImportRecords ↔ Inscriptions by DNI
        // ══════════════════════════════════════════════════════════════

        public async Task<ImportPreviewResult<CepreMatchRow>> PreviewCepreMatchAsync(
            Guid termId, Guid modalityId, CancellationToken ct = default)
        {
            var result = new ImportPreviewResult<CepreMatchRow>();

            var latestVersion = await _context.CepreImportVersions
                .AsNoTracking()
                .Where(v => v.TermId == termId && v.IsLatest)
                .FirstOrDefaultAsync(ct);

            if (latestVersion == null)
            {
                result.Errors.Add("No existen datos CEPRE importados para este período.");
                return result;
            }

            var cepreRecords = await _context.CepreImportRecords
                .AsNoTracking()
                .Where(r => r.VersionId == latestVersion.Id)
                .ToListAsync(ct);

            if (cepreRecords.Count == 0)
            {
                result.Errors.Add("La versión activa de CEPRE no contiene registros.");
                return result;
            }

            var ingresoRecords = cepreRecords
                .Where(r => string.Equals(
                    RemoveDiacritics(r.Estado ?? "").Trim().ToUpperInvariant(),
                    "INGRESO",
                    StringComparison.Ordinal))
                .ToList();

            if (ingresoRecords.Count == 0)
            {
                result.Errors.Add("No hay registros con condición de ingreso en los datos CEPRE.");
                return result;
            }

            var deduplicated = ingresoRecords
                .GroupBy(r => (r.Dni ?? "").Trim())
                .Select(g => g.OrderByDescending(r => ParseCiclo(r.Ciclo)).First())
                .ToList();

            var inscriptions = await _context.Inscriptions
                .AsNoTracking()
                .Include(i => i.Postulant)
                    .ThenInclude(p => p!.User)
                .Include(i => i.Career)
                .Where(i => i.ModalityId == modalityId)
                .ToListAsync(ct);

            var dniToInscription = new Dictionary<string, Inscription>(StringComparer.OrdinalIgnoreCase);
            foreach (var ins in inscriptions)
            {
                var dni = ins.Postulant?.User?.Document?.Trim();
                if (!string.IsNullOrEmpty(dni) && dni.Length == 8 && dni.All(char.IsDigit))
                {
                    dniToInscription.TryAdd(dni, ins);
                }
            }

            int nro = 0;
            foreach (var cepre in deduplicated)
            {
                nro++;
                var dni = (cepre.Dni ?? "").Trim();
                var row = new CepreMatchRow
                {
                    Nro = nro,
                    Dni = dni,
                    CodigoCarrera = cepre.CodigoCarrera,
                    CarreraProfesional = cepre.CarreraProfesional,
                    ApellidosNombres = cepre.ApellidosNombres,
                    NotaFinal = cepre.NotaFinal,
                    Estado = cepre.Estado
                };

                if (string.IsNullOrEmpty(dni))
                {
                    row.IsValid = false;
                    row.ValidationError = "DNI vacío";
                }
                else if (dni.Length != 8 || !dni.All(char.IsDigit))
                {
                    row.IsValid = false;
                    row.ValidationError = "DNI debe tener 8 dígitos";
                }
                else if (!dniToInscription.TryGetValue(dni, out var inscription))
                {
                    row.IsValid = false;
                    row.ValidationError = $"DNI '{dni}' no encontrado en inscripciones de la modalidad";
                }
                else
                {
                    row.InscriptionCode = inscription.CodePostulant;
                }

                if (row.IsValid) result.ValidRows++;
                else result.InvalidRows++;
                result.Rows.Add(row);
            }

            result.TotalRows = nro;
            return result;
        }

        public async Task<ImportCommitResult> ImportCepreMatchAsync(
            List<CepreMatchRow> rows, Guid termId, Guid modalityId, string actor, CancellationToken ct = default)
        {
            var result = new ImportCommitResult();
            var validRows = rows.Where(r => r.IsValid).ToList();
            var now = DateTimeOffset.UtcNow;

            var latestVersion = await _context.CepreImportVersions
                .AsNoTracking()
                .Where(v => v.TermId == termId && v.IsLatest)
                .FirstOrDefaultAsync(ct);

            if (latestVersion == null)
            {
                result.Errors.Add("No existe versión activa de CEPRE.");
                return result;
            }

            var inscriptions = await _context.Inscriptions
                .Include(i => i.Postulant)
                    .ThenInclude(p => p!.User)
                .Where(i => i.ModalityId == modalityId)
                .ToListAsync(ct);

            var dniToInscription = new Dictionary<string, Inscription>(StringComparer.OrdinalIgnoreCase);
            foreach (var ins in inscriptions)
            {
                var dni = ins.Postulant?.User?.Document?.Trim();
                if (!string.IsNullOrEmpty(dni) && dni.Length == 8 && dni.All(char.IsDigit))
                {
                    dniToInscription.TryAdd(dni, ins);
                }
            }

            var matchRecords = new List<CepreMatchRecord>();

            foreach (var row in validRows)
            {
                try
                {
                    var dni = (row.Dni ?? "").Trim();
                    if (!dniToInscription.TryGetValue(dni, out var inscription))
                    {
                        result.Skipped++;
                        continue;
                    }

                    var esIngresante = string.Equals(
                        RemoveDiacritics(row.Estado ?? "").Trim().ToUpperInvariant(),
                        "INGRESO",
                        StringComparison.Ordinal);

                    inscription.GradeAdmission = row.NotaFinal;
                    inscription.IsAdmission = esIngresante;
                    if (string.Equals(inscription.State, AppConstants.InscripcionState.Pendiente, StringComparison.OrdinalIgnoreCase))
                        inscription.State = AppConstants.InscripcionState.Aprobado;
                    inscription.UpdatedAt = now;
                    inscription.UpdatedBy = actor;

                    var examRecord = new ExamScoreRecord
                    {
                        Id = Guid.NewGuid(),
                        InscriptionId = inscription.Id,
                        Correctas = 0,
                        Blancas = 0,
                        Puntaje = 0,
                        Nota = row.NotaFinal,
                        EsIngresante = esIngresante,
                        Source = "CepreMatch",
                        CreatedAt = now,
                        CreatedBy = actor
                    };
                    _context.ExamScoreRecords.Add(examRecord);

                    var matchRecord = new CepreMatchRecord
                    {
                        Id = Guid.NewGuid(),
                        TermId = termId,
                        ModalityId = modalityId,
                        CepreVersionId = latestVersion.Id,
                        InscriptionId = inscription.Id,
                        ExamResultId = examRecord.Id,
                        Nro = row.Nro,
                        Dni = dni,
                        CodigoCarrera = row.CodigoCarrera,
                        CarreraProfesional = row.CarreraProfesional,
                        ApellidosNombres = row.ApellidosNombres,
                        NotaFinal = row.NotaFinal,
                        Estado = row.Estado,
                        IsAdmission = esIngresante,
                        IsValid = true,
                        CreatedAt = now,
                        CreatedBy = actor
                    };
                    matchRecords.Add(matchRecord);

                    result.Imported++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in CEPRE match row {Nro}", row.Nro);
                    result.Errors.Add($"Fila {row.Nro}: {ex.Message}");
                    result.Skipped++;
                }
            }

            if (matchRecords.Count > 0)
            {
                _context.CepreMatchRecords.AddRange(matchRecords);
                await _context.SaveChangesAsync(ct);
            }

            // Recalcular orden de mérito por carrera (mayor nota = position 1)
            var inscriptionsToOrder = await _context.Inscriptions
                .Include(i => i.Modality)
                .Where(i => i.Modality != null && i.Modality.TermId == termId && i.GradeAdmission != null)
                .ToListAsync(ct);

            var orderGroups = inscriptionsToOrder.GroupBy(i => i.CareerId).ToList();
            foreach (var group in orderGroups)
            {
                var ordered = group.OrderByDescending(i => i.GradeAdmission ?? 0).ToList();
                int order = 1;
                foreach (var ins in ordered)
                {
                    ins.InscriptionOrder = order++;
                    ins.UpdatedAt = now;
                    ins.UpdatedBy = actor;
                }
            }
            await _context.SaveChangesAsync(ct);

            return result;
        }

        public async Task<List<ImportBatchDto>> GetCepreMatchHistoryAsync(Guid termId, CancellationToken ct = default)
        {
            return await _context.CepreMatchRecords
                .AsNoTracking()
                .Where(r => r.TermId == termId)
                .GroupBy(r => new { r.CreatedBy, r.CreatedAt })
                .Select(g => new ImportBatchDto
                {
                    Id = g.First().Id,
                    RecordCount = g.Count(),
                    CreatedBy = g.Key.CreatedBy,
                    CreatedAt = g.Key.CreatedAt
                })
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<int> RevertCepreMatchAsync(Guid batchId, string actor, CancellationToken ct = default)
        {
            var reference = await _context.CepreMatchRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == batchId, ct);
            if (reference == null) return 0;

            var batch = await _context.CepreMatchRecords
                .Where(r => r.TermId == reference.TermId
                    && r.CreatedBy == reference.CreatedBy
                    && r.CreatedAt == reference.CreatedAt)
                .ToListAsync(ct);

            if (batch.Count == 0) return 0;

            var examResultIds = batch.Where(r => r.ExamResultId.HasValue).Select(r => r.ExamResultId!.Value).ToList();
            var inscriptionIds = batch.Where(r => r.InscriptionId.HasValue).Select(r => r.InscriptionId!.Value).Distinct().ToList();

            if (examResultIds.Count > 0)
            {
                var examRecords = await _context.ExamScoreRecords
                    .Where(r => examResultIds.Contains(r.Id))
                    .ToListAsync(ct);
                _context.ExamScoreRecords.RemoveRange(examRecords);
            }

            if (inscriptionIds.Count > 0)
            {
                var inscriptions = await _context.Inscriptions
                    .Where(i => inscriptionIds.Contains(i.Id))
                    .ToListAsync(ct);
                foreach (var ins in inscriptions)
                {
                    ins.GradeAdmission = null;
                    ins.IsAdmission = false;
                    ins.UpdatedAt = DateTimeOffset.UtcNow;
                    ins.UpdatedBy = actor;
                }
            }

            _context.CepreMatchRecords.RemoveRange(batch);
            await _context.SaveChangesAsync(ct);
            return batch.Count;
        }

        // ══════════════════════════════════════════════════════════════
        //  PARSING
        // ══════════════════════════════════════════════════════════════

        private static List<AdmissionImportRow> ParseAdmissionExcel(Stream stream)
        {
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();
            var rows = new List<AdmissionImportRow>();

            var headerMap = BuildHeaderMap(ws.Row(1));

            int nroIdx = FindCol(headerMap, "Nro", "N°", "NRO");
            int codigoIdx = FindCol(headerMap, "CÓDIGO", "CODIGO", "COD");
            int apellidosIdx = FindCol(headerMap, "APELLIDOS Y NOMBRES");
            int carreraIdx = FindCol(headerMap, "CARRERA PROFESIONAL", "CARRERA");
            int grupoIdx = FindCol(headerMap, "GRUPO", "TEMA");
            int correctasIdx = FindCol(headerMap, "CORRECTAS");
            int blancasIdx = FindCol(headerMap, "BLANCAS");
            int puntajeIdx = FindCol(headerMap, "PUNTAJE");
            int notaIdx = FindCol(headerMap, "NOTA");
            int condicionIdx = FindCol(headerMap, "CONDICIÓN", "CONDICION");

            foreach (var row in ws.RowsUsed().Skip(1))
            {
                long rowNum = row.RowNumber();
                rows.Add(new AdmissionImportRow
                {
                    Nro = GetInt(ws, rowNum, nroIdx),
                    Codigo = GetStr(ws, rowNum, codigoIdx),
                    ApellidosNombres = GetStr(ws, rowNum, apellidosIdx),
                    CarreraProfesional = GetStr(ws, rowNum, carreraIdx),
                    Grupo = GetStr(ws, rowNum, grupoIdx),
                    Correctas = GetStr(ws, rowNum, correctasIdx),
                    Blancas = GetStr(ws, rowNum, blancasIdx),
                    Puntaje = GetStr(ws, rowNum, puntajeIdx),
                    Nota = GetStr(ws, rowNum, notaIdx),
                    Condicion = GetStr(ws, rowNum, condicionIdx)
                });
            }

            return rows;
        }

        private static List<CepreImportRow> ParseCepreExcel(Stream stream)
        {
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();
            var rows = new List<CepreImportRow>();

            var headerMap = BuildHeaderMap(ws.Row(1));

            int nroIdx = FindCol(headerMap, "Nº", "Nro", "N°", "NRO");
            int cicloIdx = FindCol(headerMap, "CICLO");
            int codigoIdx = FindCol(headerMap, "CODIGO", "CÓDIGO");
            int dniIdx = FindCol(headerMap, "DNI");
            int tDocumentoIdx = FindCol(headerMap, "TIPO DE DOCUMENTO", "TIPO DOCUMENTO", "TIPODEDOCUMENTO");
            int apaternoIdx = FindCol(headerMap, "APELLIDO PATERNO");
            int amaternoIdx = FindCol(headerMap, "APELLIDO MATERNO");
            int nombresIdx = FindCol(headerMap, "NOMBRES");
            int apellidosNombresIdx = FindCol(headerMap, "APELLIDOS Y NOMBRES");
            int sexoIdx = FindCol(headerMap, "SEXO");
            int fechaNacIdx = FindCol(headerMap, "FECHA DE NACIMIENTO", "FECHANACIMIENTO");
            int direccionIdx = FindCol(headerMap, "DIRECCION", "DIRECCIÓN");
            int estadoCivilIdx = FindCol(headerMap, "ESTADO CIVIL");
            int anioEgresoIdx = FindCol(headerMap, "AÑO DE EGRESO", "AÑOEGRESO");
            int correoIdx = FindCol(headerMap, "CORREO ELECTRONICO", "CORREO");
            int celularIdx = FindCol(headerMap, "CELULAR");
            int colegioIdx = FindCol(headerMap, "COLEGIO");
            int nombreColegioIdx = FindCol(headerMap, "NOMBRE COLEGIO");
            int ubigeoColegioIdx = FindCol(headerMap, "UBIGEO COLEGIO");
            int direccionColegioIdx = FindCol(headerMap, "DIRECCION COLEGIO");
            int ubigeoIdx = FindCol(headerMap, "UBIGEO");
            int departamentoIdx = FindCol(headerMap, "DEPARTAMENTO");
            int provinciaIdx = FindCol(headerMap, "PROVINCIA");
            int distritoIdx = FindCol(headerMap, "DISTRITO");
            int lugarNacIdx = FindCol(headerMap, "LUGAR DE NACIMIENTO");
            int modalidadIdx = FindCol(headerMap, "MODALIDAD");
            int codigoCarreraIdx = FindCol(headerMap, "CODIGO CARRERA", "CÓDIGO CARRERA");
            int carreraIdx = FindCol(headerMap, "CARRERA PROFESIONAL", "CARRERA");
            int grupoIdx = FindCol(headerMap, "GRUPO");
            int modalidadPagoIdx = FindCol(headerMap, "MODALIDAD PAGO");
            int montoIdx = FindCol(headerMap, "MONTO");
            int puntaje01Idx = FindCol(headerMap, "PUNTAJE 01", "PUNTAJE 1", "PUNTAJE01");
            int nota01Idx = FindCol(headerMap, "NOTA 01");
            int puntaje02Idx = FindCol(headerMap, "PUNTAJE 02", "PUNTAJE 2", "PUNTAJE02");
            int nota02Idx = FindCol(headerMap, "NOTA 02");
            int puntaje03Idx = FindCol(headerMap, "PUNTAJE 03", "PUNTAJE 3", "PUNTAJE03");
            int nota03Idx = FindCol(headerMap, "NOTA 03");
            int notaFinalIdx = FindCol(headerMap, "NOTA FINAL");
            int puntajeIdx = FindCol(headerMap, "PUNTAJE");
            int estadoIdx = FindCol(headerMap, "ESTADO");

            foreach (var row in ws.RowsUsed().Skip(1))
            {
                long rowNum = row.RowNumber();
                rows.Add(new CepreImportRow
                {
                    Nro = GetInt(ws, rowNum, nroIdx),
                    Ciclo = GetStr(ws, rowNum, cicloIdx),
                    Codigo = GetStr(ws, rowNum, codigoIdx),
                    Dni = GetStr(ws, rowNum, dniIdx),
                    TDocumento = GetStr(ws, rowNum, tDocumentoIdx),
                    Apaterno = GetStr(ws, rowNum, apaternoIdx),
                    Amaterno = GetStr(ws, rowNum, amaternoIdx),
                    Nombres = GetStr(ws, rowNum, nombresIdx),
                    ApellidosNombres = GetStr(ws, rowNum, apellidosNombresIdx),
                    Sexo = GetStr(ws, rowNum, sexoIdx),
                    FechaNacimiento = GetStr(ws, rowNum, fechaNacIdx),
                    Direccion = GetStr(ws, rowNum, direccionIdx),
                    EstadoCivil = GetStr(ws, rowNum, estadoCivilIdx),
                    AnioEgreso = GetStr(ws, rowNum, anioEgresoIdx),
                    Correo = GetStr(ws, rowNum, correoIdx),
                    Celular = GetStr(ws, rowNum, celularIdx),
                    Colegio = GetStr(ws, rowNum, colegioIdx),
                    NombreColegio = GetStr(ws, rowNum, nombreColegioIdx),
                    UbigeoColegio = GetStr(ws, rowNum, ubigeoColegioIdx),
                    DireccionColegio = GetStr(ws, rowNum, direccionColegioIdx),
                    Ubigeo = GetStr(ws, rowNum, ubigeoIdx),
                    Departamento = GetStr(ws, rowNum, departamentoIdx),
                    Provincia = GetStr(ws, rowNum, provinciaIdx),
                    Distrito = GetStr(ws, rowNum, distritoIdx),
                    LugarNacimiento = GetStr(ws, rowNum, lugarNacIdx),
                    Modalidad = GetStr(ws, rowNum, modalidadIdx),
                    CodigoCarrera = GetStr(ws, rowNum, codigoCarreraIdx),
                    CarreraProfesional = GetStr(ws, rowNum, carreraIdx),
                    Grupo = GetStr(ws, rowNum, grupoIdx),
                    ModalidadPago = GetStr(ws, rowNum, modalidadPagoIdx),
                    Monto = GetDecimal(ws, rowNum, montoIdx),
                    Puntaje01 = GetDecimalOrZero(ws, rowNum, puntaje01Idx),
                    Nota01 = GetDecimalOrZero(ws, rowNum, nota01Idx),
                    Puntaje02 = GetDecimalOrZero(ws, rowNum, puntaje02Idx),
                    Nota02 = GetDecimalOrZero(ws, rowNum, nota02Idx),
                    Puntaje03 = GetDecimalOrZero(ws, rowNum, puntaje03Idx),
                    Nota03 = GetDecimalOrZero(ws, rowNum, nota03Idx),
                    NotaFinal = GetDecimalOrZero(ws, rowNum, notaFinalIdx),
                    Puntaje = GetDecimalOrZero(ws, rowNum, puntajeIdx),
                    Estado = GetStr(ws, rowNum, estadoIdx)
                });
            }

            return rows;
        }

        // ══════════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════════

        private static Dictionary<string, int> BuildHeaderMap(IXLRow headerRow)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in headerRow.CellsUsed())
            {
                var h = cell.GetString().Trim();
                if (!string.IsNullOrEmpty(h))
                    map[h] = cell.Address.ColumnNumber;
            }
            return map;
        }

        private static int FindCol(Dictionary<string, int> headerMap, params string[] names)
        {
            foreach (var name in names)
            {
                if (headerMap.TryGetValue(name, out var colNum)) return colNum;
            }
            return -1;
        }

        private static string? GetStr(IXLWorksheet ws, long rowNum, int colNum)
        {
            if (colNum < 0) return null;
            var val = ws.Cell((int)rowNum, colNum).GetString();
            return string.IsNullOrWhiteSpace(val) ? null : val.Trim();
        }

        private static int GetInt(IXLWorksheet ws, long rowNum, int colNum)
        {
            if (colNum < 0) return 0;
            try { return ws.Cell((int)rowNum, colNum).GetValue<int>(); }
            catch { return 0; }
        }

        private static decimal? GetDecimal(IXLWorksheet ws, long rowNum, int colNum)
        {
            if (colNum < 0) return null;
            var raw = ws.Cell((int)rowNum, colNum).GetString()?.Trim();
            if (string.IsNullOrEmpty(raw)) return null;
            raw = raw.Replace(",", ".");
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
                return val;
            return null;
        }

        private static decimal? GetDecimalOrZero(IXLWorksheet ws, long rowNum, int colNum)
        {
            if (colNum < 0) return null;
            var raw = ws.Cell((int)rowNum, colNum).GetString()?.Trim();
            if (string.IsNullOrEmpty(raw)) return 0.0m;
            raw = raw.Replace(",", ".");
            if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
                return val;
            return null;
        }

        private static int? ParseInt(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim().Replace(",", ".");
            if (int.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
                return val;
            return null;
        }

        private static decimal? ParseDecimal(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim().Replace(",", ".");
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
                return val;
            return null;
        }

        private static DateOnly? ParseBirthdate(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            if (DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d;
            if (DateOnly.TryParse(s, out d))
                return d;
            return null;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // ══════════════════════════════════════════════════════════════
        //  IMPORT HISTORY + REVERT
        // ══════════════════════════════════════════════════════════════

        public async Task<List<ImportBatchDto>> GetAdmissionImportHistoryAsync(Guid termId, CancellationToken ct = default)
        {
            return await _context.AdmissionResultImportRecords
                .AsNoTracking()
                .Where(r => r.TermId == termId)
                .GroupBy(r => new { r.CreatedBy, r.CreatedAt })
                .Select(g => new ImportBatchDto
                {
                    Id = g.First().Id,
                    RecordCount = g.Count(),
                    CreatedBy = g.Key.CreatedBy,
                    CreatedAt = g.Key.CreatedAt
                })
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<ImportBatchDto>> GetCepreImportHistoryAsync(Guid termId, CancellationToken ct = default)
        {
            return await _context.CepreImportVersions
                .AsNoTracking()
                .Where(v => v.TermId == termId)
                .Select(v => new ImportBatchDto
                {
                    Id = v.Id,
                    RecordCount = v.RecordCount,
                    CreatedBy = v.CreatedBy,
                    CreatedAt = v.CreatedAt
                })
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<int> RevertAdmissionImportAsync(Guid batchId, string actor, CancellationToken ct = default)
        {
            var reference = await _context.AdmissionResultImportRecords
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == batchId, ct);
            if (reference == null) return 0;

            var batch = await _context.AdmissionResultImportRecords
                .Where(r => r.TermId == reference.TermId
                    && r.CreatedBy == reference.CreatedBy
                    && r.CreatedAt == reference.CreatedAt)
                .ToListAsync(ct);

            if (batch.Count == 0) return 0;

            var examResultIds = batch.Where(r => r.ExamResultId.HasValue).Select(r => r.ExamResultId!.Value).ToList();
            var inscriptionIds = batch.Where(r => r.InscriptionId.HasValue).Select(r => r.InscriptionId!.Value).Distinct().ToList();

            if (examResultIds.Count > 0)
            {
                var examRecords = await _context.ExamScoreRecords
                    .Where(r => examResultIds.Contains(r.Id))
                    .ToListAsync(ct);
                _context.ExamScoreRecords.RemoveRange(examRecords);
            }

            if (inscriptionIds.Count > 0)
            {
                var inscriptions = await _context.Inscriptions
                    .Where(i => inscriptionIds.Contains(i.Id))
                    .ToListAsync(ct);
                foreach (var ins in inscriptions)
                {
                    ins.GradeAdmission = null;
                    ins.IsAdmission = false;
                    ins.UpdatedAt = DateTimeOffset.UtcNow;
                    ins.UpdatedBy = actor;
                }
            }

            _context.AdmissionResultImportRecords.RemoveRange(batch);
            await _context.SaveChangesAsync(ct);
            return batch.Count;
        }

        public async Task<int> RevertCepreImportAsync(Guid batchId, string actor, CancellationToken ct = default)
        {
            var version = await _context.CepreImportVersions
                .FirstOrDefaultAsync(v => v.Id == batchId, ct);
            if (version == null) return 0;

            var records = await _context.CepreImportRecords
                .Where(r => r.VersionId == version.Id)
                .ToListAsync(ct);

            if (records.Count > 0)
                _context.CepreImportRecords.RemoveRange(records);

            _context.CepreImportVersions.Remove(version);

            var previousVersion = await _context.CepreImportVersions
                .Where(v => v.TermId == version.TermId && v.VersionNumber < version.VersionNumber)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(ct);
            if (previousVersion != null)
                previousVersion.IsLatest = true;

            await _context.SaveChangesAsync(ct);
            return records.Count;
        }

        // ══════════════════════════════════════════════════════════════
        //  TURNOS
        // ══════════════════════════════════════════════════════════════

        public async Task<List<CepreTurn>> GetTurnsByTermAsync(Guid termId, CancellationToken ct = default)
        {
            return await _context.CepreTurns
                .AsNoTracking()
                .Include(t => t.User)
                .Where(t => t.TermId == termId)
                .OrderBy(t => t.StartDate)
                .ToListAsync(ct);
        }

        public async Task<CepreTurn?> GetActiveTurnAsync(Guid termId, Guid userId, CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow;
            return await _context.CepreTurns
                .AsNoTracking()
                .Include(t => t.User)
                .Where(t => t.TermId == termId
                    && t.UserId == userId
                    && t.StartDate <= now
                    && t.EndDate >= now)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> HasActiveTurnAsync(Guid termId, Guid userId, CancellationToken ct = default)
        {
            var now = DateTimeOffset.UtcNow;
            return await _context.CepreTurns
                .AsNoTracking()
                .AnyAsync(t => t.TermId == termId
                    && t.UserId == userId
                    && t.StartDate <= now
                    && t.EndDate >= now, ct);
        }

        public async Task<bool> CreateTurnAsync(CepreTurn turn, CancellationToken ct = default)
        {
            var exists = await _context.CepreTurns
                .AnyAsync(t => t.TermId == turn.TermId && t.UserId == turn.UserId, ct);
            if (exists) return false;

            _context.CepreTurns.Add(turn);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteTurnAsync(Guid turnId, CancellationToken ct = default)
        {
            var turn = await _context.CepreTurns.FindAsync(new object[] { turnId }, ct);
            if (turn == null) return false;

            _context.CepreTurns.Remove(turn);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        // ══════════════════════════════════════════════════════════════
        //  VERSIONES
        // ══════════════════════════════════════════════════════════════

        public async Task<List<CepreImportVersion>> GetVersionsAsync(Guid termId, CancellationToken ct = default)
        {
            return await _context.CepreImportVersions
                .AsNoTracking()
                .Where(v => v.TermId == termId)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync(ct);
        }

        public async Task<CepreImportVersion?> GetLatestVersionAsync(Guid termId, CancellationToken ct = default)
        {
            return await _context.CepreImportVersions
                .AsNoTracking()
                .Where(v => v.TermId == termId && v.IsLatest)
                .FirstOrDefaultAsync(ct);
        }

        public byte[] BuildAdmissionTemplate()
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Resultados");

            var headers = new[] { "Nro", "CÓDIGO", "APELLIDOS Y NOMBRES", "CARRERA PROFESIONAL", "GRUPO", "CORRECTAS", "BLANCAS", "PUNTAJE", "NOTA", "CONDICIÓN" };
            for (int i = 0; i < headers.Length; i++)
            {
                var c = ws.Cell(1, i + 1);
                c.Value = headers[i];
                c.Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#374151"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }

            var example = new object[]
            {
                1, "202400001", "GARCIA LOPEZ, JUAN CARLOS", "INGENIERIA DE SISTEMAS",
                "P", 38, 2, 19.5m, 18.2m, "INGRESANTE"
            };
            for (int i = 0; i < example.Length; i++)
                ws.Cell(2, i + 1).SetValue(XLCellValue.FromObject(example[i]));

            ws.Columns().AdjustToContents();

            AddInstructionsSheet(wb, "Importación de resultados por modalidad", new[]
            {
                "La cabecera debe estar en la FILA 1 con estos nombres de columna (no importan mayúsculas/minúsculas):",
                "Nro · CÓDIGO · APELLIDOS Y NOMBRES · CARRERA PROFESIONAL · GRUPO · CORRECTAS · BLANCAS · PUNTAJE · NOTA · CONDICIÓN.",
                "También se aceptan estos alias: 'CODIGO'/'COD' en vez de 'CÓDIGO'; 'CARRERA' en vez de 'CARRERA PROFESIONAL'; 'TEMA' en vez de 'GRUPO'; 'CONDICION' en vez de 'CONDICIÓN'; 'N°'/'NRO' en vez de 'Nro'.",
                "No agregue filas de título ni filas en blanco sobre la cabecera.",
                "Complete un registro por fila a partir de la fila 2. La fila 2 es solo un ejemplo y puede reemplazarla."
            });

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        public byte[] BuildCepreTemplate()
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("CEPRE");

            var headers = new[]
            {
                "Nº", "CICLO", "CODIGO", "DNI", "TIPO DE DOCUMENTO", "APELLIDO PATERNO", "APELLIDO MATERNO",
                "NOMBRES", "APELLIDOS Y NOMBRES", "SEXO", "FECHA DE NACIMIENTO",
                "DIRECCION", "ESTADO CIVIL", "AÑO DE EGRESO", "CORREO ELECTRONICO",
                "CELULAR", "COLEGIO", "NOMBRE COLEGIO", "UBIGEO COLEGIO",
                "DIRECCION COLEGIO", "UBIGEO", "DEPARTAMENTO", "PROVINCIA", "DISTRITO",
                "LUGAR DE NACIMIENTO", "MODALIDAD", "CODIGO CARRERA", "CARRERA PROFESIONAL", "GRUPO",
                "MODALIDAD PAGO", "MONTO", "PUNTAJE 01", "NOTA 01", "PUNTAJE 02", "NOTA 02",
                "PUNTAJE 03", "NOTA 03", "NOTA FINAL", "PUNTAJE", "ESTADO"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                var c = ws.Cell(1, i + 1);
                c.Value = headers[i];
                c.Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#374151"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }

            var example = new object[]
            {
                1, "2024-II", "5000002", "70123456", "DNI", "GARCIA", "LOPEZ", "JUAN CARLOS",
                "GARCIA LOPEZ, JUAN CARLOS", "M", "01/01/2005", "AV. LOS ANDES 123",
                "SOLTERO", "2023", "juan@correo.com", "999888777", "01",
                "I.E. JOSE CARLOS MARIATEGUI", "040101", "MADRE DE DIOS", "040101",
                "MADRE DE DIOS", "TAMBOPATA", "PUERTO MALDONADO",
                "PUERTO MALDONADO", "CEPRE", "01", "INGENIERIA DE SISTEMAS", "P",
                "C", 450.00m, 155.0m, 15.5m, 160.0m, 16.0m, 145.0m, 14.5m, 15.33m, 153.33m, "APROBADO"
            };
            for (int i = 0; i < example.Length; i++)
                ws.Cell(2, i + 1).SetValue(XLCellValue.FromObject(example[i]));

            ws.Columns().AdjustToContents();

            AddInstructionsSheet(wb, "Importación de postulantes CEPRE", new[]
            {
                "La cabecera debe estar en la FILA 1 con estos nombres de columna (no importan mayúsculas/minúsculas):",
                "Nº · CICLO · CODIGO · DNI · TIPO DE DOCUMENTO · APELLIDO PATERNO · APELLIDO MATERNO · NOMBRES · APELLIDOS Y NOMBRES · SEXO · FECHA DE NACIMIENTO · DIRECCION · ESTADO CIVIL · AÑO DE EGRESO · CORREO ELECTRONICO · CELULAR · COLEGIO · NOMBRE COLEGIO · UBIGEO COLEGIO · DIRECCION COLEGIO · UBIGEO · DEPARTAMENTO · PROVINCIA · DISTRITO · LUGAR DE NACIMIENTO · MODALIDAD · CODIGO CARRERA · CARRERA PROFESIONAL · GRUPO · MODALIDAD PAGO · MONTO · PUNTAJE 01 · NOTA 01 · PUNTAJE 02 · NOTA 02 · PUNTAJE 03 · NOTA 03 · NOTA FINAL · PUNTAJE · ESTADO.",
                "También se aceptan estos alias: 'CÓDIGO' en vez de 'CODIGO'; 'FECHANACIMIENTO' en vez de 'FECHA DE NACIMIENTO'; 'DIRECCIÓN' en vez de 'DIRECCION'; 'AÑOEGRESO' en vez de 'AÑO DE EGRESO'; 'CORREO' en vez de 'CORREO ELECTRONICO'; 'TIPO DOCUMENTO'/'TIPODEDOCUMENTO' en vez de 'TIPO DE DOCUMENTO'; 'CÓDIGO CARRERA' en vez de 'CODIGO CARRERA'; 'CARRERA' en vez de 'CARRERA PROFESIONAL'; 'PUNTAJE01' en vez de 'PUNTAJE 01'; 'PUNTAJE02' en vez de 'PUNTAJE 02'; 'PUNTAJE03' en vez de 'PUNTAJE 03'; 'Nro'/'N°'/'NRO' en vez de 'Nº'.",
                "No agregue filas de título ni filas en blanco sobre la cabecera.",
                "Complete un registro por fila a partir de la fila 2. La fila 2 es solo un ejemplo y puede reemplazarla.",
                "'CICLO' con formato aaaa-N (ej. 2024-II). Fechas en formato dd/mm/aaaa.",
                "'TIPO DE DOCUMENTO' es obligatorio (ej. DNI). Las columnas 'PUNTAJE 01/02/03', 'NOTA 01/02/03', 'NOTA FINAL' y 'PUNTAJE' aceptan números; si vienen vacías se guardan como 0.0."
            });

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private static void AddInstructionsSheet(XLWorkbook wb, string title, string[] lines)
        {
            var ws = wb.Worksheets.Add("Instrucciones");
            ws.Cell(1, 1).Value = title;
            ws.Cell(1, 1).Style.Font.SetBold(true).Font.SetFontSize(12).Font.SetFontColor(XLColor.FromHtml("#1e3a8a"));
            for (int i = 0; i < lines.Length; i++)
                ws.Cell(i + 2, 1).Value = lines[i];
            ws.Column(1).Width = 110;
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

