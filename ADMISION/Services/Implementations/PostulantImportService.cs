using System.Globalization;
using System.Text;
using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.ENTITIES.Models.Postulant;
using ADMISION.ENTITIES.Models.Postulante;
using ADMISION.ENTITIES.Models.Schools;
using ADMISION.ENTITIES.Models.Ubigeo;
using ADMISION.ENTITIES.Models.Users;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using DisabilityType = ADMISION.ENTITIES.Models.Postulant.DisabilityType;

namespace ADMISION.Services.Implementations;

public class PostulantImportService : IPostulantImportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PostulantImportService> _logger;

    public PostulantImportService(AppDbContext context, ILogger<PostulantImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<PostulantImportRow>> PreviewAsync(Stream excelStream, CancellationToken ct = default)
    {
        var rows = ParseExcel(excelStream);
        await ValidateRowsAsync(rows, ct);
        return rows;
    }

    public async Task<PostulantImportResult> ExecuteImportAsync(Stream excelStream, string actor, CancellationToken ct = default)
    {
        var rows = ParseExcel(excelStream);
        await ValidateRowsAsync(rows, ct);

        var valid = rows.Where(r => r.IsValid).ToList();
        if (valid.Count == 0)
            return new PostulantImportResult { Inserted = 0, Skipped = 0, Errors = rows.Count, FailedRows = rows.Where(r => !r.IsValid).ToList() };

        var result = new PostulantImportResult();
        var now = DateTimeOffset.UtcNow;

        var countries = await ToDictionarySafeAsync(_context.Countries.AsNoTracking(), c => RemoveDiacritics(c.Name.ToUpper()), c => c.Id, ct);
        var distrits = await _context.Distrits.AsNoTracking().ToListAsync(ct);
        var careers = await ToDictionarySafeAsync(_context.Careers.AsNoTracking(), c => c.Code, c => c.Id, ct);
        var schools = await ToDictionarySafeAsync(_context.Schools.AsNoTracking(), s => RemoveDiacritics(s.Name.ToUpper()), s => s.Id, ct);
        var disabilityTypes = await ToDictionarySafeAsync(_context.DisabilityTypes.AsNoTracking(), d => RemoveDiacritics(d.Name.ToUpper()), d => d.Id, ct);
        var existingUsers = await ToDictionarySafeAsync(_context.Users.AsNoTracking(), u => u.Document, u => u.Id, ct);

        var sinDiscapacidadId = await GetOrCreateSinDiscapacidadAsync(actor, ct);

        foreach (var group in valid.GroupBy(r => r.Periodo ?? "SIN_PERIODO"))
        {
            var periodoName = group.Key;
            var rowsInPeriod = group.ToList();

            var fechas = rowsInPeriod
                .Select(r => (Inicio: TryParseDate(r.FechaInicio), Fin: TryParseDate(r.FechaFin)))
                .Where(f => f.Inicio.HasValue || f.Fin.HasValue)
                .ToList();

            var startDate = fechas.Any() ? fechas.Min(f => f.Inicio ?? DateOnly.MinValue) : DateOnly.MinValue;
            var endDate = fechas.Any() ? fechas.Max(f => f.Fin ?? DateOnly.MinValue) : DateOnly.MinValue;

            var existingTerm = await _context.Terms.AsNoTracking()
                .Include(t => t.Modalities)
                .FirstOrDefaultAsync(t => t.Name == periodoName, ct);

            Term term;
            if (existingTerm != null)
            {
                term = existingTerm;
            }
            else
            {
                var year = ExtractYear(periodoName);
                term = new Term
                {
                    Id = Guid.NewGuid(),
                    Name = periodoName,
                    Number = 1,
                    Year = year,
                    IsActive = false,
                    StartDate = startDate,
                    EndDate = endDate,
                    CreatedAt = now,
                    CreatedBy = actor
                };
                _context.Terms.Add(term);
                await _context.SaveChangesAsync(ct);
            }

            var modalityNames = rowsInPeriod
                .Select(r => r.Modalidad)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existingModalities = await ToDictionarySafeAsync(
                _context.Modalities.AsNoTracking().Where(m => m.TermId == term.Id),
                m => RemoveDiacritics(m.Name.ToUpper()),
                m => m,
                ct);

            var modalityCache = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (var modName in modalityNames)
            {
                var key = RemoveDiacritics(modName!.ToUpper());
                if (existingModalities.TryGetValue(key, out var existing))
                {
                    modalityCache[modName] = existing.Id;
                }
                else
                {
                    var mDates = rowsInPeriod
                        .Where(r => string.Equals(r.Modalidad, modName, StringComparison.OrdinalIgnoreCase))
                        .Select(r => (Inicio: TryParseDate(r.FechaInicio), Fin: TryParseDate(r.FechaFin)))
                        .Where(f => f.Inicio.HasValue || f.Fin.HasValue)
                        .ToList();

                    var mStart = mDates.Any() ? mDates.Min(f => f.Inicio ?? DateOnly.MinValue) : startDate;
                    var mEnd = mDates.Any() ? mDates.Max(f => f.Fin ?? DateOnly.MinValue) : endDate;

                    var modality = new Modality
                    {
                        Id = Guid.NewGuid(),
                        Name = modName,
                        Description = modName,
                        IsActive = false,
                        StartDate = mStart,
                        EndDate = mEnd,
                        TermId = term.Id,
                        CreatedAt = now,
                        CreatedBy = actor
                    };
                    _context.Modalities.Add(modality);
                    await _context.SaveChangesAsync(ct);
                    modalityCache[modName] = modality.Id;
                }
            }

            var typeModalityNames = rowsInPeriod
                .Select(r => r.TipoModalidad)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (typeModalityNames.Count > 0)
            {
                var existingTypeModalities = await ToDictionarySafeAsync(
                    _context.TypeModalities.AsNoTracking().Where(tm => modalityCache.Values.Contains(tm.ModalityId)),
                    tm => tm.Name.ToUpper(),
                    tm => tm,
                    ct);

                foreach (var tmName in typeModalityNames)
                {
                    var key = RemoveDiacritics(tmName!.ToUpper());
                    if (existingTypeModalities.ContainsKey(key)) continue;

                    var firstRow = rowsInPeriod.FirstOrDefault(r =>
                        string.Equals(r.TipoModalidad, tmName, StringComparison.OrdinalIgnoreCase));
                    var modalityId = firstRow != null && firstRow.Modalidad != null
                        ? modalityCache.GetValueOrDefault(firstRow.Modalidad)
                        : modalityCache.Values.FirstOrDefault();

                    if (modalityId == Guid.Empty) continue;

                    _context.TypeModalities.Add(new TypeModality
                    {
                        Id = Guid.NewGuid(),
                        Name = tmName,
                        Description = tmName,
                        IsActive = false,
                        ModalityId = modalityId,
                        CreatedAt = now,
                        CreatedBy = actor
                    });
                }
                await _context.SaveChangesAsync(ct);
            }

            var tipoPostulanteNames = rowsInPeriod
                .Select(r => r.TipoPostulante)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existingTipoPostulantes = await ToDictionarySafeAsync(
                _context.TypePostulantInscriptions.AsNoTracking(),
                t => RemoveDiacritics(t.Name.ToUpper()),
                t => t.Id,
                ct);

            foreach (var tpName in tipoPostulanteNames)
            {
                var key = RemoveDiacritics(tpName!.ToUpper());
                if (existingTipoPostulantes.ContainsKey(key)) continue;

                _context.TypePostulantInscriptions.Add(new TypePostulantInscription
                {
                    Id = Guid.NewGuid(),
                    Name = tpName,
                    Description = tpName,
                    IsActive = false,
                    CreatedAt = now,
                    CreatedBy = actor
                });
                await _context.SaveChangesAsync(ct);
                existingTipoPostulantes[key] = (await _context.TypePostulantInscriptions
                    .AsNoTracking()
                    .FirstAsync(t => t.Name == tpName, ct)).Id;
            }

            var typeModalityLookup = await ToDictionarySafeAsync(
                _context.TypeModalities.AsNoTracking().Where(tm => modalityCache.Values.Contains(tm.ModalityId)),
                tm => RemoveDiacritics(tm.Name.ToUpper()),
                tm => tm.Id,
                ct);

            var tipoPostulanteLookup = await ToDictionarySafeAsync(
                _context.TypePostulantInscriptions.AsNoTracking(),
                t => RemoveDiacritics(t.Name.ToUpper()),
                t => t.Id,
                ct);

            foreach (var row in rowsInPeriod)
            {
                try
                {
                    await ImportRowAsync(row, modalityCache, typeModalityLookup, tipoPostulanteLookup,
                        careers, countries, distrits, schools, disabilityTypes, existingUsers,
                        sinDiscapacidadId, actor, now, ct);
                    result.Inserted++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importando fila {Row}: {Dni}", row.RowNumber, row.Dni);
                    row.Errors.Add($"Error inesperado: {ex.Message}");
                    result.FailedRows.Add(row);
                    result.Errors++;
                }
            }
        }

        await _context.SaveChangesAsync(ct);
        return result;
    }

    public async Task ImportBackgroundAsync(Guid jobId, string tempPath, string actor,
        Func<ImportProgress, Task>? onProgress = null, CancellationToken ct = default)
    {
        List<PostulantImportRow> rows;
        using (var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Read))
        {
            rows = ParseExcel(stream);
        }

        await ValidateRowsAsync(rows, ct);

        var valid = rows.Where(r => r.IsValid).ToList();
        var total = valid.Count;
        if (total == 0)
        {
            await onProgress?.Invoke(new ImportProgress { Processed = 0, Total = 0, Inserted = 0, Skipped = 0, Failed = rows.Count });
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var batchSize = 100;
        var globalInserted = 0;
        var globalSkipped = 0;
        var globalFailed = 0;
        var globalProcessed = 0;

        var countries = await ToDictionarySafeAsync(_context.Countries.AsNoTracking(), c => RemoveDiacritics(c.Name.ToUpper()), c => c.Id, ct);
        var distrits = await _context.Distrits.AsNoTracking().ToListAsync(ct);
        var careers = await ToDictionarySafeAsync(_context.Careers.AsNoTracking(), c => c.Code, c => c.Id, ct);
        var schools = await ToDictionarySafeAsync(_context.Schools.AsNoTracking(), s => RemoveDiacritics(s.Name.ToUpper()), s => s.Id, ct);
        var disabilityTypes = await ToDictionarySafeAsync(_context.DisabilityTypes.AsNoTracking(), d => RemoveDiacritics(d.Name.ToUpper()), d => d.Id, ct);
        var existingUsers = await ToDictionarySafeAsync(_context.Users.AsNoTracking(), u => u.Document, u => u.Id, ct);
        var sinDiscapacidadId = await GetOrCreateSinDiscapacidadAsync(actor, ct);

        foreach (var group in valid.GroupBy(r => r.Periodo ?? "SIN_PERIODO"))
        {
            var groupRows = group.ToList();

            var fechas = groupRows
                .Select(r => (Inicio: TryParseDate(r.FechaInicio), Fin: TryParseDate(r.FechaFin)))
                .Where(f => f.Inicio.HasValue || f.Fin.HasValue)
                .ToList();

            var startDate = fechas.Any() ? fechas.Min(f => f.Inicio ?? DateOnly.MinValue) : DateOnly.MinValue;
            var endDate = fechas.Any() ? fechas.Max(f => f.Fin ?? DateOnly.MinValue) : DateOnly.MinValue;

            var existingTerm = await _context.Terms.AsNoTracking()
                .Include(t => t.Modalities)
                .FirstOrDefaultAsync(t => t.Name == group.Key, ct);

            Term term;
            if (existingTerm != null)
            {
                term = existingTerm;
            }
            else
            {
                var year = ExtractYear(group.Key);
                term = new Term
                {
                    Id = Guid.NewGuid(),
                    Name = group.Key,
                    Number = 1,
                    Year = year,
                    IsActive = false,
                    StartDate = startDate,
                    EndDate = endDate,
                    CreatedAt = now,
                    CreatedBy = actor
                };
                _context.Terms.Add(term);
                await _context.SaveChangesAsync(ct);
            }

            var modalityNames = groupRows
                .Select(r => r.Modalidad)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existingModalities = await ToDictionarySafeAsync(
                _context.Modalities.AsNoTracking().Where(m => m.TermId == term.Id),
                m => RemoveDiacritics(m.Name.ToUpper()),
                m => m,
                ct);

            var modalityCache = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            foreach (var modName in modalityNames)
            {
                var key = RemoveDiacritics(modName!.ToUpper());
                if (existingModalities.TryGetValue(key, out var existing))
                {
                    modalityCache[modName] = existing.Id;
                }
                else
                {
                    var mDates = groupRows
                        .Where(r => string.Equals(r.Modalidad, modName, StringComparison.OrdinalIgnoreCase))
                        .Select(r => (Inicio: TryParseDate(r.FechaInicio), Fin: TryParseDate(r.FechaFin)))
                        .Where(f => f.Inicio.HasValue || f.Fin.HasValue)
                        .ToList();

                    var mStart = mDates.Any() ? mDates.Min(f => f.Inicio ?? DateOnly.MinValue) : startDate;
                    var mEnd = mDates.Any() ? mDates.Max(f => f.Fin ?? DateOnly.MinValue) : endDate;

                    var modality = new Modality
                    {
                        Id = Guid.NewGuid(),
                        Name = modName,
                        Description = modName,
                        IsActive = false,
                        StartDate = mStart,
                        EndDate = mEnd,
                        TermId = term.Id,
                        CreatedAt = now,
                        CreatedBy = actor
                    };
                    _context.Modalities.Add(modality);
                    await _context.SaveChangesAsync(ct);
                    modalityCache[modName] = modality.Id;
                }
            }

            var typeModalityNames = groupRows
                .Select(r => r.TipoModalidad)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (typeModalityNames.Count > 0)
            {
                var existingTypeModalities = await ToDictionarySafeAsync(
                    _context.TypeModalities.AsNoTracking().Where(tm => modalityCache.Values.Contains(tm.ModalityId)),
                    tm => tm.Name.ToUpper(),
                    tm => tm,
                    ct);

                foreach (var tmName in typeModalityNames)
                {
                    var key = RemoveDiacritics(tmName!.ToUpper());
                    if (existingTypeModalities.ContainsKey(key)) continue;

                    var firstRow = groupRows.FirstOrDefault(r =>
                        string.Equals(r.TipoModalidad, tmName, StringComparison.OrdinalIgnoreCase));
                    var modId = firstRow != null && firstRow.Modalidad != null
                        ? modalityCache.GetValueOrDefault(firstRow.Modalidad)
                        : modalityCache.Values.FirstOrDefault();

                    if (modId == Guid.Empty) continue;

                    _context.TypeModalities.Add(new TypeModality
                    {
                        Id = Guid.NewGuid(),
                        Name = tmName,
                        Description = tmName,
                        IsActive = false,
                        ModalityId = modId,
                        CreatedAt = now,
                        CreatedBy = actor
                    });
                }
                await _context.SaveChangesAsync(ct);
            }

            var tipoPostulanteNames = groupRows
                .Select(r => r.TipoPostulante)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existingTipoPostulantes = await ToDictionarySafeAsync(
                _context.TypePostulantInscriptions.AsNoTracking(),
                t => RemoveDiacritics(t.Name.ToUpper()),
                t => t.Id,
                ct);

            foreach (var tpName in tipoPostulanteNames)
            {
                var key = RemoveDiacritics(tpName!.ToUpper());
                if (existingTipoPostulantes.ContainsKey(key)) continue;

                _context.TypePostulantInscriptions.Add(new TypePostulantInscription
                {
                    Id = Guid.NewGuid(),
                    Name = tpName,
                    Description = tpName,
                    IsActive = false,
                    CreatedAt = now,
                    CreatedBy = actor
                });
                await _context.SaveChangesAsync(ct);
                existingTipoPostulantes[key] = (await _context.TypePostulantInscriptions
                    .AsNoTracking()
                    .FirstAsync(t => t.Name == tpName, ct)).Id;
            }

            var typeModalityLookup = await ToDictionarySafeAsync(
                _context.TypeModalities.AsNoTracking().Where(tm => modalityCache.Values.Contains(tm.ModalityId)),
                tm => RemoveDiacritics(tm.Name.ToUpper()),
                tm => tm.Id,
                ct);

            var tipoPostulanteLookup = await ToDictionarySafeAsync(
                _context.TypePostulantInscriptions.AsNoTracking(),
                t => RemoveDiacritics(t.Name.ToUpper()),
                t => t.Id,
                ct);

            for (int i = 0; i < groupRows.Count; i += batchSize)
            {
                var batch = groupRows.Skip(i).Take(batchSize).ToList();
                var batchInserted = 0;
                var batchFailed = 0;

                var batchUserCache = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in existingUsers) batchUserCache[kv.Key] = kv.Value;

                var batchPostulantCache = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

                foreach (var row in batch)
                {
                    try
                    {
                        await ImportRowNoSaveAsync(row, modalityCache, typeModalityLookup, tipoPostulanteLookup,
                            careers, countries, distrits, schools, disabilityTypes, batchUserCache, batchPostulantCache,
                            sinDiscapacidadId, actor, now, ct);
                        batchInserted++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error importando fila {Row}: {Dni}", row.RowNumber, row.Dni);
                        batchFailed++;
                    }
                }

                await _context.SaveChangesAsync(ct);
                _context.ChangeTracker.Clear();

                foreach (var kv in batchUserCache)
                {
                    existingUsers[kv.Key] = kv.Value;
                }

                globalProcessed += batch.Count;
                globalInserted += batchInserted;
                globalFailed += batchFailed;

                if (onProgress != null)
                {
                    await onProgress(new ImportProgress
                    {
                        Processed = globalProcessed,
                        Total = total,
                        Inserted = globalInserted,
                        Skipped = globalSkipped,
                        Failed = globalFailed
                    });
                }
            }
        }

        if (onProgress != null)
        {
            await onProgress(new ImportProgress
            {
                Processed = total,
                Total = total,
                Inserted = globalInserted,
                Skipped = globalSkipped,
                Failed = globalFailed
            });
        }
    }

    private async Task ImportRowAsync(
        PostulantImportRow row,
        Dictionary<string, Guid> modalityCache,
        Dictionary<string, Guid> typeModalityLookup,
        Dictionary<string, Guid> tipoPostulanteLookup,
        Dictionary<string, Guid> careers,
        Dictionary<string, Guid> countries,
        List<Distrit> distrits,
        Dictionary<string, Guid> schools,
        Dictionary<string, Guid> disabilityTypes,
        Dictionary<string, Guid> existingUsers,
        Guid sinDiscapacidadId,
        string actor,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var dni = (row.Dni ?? "").Trim();
        if (string.IsNullOrEmpty(dni)) return;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Document == dni, ct);
        Guid userId;
        Guid postulantId;

        if (user == null)
        {
            user = new Users
            {
                Id = Guid.NewGuid(),
                Document = dni,
                DocumentType = "DNI",
                Name = (row.Nombres ?? "").Trim().ToUpper(),
                FirstNameFather = (row.Apaterno ?? "").Trim().ToUpper(),
                FirstNameMother = (row.Amaterno ?? "").Trim().ToUpper(),
                FullName = $"{row.Nombres} {row.Apaterno} {row.Amaterno}".Trim().ToUpper(),
                PhoneNumber = (row.Celular ?? "").Trim(),
                Email = (row.Correo ?? "").Trim(),
                Genero = NormalizeGenero(row.Sexo),
                CivilStatus = string.IsNullOrWhiteSpace(row.EstadoCivil) ? null : row.EstadoCivil.Trim(),
                Address = string.IsNullOrWhiteSpace(row.Direccion) ? null : row.Direccion.Trim().ToUpper(),
                Birthdate = ParseBirthdate(row.FechaNacimiento),
                CreatedAt = now,
                CreatedBy = actor
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync(ct);
            userId = user.Id;

            var postulant = new Postulant
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = now,
                CreatedBy = actor
            };
            _context.Postulants.Add(postulant);
            await _context.SaveChangesAsync(ct);
            postulantId = postulant.Id;

            existingUsers[dni] = userId;
        }
        else
        {
            userId = user.Id;
            var postulant = await _context.Postulants.FirstOrDefaultAsync(p => p.UserId == userId, ct);
            if (postulant == null)
            {
                postulant = new Postulant
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = now,
                    CreatedBy = actor
                };
                _context.Postulants.Add(postulant);
                await _context.SaveChangesAsync(ct);
            }
            postulantId = postulant.Id;
        }

        var modalityId = row.Modalidad != null && modalityCache.TryGetValue(row.Modalidad, out var mid)
            ? mid
            : modalityCache.Values.FirstOrDefault();

        Guid? typeModalityId = null;
        if (!string.IsNullOrWhiteSpace(row.TipoModalidad) && typeModalityLookup.TryGetValue(RemoveDiacritics(row.TipoModalidad.Trim().ToUpper()), out var tmid))
            typeModalityId = tmid;

        Guid? tipoPostulanteId = null;
        if (!string.IsNullOrWhiteSpace(row.TipoPostulante) && tipoPostulanteLookup.TryGetValue(RemoveDiacritics(row.TipoPostulante.Trim().ToUpper()), out var tpid))
            tipoPostulanteId = tpid;

        var careerId = row.CodigoCarrera != null && careers.TryGetValue(row.CodigoCarrera, out var cid)
            ? cid
            : careers.Values.FirstOrDefault();

        var countryId = countries.Values.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(row.Pais) && countries.TryGetValue(RemoveDiacritics(row.Pais.Trim().ToUpper()), out var foundCountry))
            countryId = foundCountry;
        else             if (countries.TryGetValue(RemoveDiacritics("PERU"), out var peruId))
            countryId = peruId;

        var distritId = ResolveDistritId(row.CodUbigeo, distrits);

        Guid? schoolId = null;
        var otherSchool = (string?)null;
        if (!string.IsNullOrWhiteSpace(row.Colegio))
        {
            var schoolKey = RemoveDiacritics(row.Colegio.Trim().ToUpper());
            if (schools.TryGetValue(schoolKey, out var sid))
                schoolId = sid;
            else
                otherSchool = RemoveDiacritics(row.Colegio.Trim().ToUpper());
        }

        var inscriptionDate = ParseInscriptionDate(row.FechaInscripcion, now);

        var inscription = new Inscription
        {
            Id = Guid.NewGuid(),
            PostulantId = postulantId,
            ModalityId = modalityId != Guid.Empty ? modalityId : null,
            TypeModalityId = typeModalityId,
            TypePostulantInscriptionId = tipoPostulanteId,
            CareerId = careerId,
            CountryId = countryId,
            DistritId = distritId,
            CodePostulant = (row.CodigoPostulante ?? "").Trim(),
            State = AppConstants.InscripcionState.Pendiente,
            IsAdmission = false,
            CreatedAt = inscriptionDate,
            CreatedBy = actor,
            OtherSchool = otherSchool,
            SchoolId = schoolId,
            DJ = true
        };
        _context.Inscriptions.Add(inscription);
        await _context.SaveChangesAsync(ct);

        var disabilityName = RemoveDiacritics((row.TipoDiscapacidad ?? "").Trim().ToUpper());
        Guid disabilityIdToUse;
        if (string.IsNullOrEmpty(disabilityName) || disabilityName is "SIN DISCAPACIDAD" or "NINGUNO" or "N/A" or "")
        {
            disabilityIdToUse = sinDiscapacidadId;
        }
        else
        {
            disabilityIdToUse = disabilityTypes.TryGetValue(disabilityName, out var did)
                ? did
                : sinDiscapacidadId;
        }

        _context.PostulantDisabilities.Add(new PostulantDisability
        {
            Id = Guid.NewGuid(),
            PostulantId = postulantId,
            DisabilityTypeId = disabilityIdToUse,
            CreatedAt = now,
            CreatedBy = actor
        });

        if (!string.IsNullOrWhiteSpace(row.DniApoderado) && !string.IsNullOrWhiteSpace(row.NombresApoderado))
        {
            var apellidos = (row.ApellidosApoderado ?? "").Trim().ToUpper();
            var nombresApo = (row.NombresApoderado ?? "").Trim().ToUpper();
            var fullNameApo = $"{nombresApo} {apellidos}".Trim();

            _context.Parents.Add(new Parent
            {
                Id = Guid.NewGuid(),
                PostulantId = postulantId,
                InscriptionId = inscription.Id,
                Name = nombresApo,
                FirstNameFather = apellidos,
                FirstNameMother = "",
                FullName = fullNameApo,
                TypeDocument = "DNI",
                NumberDocument = (row.DniApoderado ?? "").Trim(),
                Phone = (row.TelfCelApoderado ?? "").Trim(),
                Email = null,
                CreatedAt = now,
                CreatedBy = actor
            });
        }
    }

    private async Task ImportRowNoSaveAsync(
        PostulantImportRow row,
        Dictionary<string, Guid> modalityCache,
        Dictionary<string, Guid> typeModalityLookup,
        Dictionary<string, Guid> tipoPostulanteLookup,
        Dictionary<string, Guid> careers,
        Dictionary<string, Guid> countries,
        List<Distrit> distrits,
        Dictionary<string, Guid> schools,
        Dictionary<string, Guid> disabilityTypes,
        Dictionary<string, Guid> existingUsers,
        Dictionary<string, Guid> batchPostulantCache,
        Guid sinDiscapacidadId,
        string actor,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var dni = (row.Dni ?? "").Trim();
        if (string.IsNullOrEmpty(dni)) return;

        if (!existingUsers.TryGetValue(dni, out var userId))
        {
            var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Document == dni, ct);
            if (existingUser != null)
            {
                userId = existingUser.Id;
                existingUsers[dni] = userId;
            }
        }

        Guid postulantId;

        if (userId == Guid.Empty)
        {
            var user = new Users
            {
                Id = Guid.NewGuid(),
                Document = dni,
                DocumentType = "DNI",
                Name = (row.Nombres ?? "").Trim().ToUpper(),
                FirstNameFather = (row.Apaterno ?? "").Trim().ToUpper(),
                FirstNameMother = (row.Amaterno ?? "").Trim().ToUpper(),
                FullName = $"{row.Nombres} {row.Apaterno} {row.Amaterno}".Trim().ToUpper(),
                PhoneNumber = (row.Celular ?? "").Trim(),
                Email = (row.Correo ?? "").Trim(),
                Genero = NormalizeGenero(row.Sexo),
                CivilStatus = string.IsNullOrWhiteSpace(row.EstadoCivil) ? null : row.EstadoCivil.Trim(),
                Address = string.IsNullOrWhiteSpace(row.Direccion) ? null : row.Direccion.Trim().ToUpper(),
                Birthdate = ParseBirthdate(row.FechaNacimiento),
                CreatedAt = now,
                CreatedBy = actor
            };
            _context.Users.Add(user);
            userId = user.Id;

            var postulant = new Postulant
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CreatedAt = now,
                CreatedBy = actor
            };
            _context.Postulants.Add(postulant);
            postulantId = postulant.Id;

            existingUsers[dni] = userId;
            batchPostulantCache[dni] = postulantId;
        }
        else
        {
            if (!batchPostulantCache.TryGetValue(dni, out postulantId))
            {
                var existingPostulant = await _context.Postulants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == userId, ct);
                if (existingPostulant != null)
                {
                    postulantId = existingPostulant.Id;
                }
                else
                {
                    var postulant = new Postulant
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        CreatedAt = now,
                        CreatedBy = actor
                    };
                    _context.Postulants.Add(postulant);
                    postulantId = postulant.Id;
                    batchPostulantCache[dni] = postulantId;
                }
            }
        }

        var modalityId = row.Modalidad != null && modalityCache.TryGetValue(row.Modalidad, out var mid)
            ? mid
            : modalityCache.Values.FirstOrDefault();

        Guid? typeModalityId = null;
        if (!string.IsNullOrWhiteSpace(row.TipoModalidad) && typeModalityLookup.TryGetValue(RemoveDiacritics(row.TipoModalidad.Trim().ToUpper()), out var tmid))
            typeModalityId = tmid;

        Guid? tipoPostulanteId = null;
        if (!string.IsNullOrWhiteSpace(row.TipoPostulante) && tipoPostulanteLookup.TryGetValue(RemoveDiacritics(row.TipoPostulante.Trim().ToUpper()), out var tpid))
            tipoPostulanteId = tpid;

        var careerId = row.CodigoCarrera != null && careers.TryGetValue(row.CodigoCarrera, out var cid)
            ? cid
            : careers.Values.FirstOrDefault();

        var countryId = countries.Values.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(row.Pais) && countries.TryGetValue(RemoveDiacritics(row.Pais.Trim().ToUpper()), out var foundCountry))
            countryId = foundCountry;
        else             if (countries.TryGetValue(RemoveDiacritics("PERU"), out var peruId))
            countryId = peruId;

        var distritId = ResolveDistritId(row.CodUbigeo, distrits);

        Guid? schoolId = null;
        var otherSchool = (string?)null;
        if (!string.IsNullOrWhiteSpace(row.Colegio))
        {
            var schoolKey = RemoveDiacritics(row.Colegio.Trim().ToUpper());
            if (schools.TryGetValue(schoolKey, out var sid))
                schoolId = sid;
            else
                otherSchool = RemoveDiacritics(row.Colegio.Trim().ToUpper());
        }

        var inscriptionDate = ParseInscriptionDate(row.FechaInscripcion, now);

        var inscription = new Inscription
        {
            Id = Guid.NewGuid(),
            PostulantId = postulantId,
            ModalityId = modalityId != Guid.Empty ? modalityId : null,
            TypeModalityId = typeModalityId,
            TypePostulantInscriptionId = tipoPostulanteId,
            CareerId = careerId,
            CountryId = countryId,
            DistritId = distritId,
            CodePostulant = (row.CodigoPostulante ?? "").Trim(),
            State = AppConstants.InscripcionState.Pendiente,
            IsAdmission = false,
            CreatedAt = inscriptionDate,
            CreatedBy = actor,
            OtherSchool = otherSchool,
            SchoolId = schoolId,
            DJ = true
        };
        _context.Inscriptions.Add(inscription);

        var disabilityName = RemoveDiacritics((row.TipoDiscapacidad ?? "").Trim().ToUpper());
        Guid disabilityIdToUse;
        if (string.IsNullOrEmpty(disabilityName) || disabilityName is "SIN DISCAPACIDAD" or "NINGUNO" or "N/A" or "")
        {
            disabilityIdToUse = sinDiscapacidadId;
        }
        else
        {
            disabilityIdToUse = disabilityTypes.TryGetValue(disabilityName, out var did)
                ? did
                : sinDiscapacidadId;
        }

        _context.PostulantDisabilities.Add(new PostulantDisability
        {
            Id = Guid.NewGuid(),
            PostulantId = postulantId,
            DisabilityTypeId = disabilityIdToUse,
            CreatedAt = now,
            CreatedBy = actor
        });

        if (!string.IsNullOrWhiteSpace(row.DniApoderado) && !string.IsNullOrWhiteSpace(row.NombresApoderado))
        {
            var apoDni = (row.DniApoderado ?? "").Trim();
            var apellidos = (row.ApellidosApoderado ?? "").Trim().ToUpper();
            var nombresApo = (row.NombresApoderado ?? "").Trim().ToUpper();
            var fullNameApo = $"{nombresApo} {apellidos}".Trim();

            _context.Parents.Add(new Parent
            {
                Id = Guid.NewGuid(),
                PostulantId = postulantId,
                InscriptionId = inscription.Id,
                Name = nombresApo,
                FirstNameFather = apellidos,
                FirstNameMother = "",
                FullName = fullNameApo,
                TypeDocument = "DNI",
                NumberDocument = apoDni,
                Phone = (row.TelfCelApoderado ?? "").Trim(),
                Email = null,
                CreatedAt = now,
                CreatedBy = actor
            });
        }
    }

    public byte[] BuildPostulantsTemplate()
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Postulantes");

        var headers = new[]
        {
            "periodo", "fechainicio", "fechafin", "modalidad", "tipomodalidad",
            "codigo_postulante", "codigo_carrera", "fecha_inscripcion", "dni",
            "apaterno", "amaterno", "nombres", "Sexo", "fechanacimiento", "direccion",
            "estadocivil", "correo", "celular", "colegio", "tipopostulante",
            "tipodiscapacidad", "pais", "CodUbigeo", "dniApoderado",
            "apellidosApoderado", "nombresApoderado", "telf_celApoderado"
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
            "2024-II", "01/03/2024", "30/06/2024", "ORDINARIO", "",
            "202400001", "01", "05/03/2024", "70123456",
            "GARCIA", "LOPEZ", "JUAN CARLOS", "M", "01/01/2005", "AV. LOS ANDES 123",
            "SOLTERO", "juan@correo.com", "999888777", "I.E. JOSE CARLOS MARIATEGUI", "INGRESANTE",
            "SIN DISCAPACIDAD", "PERU", "040101", "70123457",
            "RODRIGUEZ RAMIREZ", "MARIA ELENA", "999888776"
        };
        for (int i = 0; i < example.Length; i++)
            ws.Cell(2, i + 1).SetValue(XLCellValue.FromObject(example[i]));

        ws.Cell(2, 1).Style.Font.SetItalic(true).Font.SetFontColor(XLColor.FromHtml("#9ca3af"));
        ws.Columns().AdjustToContents();

        AddInstructionsSheet(wb, "Importación de postulantes", new[]
        {
            "El sistema lee las columnas por POSICIÓN: la fila 1 es la cabecera y se ignora. Mantenga el orden de columnas tal como aparece.",
            "Complete un registro por fila a partir de la fila 2. La fila 2 es solo un ejemplo y puede reemplazarla.",
            "Columna 'modalidad': debe coincidir exactamente con una modalidad existente del período.",
            "Columna 'codigo_carrera': debe ser el código de una carrera existente.",
            "Columna 'dni': 8 dígitos. Columna 'dniApoderado': opcional.",
            "Fechas en formato dd/mm/aaaa (fechainicio, fechafin, fecha_inscripcion, fechanacimiento).",
            "Las columnas 'tipomodalidad', 'estadocivil', 'tipodiscapacidad', 'pais', 'telf_celApoderado' son opcionales."
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

    private List<PostulantImportRow> ParseExcel(Stream excelStream)
    {
        var rows = new List<PostulantImportRow>();
        using var workbook = new XLWorkbook(excelStream);
        var ws = workbook.Worksheet(1);
        var usedRows = ws.RangeUsed()?.RowsUsed().Skip(1) ?? Enumerable.Empty<IXLRangeRow>();

        foreach (var row in usedRows)
        {
            var r = new PostulantImportRow
            {
                RowNumber = row.RowNumber(),
                Periodo = row.Cell(1).GetString().Trim(),
                FechaInicio = row.Cell(2).GetString().Trim(),
                FechaFin = row.Cell(3).GetString().Trim(),
                Modalidad = row.Cell(4).GetString().Trim(),
                TipoModalidad = row.Cell(5).GetString().Trim(),
                CodigoPostulante = row.Cell(6).GetString().Trim(),
                CodigoCarrera = row.Cell(7).GetString().Trim(),
                FechaInscripcion = row.Cell(8).GetString().Trim(),
                Dni = row.Cell(9).GetString().Trim(),
                Apaterno = row.Cell(10).GetString().Trim(),
                Amaterno = row.Cell(11).GetString().Trim(),
                Nombres = row.Cell(12).GetString().Trim(),
                Sexo = row.Cell(13).GetString().Trim(),
                FechaNacimiento = row.Cell(14).GetString().Trim(),
                Direccion = row.Cell(15).GetString().Trim(),
                EstadoCivil = row.Cell(16).GetString().Trim(),
                Correo = row.Cell(17).GetString().Trim(),
                Celular = row.Cell(18).GetString().Trim(),
                Colegio = row.Cell(19).GetString().Trim(),
                TipoPostulante = row.Cell(20).GetString().Trim(),
                TipoDiscapacidad = row.Cell(21).GetString().Trim(),
                Pais = row.Cell(22).GetString().Trim(),
                CodUbigeo = row.Cell(23).GetString().Trim(),
                DniApoderado = row.Cell(24).GetString().Trim(),
                ApellidosApoderado = row.Cell(25).GetString().Trim(),
                NombresApoderado = row.Cell(26).GetString().Trim(),
                TelfCelApoderado = row.Cell(27).GetString().Trim()
            };
            rows.Add(r);
        }

        return rows;
    }

    private async Task ValidateRowsAsync(List<PostulantImportRow> rows, CancellationToken ct)
    {
        var allCareers = await _context.Careers.AsNoTracking().Select(c => c.Code).ToHashSetAsync(ct);
        var allDistrits = await _context.Distrits.AsNoTracking().Select(d => d.Code).ToHashSetAsync(ct);

        var codeByModality = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.CodigoPostulante) && !string.IsNullOrWhiteSpace(r.Modalidad))
            .GroupBy(r => new { Modalidad = r.Modalidad!.Trim(), Codigo = r.CodigoPostulante!.Trim() })
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Skip(1));

        foreach (var row in codeByModality)
            row.Errors.Add($"Código '{row.CodigoPostulante}' duplicado en la modalidad '{row.Modalidad}'");

        var modalityGroups = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.CodigoPostulante)
                     && !string.IsNullOrWhiteSpace(r.Modalidad)
                     && !string.IsNullOrWhiteSpace(r.Periodo))
            .GroupBy(r => new { r.Periodo, r.Modalidad });

        var existingCodeLookup = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in modalityGroups)
        {
            var term = await _context.Terms.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name == group.Key.Periodo, ct);
            if (term == null) continue;

            var modality = await _context.Modalities.AsNoTracking()
                .FirstOrDefaultAsync(m => m.TermId == term.Id
                    && m.Name.Trim().ToUpper() == group.Key.Modalidad!.Trim().ToUpper(), ct);
            if (modality == null) continue;

            var codes = await _context.Inscriptions.AsNoTracking()
                .Where(i => i.ModalityId == modality.Id && i.CodePostulant != null)
                .Select(i => i.CodePostulant!.Trim())
                .ToHashSetAsync(ct);

            if (codes.Count > 0)
                existingCodeLookup[group.Key.Modalidad!] = codes;
        }

        foreach (var row in rows)
        {
            if (!string.IsNullOrWhiteSpace(row.CodigoPostulante)
                && !string.IsNullOrWhiteSpace(row.Modalidad)
                && existingCodeLookup.TryGetValue(row.Modalidad, out var existing)
                && existing.Contains(row.CodigoPostulante!.Trim()))
            {
                row.Errors.Add($"Código '{row.CodigoPostulante}' ya existe en la modalidad '{row.Modalidad}'");
            }
        }

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Dni))
                row.Errors.Add("DNI es requerido");

            if (!string.IsNullOrWhiteSpace(row.CodigoCarrera) && !allCareers.Contains(row.CodigoCarrera))
                row.Errors.Add($"Carrera '{row.CodigoCarrera}' no encontrada");

            if (string.IsNullOrWhiteSpace(row.Periodo))
                row.Errors.Add("Periodo es requerido");

            if (!string.IsNullOrWhiteSpace(row.CodUbigeo) && !allDistrits.Contains(row.CodUbigeo))
                row.Errors.Add($"Código de ubigeo '{row.CodUbigeo}' no encontrado");
        }
    }

    private async Task<Guid> GetOrCreateSinDiscapacidadAsync(string actor, CancellationToken ct)
    {
        var existing = await _context.DisabilityTypes.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Name.ToUpper() == "SIN DISCAPACIDAD", ct);

        if (existing != null) return existing.Id;

        var newDt = new DisabilityType
        {
            Id = Guid.NewGuid(),
            Name = "Sin Discapacidad",
            Description = "Postulante sin ningún tipo de discapacidad",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = actor
        };
        _context.DisabilityTypes.Add(newDt);
        await _context.SaveChangesAsync(ct);
        return newDt.Id;
    }

    private static async Task<Dictionary<string, TValue>> ToDictionarySafeAsync<T, TValue>(
        IQueryable<T> query, Func<T, string> keySelector, Func<T, TValue> valueSelector, CancellationToken ct)
    {
        var dict = new Dictionary<string, TValue>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in await query.ToListAsync(ct))
        {
            var key = keySelector(item);
            if (!dict.ContainsKey(key))
                dict[key] = valueSelector(item);
        }
        return dict;
    }

    private static Guid? ResolveDistritId(string? codUbigeo, List<Distrit> distrits)
    {
        if (string.IsNullOrWhiteSpace(codUbigeo)) return null;

        var code = codUbigeo.Trim();
        var distrit = distrits.FirstOrDefault(d => d.Code == code);
        return distrit?.Id;
    }

    private static string NormalizeGenero(string? sexo)
    {
        if (string.IsNullOrWhiteSpace(sexo)) return "M";
        var s = sexo.Trim().ToUpper();
        return s switch
        {
            "F" or "FEMENINO" or "MUJER" => "F",
            _ => "M"
        };
    }

    private static DateTimeOffset ParseBirthdate(string? fecha)
    {
        if (string.IsNullOrWhiteSpace(fecha)) return DateTimeOffset.MinValue;

        if (DateTime.TryParseExact(fecha.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return new DateTimeOffset(dt, TimeSpan.Zero);

        if (DateTime.TryParse(fecha.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return new DateTimeOffset(dt, TimeSpan.Zero);

        return DateTimeOffset.MinValue;
    }

    private static DateTimeOffset ParseInscriptionDate(string? fecha, DateTimeOffset fallback)
    {
        if (string.IsNullOrWhiteSpace(fecha)) return fallback;
        var dateOnly = TryParseDate(fecha);
        return dateOnly.HasValue
            ? new DateTimeOffset(dateOnly.Value.Year, dateOnly.Value.Month, dateOnly.Value.Day, 0, 0, 0, TimeSpan.Zero)
            : fallback;
    }

    private static DateOnly? TryParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateOnly.TryParseExact(value.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        if (DateOnly.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
            return d;
        return null;
    }

    private static string ExtractYear(string periodoName)
    {
        var match = System.Text.RegularExpressions.Regex.Match(periodoName, @"\d{4}");
        return match.Success ? match.Value : DateTime.Now.Year.ToString();
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text ?? string.Empty;
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
