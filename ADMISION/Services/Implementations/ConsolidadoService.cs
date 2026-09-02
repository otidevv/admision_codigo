using System.Collections.Concurrent;
using System.Security.Claims;
using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Exam;
using ADMISION.ENTITIES.Models.Postulante;
using ADMISION.Models.ViewModels.Admin;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations;

public class ConsolidadoService : IConsolidadoService
{
    private readonly AppDbContext _context;
    private readonly IExternalApiService _externalApi;
    private readonly IServiceScopeFactory _scopeFactory;

    public ConsolidadoService(AppDbContext context, IExternalApiService externalApi, IServiceScopeFactory scopeFactory)
    {
        _context = context;
        _externalApi = externalApi;
        _scopeFactory = scopeFactory;
    }

    public async Task<ConsolidadoPreviewViewModel> GetPreviewAsync(Guid? selectedTermId, ClaimsPrincipal? currentUser = null, string? remoteIp = null, CancellationToken ct = default)
    {
        var vm = new ConsolidadoPreviewViewModel
        {
            SelectedTermId = selectedTermId
        };

        vm.Terms = await _context.Terms
            .AsNoTracking()
            .OrderByDescending(t => t.Year).ThenByDescending(t => t.Number)
            .ToListAsync(ct);

        if (!selectedTermId.HasValue) return vm;

        var term = await _context.Terms.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == selectedTermId.Value, ct);

        if (term == null) return vm;

        vm.TermName = term.Name;

        var configs = await _context.PostulantTypeConfigs
            .AsNoTracking()
            .Where(c => c.TermId == selectedTermId.Value)
            .ToListAsync(ct);

        vm.Versions = await _context.ConsolidadoIngresantesVersions
            .AsNoTracking()
            .Where(v => v.TermId == selectedTermId.Value)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);

        var uniqueDnis = await GetUniqueDnisAsync(selectedTermId.Value, ct);
        var dniConCarreraPrevia = await FetchSegundaCarreraAsync(uniqueDnis, currentUser, remoteIp, ct);

        var sorted = await QuerySortedInscriptionsAsync(selectedTermId.Value, ct);
        var duplicates = DetectDuplicateDnis(sorted);
        vm.DuplicateDnis = duplicates;

        vm.Items = BuildPreviewItems(sorted, term, configs, dniConCarreraPrevia);
        return vm;
    }

    public async Task<ConsolidadoPreviewViewModel> GetEditAsync(Guid? selectedTermId, CancellationToken ct = default)
    {
        var vm = new ConsolidadoPreviewViewModel
        {
            SelectedTermId = selectedTermId
        };

        vm.Terms = await _context.Terms
            .AsNoTracking()
            .OrderByDescending(t => t.Year).ThenByDescending(t => t.Number)
            .ToListAsync(ct);

        if (!selectedTermId.HasValue) return vm;

        var term = await _context.Terms.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == selectedTermId.Value, ct);

        if (term == null) return vm;

        vm.TermName = term.Name;

        vm.Versions = await _context.ConsolidadoIngresantesVersions
            .AsNoTracking()
            .Where(v => v.TermId == selectedTermId.Value)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);

        var latestVersion = vm.Versions.FirstOrDefault();
        if (latestVersion == null) return vm;

        var records = await _context.ConsolidadoIngresantesRecords
            .AsNoTracking()
            .Where(r => r.VersionId == latestVersion.Id)
            .OrderBy(r => r.Nro)
            .ToListAsync(ct);

        vm.Items = records.Select(MapRecordToItem).ToList();
        return vm;
    }

    public async Task<ConsolidadoResult> ConfirmAsync(Guid termId, string createdBy, List<ConsolidadoPreviewItem>? previewItems = null)
    {
        var configs = await _context.PostulantTypeConfigs
            .AsNoTracking()
            .Where(c => c.TermId == termId)
            .ToListAsync();

        var term = await _context.Terms.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == termId);

        if (term == null)
            return new ConsolidadoResult { Success = false, Message = "Período no encontrado." };

        var sorted = await QuerySortedInscriptionsAsync(termId);
        var dniConCarreraPrevia = await FetchSegundaCarreraCachedAsync(termId);

        var items = BuildPreviewItems(sorted, term, configs, dniConCarreraPrevia);

        ApplySegundaCarreraOverrides(items, previewItems);

        if (items.Count == 0)
            return new ConsolidadoResult { Success = false, Message = "No hay ingresantes para consolidar en este período." };

        var maxVersion = await _context.ConsolidadoIngresantesVersions
            .Where(v => v.TermId == termId)
            .MaxAsync(v => (int?)v.VersionNumber) ?? 0;

        var newVersionNumber = maxVersion + 1;

        var existingLatest = await _context.ConsolidadoIngresantesVersions
            .Where(v => v.TermId == termId && v.IsLatest)
            .ToListAsync();

        foreach (var v in existingLatest)
            v.IsLatest = false;

        var version = new ConsolidadoIngresantesVersion
        {
            Id = Guid.NewGuid(),
            TermId = termId,
            VersionNumber = newVersionNumber,
            IsLatest = true,
            RecordCount = items.Count,
            CreatedBy = createdBy
        };

        _context.ConsolidadoIngresantesVersions.Add(version);

        var records = items
            .Select(item => MapToRecord(item, termId, version.Id, createdBy))
            .ToList();

        _context.ConsolidadoIngresantesRecords.AddRange(records);
        await _context.SaveChangesAsync();

        return new ConsolidadoResult
        {
            Success = true,
            Message = $"Consolidado generado exitosamente (Versión {newVersionNumber}).",
            RecordsSaved = items.Count,
            VersionNumber = newVersionNumber
        };
    }

    public async Task<ConsolidadoResult> AddIngresanteAsync(Guid termId, string? codePostulant, string createdBy, CancellationToken ct = default)
    {
        var term = await _context.Terms.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == termId, ct);
        if (term == null)
            return new ConsolidadoResult { Success = false, Message = "Período no encontrado." };

        codePostulant = (codePostulant ?? "").Trim();
        if (codePostulant.Length == 0)
            return new ConsolidadoResult { Success = false, Message = "Debe ingresar un código de postulante." };

        var inscription = await _context.Inscriptions
            .AsNoTracking()
            .Include(i => i.Modality)
            .Include(i => i.Career)
            .Include(i => i.TypeModality)
            .Include(i => i.Postulant).ThenInclude(p => p!.User)
            .Include(i => i.Distrit)
            .Include(i => i.Observations!)
            .FirstOrDefaultAsync(i =>
                i.CodePostulant == codePostulant
                && i.Modality != null
                && i.Modality.TermId == termId, ct);

        if (inscription == null)
            return new ConsolidadoResult { Success = false, Message = $"No se encontró inscripción con código «{codePostulant}» en el período seleccionado." };

        bool hasResignation = await _context.Resignations
            .AnyAsync(r => r.InscriptionId == inscription.Id, ct);
        if (hasResignation)
            return new ConsolidadoResult { Success = false, Message = "La inscripción tiene un registro de renuncia y no puede añadirse al consolidado." };

        bool yaEnConsolidado = await _context.ConsolidadoIngresantesRecords
            .AnyAsync(r => r.TermId == termId && r.InscriptionId == inscription.Id, ct);
        if (yaEnConsolidado)
            return new ConsolidadoResult { Success = false, Message = "El postulante ya tiene un registro en el consolidado." };

        var configs = await _context.PostulantTypeConfigs
            .AsNoTracking()
            .Where(c => c.TermId == termId)
            .ToListAsync(ct);

        // Determinar si el nuevo ingresante tiene una segunda carrera previa.
        var dni = inscription.Postulant?.User?.Document;

        // Validar por DNI: el postulante pudo haber ingresado por otra inscripción
        // o carrera y ya constar dentro del consolidado vigente del período.
        if (!string.IsNullOrWhiteSpace(dni))
        {
            var dniNormalizado = dni.Trim();
            bool dniYaEnConsolidado = await _context.ConsolidadoIngresantesRecords
                .AsNoTracking()
                .AnyAsync(r => r.TermId == termId && r.DNI != null && r.DNI.Trim() == dniNormalizado, ct);
            if (dniYaEnConsolidado)
                return new ConsolidadoResult
                {
                    Success = false,
                    Message = "El DNI del postulante ya tiene un registro dentro del consolidado. No se añadirá al consolidado."
                };
        }

        var dniConCarreraPrevia = new HashSet<string>();
        if (!string.IsNullOrWhiteSpace(dni))
        {
            bool tieneCarrera = await _context.ExternalAcademicInfos
                .AsNoTracking()
                .AnyAsync(e => e.Dni == dni, ct);
            if (tieneCarrera) dniConCarreraPrevia.Add(dni);
        }

        // Identificar la versión anterior (consolidado actual) para copiarla.
        var maxVersion = await _context.ConsolidadoIngresantesVersions
            .Where(v => v.TermId == termId)
            .MaxAsync(v => (int?)v.VersionNumber) ?? 0;
        var newVersionNumber = maxVersion + 1;

        var existingLatest = await _context.ConsolidadoIngresantesVersions
            .Where(v => v.TermId == termId && v.IsLatest)
            .ToListAsync(ct);

        var latestVersion = existingLatest.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        var previousRecords = latestVersion != null
            ? await _context.ConsolidadoIngresantesRecords
                .AsNoTracking()
                .Where(r => r.VersionId == latestVersion.Id)
                .ToListAsync(ct)
            : new List<ConsolidadoIngresantesRecord>();

        // El código de estudiante del nuevo ingresante será el último correlativo
        // de su programa académico (carrera): máximo existente en la versión + 1.
        var yearSuffix = term.Year.Length >= 2 ? term.Year[^2..] : term.Year;
        var termNumber = term.Number.ToString();
        var programNumber = inscription.Career?.ProgramNumber ?? "";
        var prefix = $"{yearSuffix}{termNumber}{programNumber}";
        var careerCode = inscription.Career?.Code ?? "";

        var maxCorrelativo = previousRecords
            .Where(r => string.Equals(r.CodigoCarrera, careerCode, StringComparison.OrdinalIgnoreCase))
            .Select(r => ExtractCorrelativo(r.CodigoEstudiante, prefix))
            .DefaultIfEmpty(0)
            .Max();
        var newCorrelativo = maxCorrelativo + 1;
        var codigoEstudiante = $"{prefix}{newCorrelativo:D3}";

        var newNro = previousRecords.Count == 0
            ? 1
            : previousRecords.Max(r => r.Nro) + 1;

        var newItem = MapInscriptionToItem(inscription, term, configs, dniConCarreraPrevia, codigoEstudiante, newNro);

        // Nueva versión: copia los registros anteriores y añade el nuevo estudiante.
        foreach (var v in existingLatest)
            v.IsLatest = false;

        var version = new ConsolidadoIngresantesVersion
        {
            Id = Guid.NewGuid(),
            TermId = termId,
            VersionNumber = newVersionNumber,
            IsLatest = true,
            RecordCount = previousRecords.Count + 1,
            CreatedBy = createdBy
        };
        _context.ConsolidadoIngresantesVersions.Add(version);

        var copied = previousRecords.Select(r => new ConsolidadoIngresantesRecord
        {
            Id = Guid.NewGuid(),
            TermId = termId,
            VersionId = version.Id,
            InscriptionId = r.InscriptionId,
            CodigoEstudiante = r.CodigoEstudiante,
            CodigoCarrera = r.CodigoCarrera,
            SegundaCarrera = r.SegundaCarrera,
            Semestre = r.Semestre,
            Nombres = r.Nombres,
            Paterno = r.Paterno,
            Materno = r.Materno,
            DType = r.DType,
            DNI = r.DNI,
            Email = r.Email,
            Celular = r.Celular,
            Direccion = r.Direccion,
            FechaNacimiento = r.FechaNacimiento,
            Sexo = r.Sexo,
            EstadoCivil = r.EstadoCivil,
            Ubigeo = r.Ubigeo,
            TipoPostulante = r.TipoPostulante,
            TipoObs = r.TipoObs,
            Observaciones = r.Observaciones,
            Nro = r.Nro,
            CreatedBy = createdBy
        }).ToList();

        copied.Add(MapToRecord(newItem, termId, version.Id, createdBy));

        _context.ConsolidadoIngresantesRecords.AddRange(copied);
        await _context.SaveChangesAsync(ct);

        return new ConsolidadoResult
        {
            Success = true,
            Message = $"Ingresante añadido (Versión {newVersionNumber}). Código de estudiante: {codigoEstudiante}.",
            RecordsSaved = copied.Count,
            VersionNumber = newVersionNumber
        };
    }

    public async Task<ConsolidadoResult> SaveEditsAsync(Guid termId, string createdBy, List<ConsolidadoPreviewItem>? editItems, CancellationToken ct = default)
    {
        var existingLatest = await _context.ConsolidadoIngresantesVersions
            .Where(v => v.TermId == termId && v.IsLatest)
            .ToListAsync(ct);

        var latestVersion = existingLatest.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
        if (latestVersion == null)
            return new ConsolidadoResult { Success = false, Message = "No existe una versión del consolidado que editar." };

        var records = await _context.ConsolidadoIngresantesRecords
            .AsNoTracking()
            .Where(r => r.VersionId == latestVersion.Id)
            .OrderBy(r => r.Nro)
            .ToListAsync(ct);

        if (records.Count == 0)
            return new ConsolidadoResult { Success = false, Message = "La versión actual del consolidado no tiene registros." };

        // Mapa de ediciones para reaplicar sobre cada registro copiado.
        // Se prioriza el cruce por inscripción; los registros sin inscripción
        // (InscriptionId nulo) se cruzan por su número de fila.
        var byInscription = (editItems ?? new List<ConsolidadoPreviewItem>())
            .Where(i => i.InscriptionId != Guid.Empty)
            .ToDictionary(i => i.InscriptionId);
        var byNro = (editItems ?? new List<ConsolidadoPreviewItem>())
            .Where(i => i.InscriptionId == Guid.Empty)
            .ToDictionary(i => i.Nro);

        var maxVersion = await _context.ConsolidadoIngresantesVersions
            .Where(v => v.TermId == termId)
            .MaxAsync(v => (int?)v.VersionNumber) ?? 0;
        var newVersionNumber = maxVersion + 1;

        foreach (var v in existingLatest)
            v.IsLatest = false;

        var version = new ConsolidadoIngresantesVersion
        {
            Id = Guid.NewGuid(),
            TermId = termId,
            VersionNumber = newVersionNumber,
            IsLatest = true,
            RecordCount = records.Count,
            CreatedBy = createdBy
        };
        _context.ConsolidadoIngresantesVersions.Add(version);

        var copied = records.Select(r =>
        {
            var edited = byInscription.TryGetValue(r.InscriptionId.GetValueOrDefault(), out var byIns) ? byIns
                : (r.InscriptionId == null && byNro.TryGetValue(r.Nro, out var byRow) ? byRow : null);

            var segundaCarrera = edited?.SegundaCarrera ?? r.SegundaCarrera;
            var tipoObs = edited?.TipoObs ?? r.TipoObs;
            var observaciones = edited != null
                ? (string.IsNullOrWhiteSpace(edited.Observaciones) ? null : edited.Observaciones.Trim())
                : r.Observaciones;

            return new ConsolidadoIngresantesRecord
            {
                Id = Guid.NewGuid(),
                TermId = termId,
                VersionId = version.Id,
                InscriptionId = r.InscriptionId,
                CodigoEstudiante = r.CodigoEstudiante,
                CodigoCarrera = r.CodigoCarrera,
                SegundaCarrera = segundaCarrera,
                Semestre = r.Semestre,
                Nombres = r.Nombres,
                Paterno = r.Paterno,
                Materno = r.Materno,
                DType = r.DType,
                DNI = r.DNI,
                Email = r.Email,
                Celular = r.Celular,
                Direccion = r.Direccion,
                FechaNacimiento = r.FechaNacimiento,
                Sexo = r.Sexo,
                EstadoCivil = r.EstadoCivil,
                Ubigeo = r.Ubigeo,
                TipoPostulante = r.TipoPostulante,
                TipoObs = tipoObs,
                Observaciones = observaciones,
                Nro = r.Nro,
                CreatedBy = createdBy
            };
        }).ToList();

        _context.ConsolidadoIngresantesRecords.AddRange(copied);
        await _context.SaveChangesAsync(ct);

        return new ConsolidadoResult
        {
            Success = true,
            Message = $"Cambios guardados en una nueva versión (Versión {newVersionNumber}).",
            RecordsSaved = copied.Count,
            VersionNumber = newVersionNumber
        };
    }

    private static int ExtractCorrelativo(string? codigoEstudiante, string prefix)
    {
        if (string.IsNullOrEmpty(codigoEstudiante) || !codigoEstudiante.StartsWith(prefix, StringComparison.Ordinal))
            return 0;

        return int.TryParse(codigoEstudiante[prefix.Length..], out var value) ? value : 0;
    }

    private static void ApplySegundaCarreraOverrides(List<ConsolidadoPreviewItem> items, List<ConsolidadoPreviewItem>? previewItems)
    {
        if (previewItems is not { Count: > 0 }) return;

        var overrides = previewItems
            .Where(p => p.InscriptionId != Guid.Empty)
            .ToDictionary(p => p.InscriptionId, p => p.SegundaCarrera == "1" ? "1" : "0");

        foreach (var item in items)
        {
            if (overrides.TryGetValue(item.InscriptionId, out var value))
                item.SegundaCarrera = value;
        }
    }

    private async Task<List<Inscription>> QuerySortedInscriptionsAsync(Guid termId, CancellationToken ct = default)
    {
        var inscriptions = await _context.Inscriptions
            .AsNoTracking()
            .Include(i => i.Modality)
            .Include(i => i.Career)
            .Include(i => i.TypeModality)
            .Include(i => i.Postulant).ThenInclude(p => p!.User)
            .Include(i => i.Distrit)
            .Include(i => i.Observations!)
            .Where(i => i.IsAdmission && i.Modality != null && i.Modality.TermId == termId
                && !_context.Resignations.Any(r => r.InscriptionId == i.Id))
            .ToListAsync(ct);

        return inscriptions
            .OrderBy(i => i.Modality!.Orden)
            .ThenBy(i => i.InscriptionOrder)
            .ThenBy(i => i.Grade)
            .ToList();
    }

    private async Task<List<string>> GetUniqueDnisAsync(Guid termId, CancellationToken ct = default)
    {
        var inscriptions = await _context.Inscriptions
            .AsNoTracking()
            .Include(i => i.Postulant!).ThenInclude(p => p.User)
            .Where(i => i.IsAdmission && i.Modality != null && i.Modality.TermId == termId
                && !_context.Resignations.Any(r => r.InscriptionId == i.Id))
            .ToListAsync(ct);

        return inscriptions
            .Select(i => i.Postulant?.User?.Document)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct()
            .ToList()!;
    }

    private async Task<HashSet<string>> FetchSegundaCarreraAsync(List<string> dnis, ClaimsPrincipal? currentUser, string? remoteIp, CancellationToken ct = default)
    {
        var apiAcademica = await _externalApi.FindApiByCategoryAsync("Academic", ct);
        if (apiAcademica == null) return new HashSet<string>();

        var apiId = apiAcademica.Id;
        var user = currentUser ?? new ClaimsPrincipal();
        var resultados = new ConcurrentBag<(string dni, bool tieneCarrera)>();

        await Parallel.ForEachAsync(dnis, new ParallelOptions { MaxDegreeOfParallelism = 5, CancellationToken = ct }, async (dni, token) =>
        {
            using var scope = _scopeFactory.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IExternalApiService>();

            await api.FetchAndSaveAcademicAsync(apiId, dni, user, remoteIp, token);
            var info = await api.GetAcademicInfoByDniAsync(dni, token);
            resultados.Add((dni, info is { Count: > 0 }));
        });

        return resultados.Where(r => r.tieneCarrera).Select(r => r.dni).ToHashSet();
    }

    private async Task<HashSet<string>> FetchSegundaCarreraCachedAsync(Guid termId, CancellationToken ct = default)
    {
        var dnis = await _context.Inscriptions
            .AsNoTracking()
            .Include(i => i.Postulant!).ThenInclude(p => p.User)
            .Where(i => i.IsAdmission && i.Modality != null && i.Modality.TermId == termId
                && !_context.Resignations.Any(r => r.InscriptionId == i.Id))
            .Select(i => i.Postulant!.User!.Document)
            .Where(d => d != null)
            .Distinct()
            .ToListAsync(ct);

        if (dnis.Count == 0) return new HashSet<string>();

        var dnisConCarrera = await _context.ExternalAcademicInfos
            .AsNoTracking()
            .Where(e => dnis.Contains(e.Dni))
            .Select(e => e.Dni)
            .Distinct()
            .ToListAsync(ct);

        return dnisConCarrera.ToHashSet();
    }

    private static List<string> DetectDuplicateDnis(List<Inscription> sorted)
    {
        var dniGroups = sorted
            .Select(i => i.Postulant?.User?.Document)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .GroupBy(d => d!)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        return sorted
            .Where(i => dniGroups.Contains(i.Postulant?.User?.Document ?? ""))
            .Select(i => $"{i.Postulant?.User?.Document} — {i.Postulant?.User?.Name} {i.Postulant?.User?.FirstNameFather} {i.Postulant?.User?.FirstNameMother}")
            .Distinct()
            .ToList();
    }

    private static List<ConsolidadoPreviewItem> BuildPreviewItems(
        List<Inscription> sorted,
        ENTITIES.Models.Modality.Term term,
        List<PostulantTypeConfig> configs,
        HashSet<string> dniConCarreraPrevia)
    {
        var yearSuffix = term.Year.Length >= 2 ? term.Year[^2..] : term.Year;
        var termNumber = term.Number.ToString();
        var items = new List<ConsolidadoPreviewItem>();

        // El número de ingreso (últimos 3 dígitos del código de estudiante) es
        // un correlativo global DENTRO de cada programa académico (carrera).
        // Se reinicia por cada combinación de programa + período + proceso de admisión.
        var recordsByCareer = sorted
            .GroupBy(i => i.CareerId)
            .OrderBy(g => NumericProgram(g.First().Career?.ProgramNumber))
            .ThenBy(g => g.First().Career?.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var careerGroup in recordsByCareer)
        {
            int orderNumber = 1;

            foreach (var ins in OrderInscriptionsForCorrelativo(careerGroup.ToList()))
            {
                var programNumber = ins.Career?.ProgramNumber ?? "";
                var codigoEstudiante = $"{yearSuffix}{termNumber}{programNumber}{orderNumber:D3}";

                items.Add(MapInscriptionToItem(ins, term, configs, dniConCarreraPrevia, codigoEstudiante, orderNumber));

                orderNumber++;
            }
        }

        return items;
    }

    /// <summary>
    /// Proyecta una inscripción a un elemento del consolidado con todos los
    /// campos necesarios para el registro, usando el código de estudiante dado.
    /// </summary>
    private static ConsolidadoPreviewItem MapInscriptionToItem(
        Inscription ins,
        ENTITIES.Models.Modality.Term term,
        List<PostulantTypeConfig> configs,
        HashSet<string> dniConCarreraPrevia,
        string codigoEstudiante,
        int nro)
    {
        var user = ins.Postulant?.User;
        var tipoPostulanteIndex = ResolveTipoPostulante(ins, configs);
        var ubigeo = string.IsNullOrWhiteSpace(ins.Distrit?.Code)
            ? AppConstants.ConsolidadoMapping.UbigeoDefault
            : ins.Distrit.Code;
        var (tipoObs, obsLabel) = ResolveTipoObservacion(ins);

        return new ConsolidadoPreviewItem
        {
            Nro = nro,
            CodigoEstudiante = codigoEstudiante,
            CodigoCarrera = ins.Career?.Code ?? "",
            SegundaCarrera = user?.Document != null && dniConCarreraPrevia.Contains(user.Document) ? "1" : "0",
            Semestre = term.Name,
            Nombres = user?.Name ?? "",
            Paterno = user?.FirstNameFather ?? "",
            Materno = user?.FirstNameMother ?? "",
            DType = AppConstants.ConsolidadoMapping.MapTipoDocumento(user?.DocumentType),
            DNI = user?.Document ?? "",
            Email = user?.Email ?? "",
            Celular = user?.PhoneNumber ?? "",
            Direccion = user?.Address ?? "",
            FechaNacimiento = user?.Birthdate.ToString("yyyy-MM-dd") ?? "",
            Sexo = AppConstants.ConsolidadoMapping.MapGenero(user?.Genero),
            EstadoCivil = AppConstants.ConsolidadoMapping.MapEstadoCivil(user?.CivilStatus),
            Ubigeo = ubigeo,
            TipoPostulante = tipoPostulanteIndex.ToString(),
            TipoObs = tipoObs,
            Observaciones = obsLabel,
            InscriptionId = ins.Id
        };
    }

    /// <summary>
    /// Convierte un elemento del consolidado en un registro persistible de la versión.
    /// </summary>
    private static ConsolidadoIngresantesRecord MapToRecord(ConsolidadoPreviewItem item, Guid termId, Guid versionId, string createdBy)
    {
        return new ConsolidadoIngresantesRecord
        {
            Id = Guid.NewGuid(),
            TermId = termId,
            VersionId = versionId,
            InscriptionId = item.InscriptionId,
            CodigoEstudiante = item.CodigoEstudiante,
            CodigoCarrera = item.CodigoCarrera,
            SegundaCarrera = item.SegundaCarrera,
            Semestre = item.Semestre,
            Nombres = item.Nombres,
            Paterno = item.Paterno,
            Materno = item.Materno,
            DType = item.DType,
            DNI = item.DNI,
            Email = item.Email,
            Celular = item.Celular,
            Direccion = item.Direccion,
            FechaNacimiento = item.FechaNacimiento,
            Sexo = item.Sexo,
            EstadoCivil = item.EstadoCivil,
            Ubigeo = item.Ubigeo,
            TipoPostulante = item.TipoPostulante,
            TipoObs = item.TipoObs,
            Observaciones = item.Observaciones,
            Nro = item.Nro,
            CreatedBy = createdBy
        };
    }

    /// <summary>
    /// Convierte un registro persistido de una versión en un elemento editable
    /// del consolidado (para la vista de edición).
    /// </summary>
    private static ConsolidadoPreviewItem MapRecordToItem(ConsolidadoIngresantesRecord r)
    {
        return new ConsolidadoPreviewItem
        {
            Nro = r.Nro,
            CodigoEstudiante = r.CodigoEstudiante,
            CodigoCarrera = r.CodigoCarrera,
            SegundaCarrera = r.SegundaCarrera == "1" ? "1" : "0",
            Semestre = r.Semestre ?? "",
            Nombres = r.Nombres,
            Paterno = r.Paterno,
            Materno = r.Materno,
            DType = r.DType ?? "",
            DNI = r.DNI ?? "",
            Email = r.Email ?? "",
            Celular = r.Celular ?? "",
            Direccion = r.Direccion ?? "",
            FechaNacimiento = r.FechaNacimiento ?? "",
            Sexo = r.Sexo ?? "",
            EstadoCivil = r.EstadoCivil ?? "",
            Ubigeo = r.Ubigeo ?? "",
            TipoPostulante = r.TipoPostulante ?? "",
            TipoObs = r.TipoObs,
            Observaciones = r.Observaciones,
            InscriptionId = r.InscriptionId ?? Guid.Empty
        };
    }

    /// <summary>
    /// Ordena las inscripciones de una misma carrera según la jerarquía de
    /// modalidad. Para la codificación de los códigos de estudiante la
    /// prioridad es: medicina humana(1, examen diferenciado), ordinario(2),
    /// secundaria(3), dirimencia(4), CEPRE(5); cualquier otro valor se
    /// procesa al final.
    ///
    /// Ordinario, Secundaria, Dirimencia y Medicina usan su orden de mérito
    /// oficial. CEPRE no tiene orden de mérito: se ordena por puntaje de mayor
    /// a menor. Si en una carrera conviven admitidos de la modalidad de
    /// Medicina Humana con otras modalidades, los primeros se codifican antes
    /// y los demás van después, todos dentro del mismo programa de estudios.
    /// </summary>
    private static List<Inscription> OrderInscriptionsForCorrelativo(List<Inscription> careerRecords)
    {
        var medicinaOrden = AppConstants.ConsolidadoModalityOrden.MedicinaHumana;
        var tieneMedicina = careerRecords.Any(i => i.Modality?.Orden == medicinaOrden);

        // Medicina Humana: primero el examen diferenciado (Orden = 5) con su
        // propio orden de mérito; los admitidos por otras modalidades se
        // codifican después, al mismo programa de estudios.
        if (tieneMedicina)
        {
            var medicina = careerRecords
                .Where(i => i.Modality?.Orden == medicinaOrden)
                .OrderBy(i => i.InscriptionOrder)
                .ToList();

            medicina.AddRange(OrderModalityGroups(
                careerRecords.Where(i => i.Modality?.Orden != medicinaOrden).ToList()));

            return medicina;
        }

        return OrderModalityGroups(careerRecords);
    }

    /// <summary>
    /// Procesa por grupos de modalidad en orden jerárquico para poder aplicar
    /// el criterio interno de orden correcto a cada una.
    /// </summary>
    private static List<Inscription> OrderModalityGroups(List<Inscription> records)
    {
        var result = new List<Inscription>();

        foreach (var group in records
            .GroupBy(i => i.Modality?.Orden ?? int.MaxValue)
            .OrderBy(g => ModalityPriority(g.Key)))
        {
            var orden = group.Key;
            var sortedGroup = IsCepreOrden(orden)
                ? group.OrderByDescending(i => i.GradeAdmission ?? 0).ToList()   // CEPRE: mayor puntaje
                : group.OrderBy(i => i.InscriptionOrder).ToList();               // resto: orden de mérito

            result.AddRange(sortedGroup);
        }

        return result;
    }

    private static bool IsCepreOrden(int? orden)
        => orden == AppConstants.ConsolidadoModalityOrden.Cepre;

    private static int ModalityPriority(int? orden)
    {
        if (orden == null) return 6;

        return orden.Value switch
        {
            // Medicina Humana (examen diferenciado) se codifica primero.
            AppConstants.ConsolidadoModalityOrden.MedicinaHumana => 1,
            AppConstants.ConsolidadoModalityOrden.Ordinario => 2,
            AppConstants.ConsolidadoModalityOrden.Secundaria => 3,
            AppConstants.ConsolidadoModalityOrden.Dirimencia => 4,
            AppConstants.ConsolidadoModalityOrden.Cepre => 5,
            _ => 6
        };
    }

    private static int NumericProgram(string? programNumber)
    {
        return int.TryParse(programNumber, out var value) ? value : int.MaxValue;
    }

    private static (string TipoObs, string Observaciones) ResolveTipoObservacion(Inscription ins)
    {
        var labels = AppConstants.ConsolidadoMapping.TipoObservacion.Labels;
        var ninguno = AppConstants.ConsolidadoMapping.TipoObservacion.Ninguna;

        var obs = ins.Observations?
            .Where(o => !string.IsNullOrWhiteSpace(o.TipoObservacion))
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefault();

        if (obs == null)
            return (ninguno, labels[ninguno]);

        return (obs.TipoObservacion!, obs.Observation);
    }

    private static int ResolveTipoPostulante(
        Inscription ins,
        List<PostulantTypeConfig> configs)
    {
        var esCepre = ins.Modality?.IsCepreExam == true;

        var match = configs.FirstOrDefault(c =>
            (c.CareerId.HasValue && esCepre && c.CareerId.Value == ins.CareerId) ||
            (c.ModalityId.HasValue && ins.ModalityId.HasValue && c.ModalityId.Value == ins.ModalityId.Value) ||
            (c.TypeModalityId.HasValue && ins.TypeModalityId.HasValue && c.TypeModalityId.Value == ins.TypeModalityId.Value));

        return match?.Index ?? 18;
    }
}