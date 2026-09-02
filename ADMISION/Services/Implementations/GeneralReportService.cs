using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.Models.ViewModels.Reports;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class GeneralReportService : IGeneralReportService
    {
        private readonly AppDbContext _context;

        public GeneralReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<GeneralReportViewModel> BuildAsync(GeneralReportFilter filter, CancellationToken ct = default)
        {
            var vm = new GeneralReportViewModel
            {
                TermId = filter.TermId,
                ModalityId = filter.ModalityId,
                TypeModalityId = filter.TypeModalityId,
                TypePostulantId = filter.TypePostulantId,
                Page = filter.Page,
                PageSize = filter.PageSize
            };

            if (filter.TermId.HasValue)
                vm.TermName = (await _context.Terms.AsNoTracking().FirstOrDefaultAsync(t => t.Id == filter.TermId.Value, ct))?.Name;
            if (filter.ModalityId.HasValue)
                vm.ModalityName = (await _context.Modalities.AsNoTracking().FirstOrDefaultAsync(m => m.Id == filter.ModalityId.Value, ct))?.Name;
            if (filter.TypeModalityId.HasValue)
                vm.TypeModalityName = (await _context.TypeModalities.AsNoTracking().FirstOrDefaultAsync(t => t.Id == filter.TypeModalityId.Value, ct))?.Name;
            if (filter.TypePostulantId.HasValue)
                vm.TypePostulantName = (await _context.TypePostulantInscriptions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == filter.TypePostulantId.Value, ct))?.Name;

            if (!filter.TermId.HasValue) return vm;

            var baseQuery = BuildBaseQuery(filter);
            vm.TotalRecords = await baseQuery.CountAsync(ct);

            var skip = (filter.Page - 1) * filter.PageSize;
            var items = await baseQuery
                .OrderBy(i => i.CodePostulant)
                .Skip(skip)
                .Take(filter.PageSize)
                .ToListAsync(ct);

            vm.Items = await MapItemsAsync(items, filter.TermId.Value, ct);
            return vm;
        }

        public async Task<List<GeneralReportItem>> BuildAllAsync(GeneralReportFilter filter, CancellationToken ct = default)
        {
            if (!filter.TermId.HasValue) return new List<GeneralReportItem>();

            var items = await BuildBaseQuery(filter)
                .OrderBy(i => i.CodePostulant)
                .ToListAsync(ct);

            return await MapItemsAsync(items, filter.TermId.Value, ct);
        }

        private IQueryable<ENTITIES.Models.Postulante.Inscription> BuildBaseQuery(GeneralReportFilter filter)
        {
            var query = _context.Inscriptions.AsNoTracking()
                .Include(i => i.Modality).ThenInclude(m => m!.Term)
                .Include(i => i.TypeModality)
                .Where(i => i.State == AppConstants.InscripcionState.Aprobado)
                .Where(i => i.Modality != null && i.Modality.TermId == filter.TermId!.Value);

            if (filter.ModalityId.HasValue)
                query = query.Where(i => i.ModalityId == filter.ModalityId.Value);
            if (filter.TypeModalityId.HasValue)
                query = query.Where(i => i.TypeModalityId == filter.TypeModalityId.Value);
            if (filter.TypePostulantId.HasValue)
                query = query.Where(i => i.TypePostulantInscriptionId == filter.TypePostulantId.Value);

            return query;
        }

        private async Task<List<GeneralReportItem>> MapItemsAsync(
            List<ENTITIES.Models.Postulante.Inscription> inscriptions,
            Guid termId,
            CancellationToken ct)
        {
            var careerIds = inscriptions.Select(i => i.CareerId).Distinct().ToList();
            var schoolIds = inscriptions.Where(i => i.SchoolId.HasValue).Select(i => i.SchoolId!.Value).Distinct().ToList();
            var districtIds = inscriptions.Where(i => i.DistritId.HasValue).Select(i => i.DistritId!.Value).Distinct().ToList();
            var countryIds = inscriptions.Select(i => i.CountryId).Distinct().ToList();
            var postulantIds = inscriptions.Select(i => i.PostulantId).Distinct().ToList();

            var tacPairs = await _context.TematicAreaCareers.AsNoTracking()
                .Where(tac => tac.TermId == termId && careerIds.Contains(tac.CareerId))
                .Select(tac => new { tac.CareerId, tac.TematicAreaId })
                .ToListAsync(ct);

            var tematicAreaIds = tacPairs.Select(t => t.TematicAreaId).Distinct().ToList();
            var tematicAreaNames = await _context.TematicAreas.AsNoTracking()
                .Where(a => tematicAreaIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, a => a.Code, ct);

            var tematicAreas = tacPairs
                .Where(t => tematicAreaNames.ContainsKey(t.TematicAreaId))
                .ToDictionary(t => t.CareerId, t => tematicAreaNames[t.TematicAreaId]);

            var careers = await _context.Careers.AsNoTracking()
                .Where(c => careerIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => new { c.Code, c.Name }, ct);

            var schools = await _context.Schools.AsNoTracking()
                .Include(s => s.Distrit)
                    .ThenInclude(d => d!.Province)
                        .ThenInclude(p => p!.Department)
                .Where(s => schoolIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, ct);

            var districts = await _context.Distrits.AsNoTracking()
                .Include(d => d.Province)
                    .ThenInclude(p => p!.Department)
                .Where(d => districtIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, ct);

            var countries = await _context.Countries.AsNoTracking()
                .Where(c => countryIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

            var postulants = await _context.Postulants.AsNoTracking()
                .Include(p => p.User)
                .Include(p => p.Disabilities)
                    .ThenInclude(d => d!.DisabilityType)
                .Where(p => postulantIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);

            var result = new List<GeneralReportItem>(inscriptions.Count);

            foreach (var i in inscriptions)
            {
                var postulant = postulants.TryGetValue(i.PostulantId, out var p) ? p : null;
                var user = postulant?.User;

                var career = careers.TryGetValue(i.CareerId, out var c) ? c : null;

                var school = i.SchoolId.HasValue && schools.TryGetValue(i.SchoolId.Value, out var s) ? s : null;

                var dist = i.DistritId.HasValue && districts.TryGetValue(i.DistritId.Value, out var d) ? d : null;

                var lastDisability = postulant?.Disabilities?
                    .Where(d => d.DisabilityType != null)
                    .OrderByDescending(d => d.CreatedAt)
                    .FirstOrDefault();

                var disabilityName = lastDisability?.DisabilityType?.Name;
                var hasDisability = disabilityName != null
                    && !disabilityName.Trim().Equals("Sin Discapacidad", StringComparison.OrdinalIgnoreCase);
                disabilityName = hasDisability ? disabilityName : null;

                var ubigeoCode = dist?.Code ?? "—";
                var isForeigner = i.CountryId != GetPeruCountryId(countries);
                if (isForeigner)
                {
                    ubigeoCode = "—";
                }

                result.Add(new GeneralReportItem
                {
                    TipoExamen = i.Modality?.Name ?? "—",
                    Modalidad = i.TypeModality?.Name ?? "—",
                    FechaInscripcion = i.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy"),
                    CodigoPostulante = i.CodePostulant ?? "—",
                    Apellidos = $"{user?.FirstNameFather ?? ""} {user?.FirstNameMother ?? ""}".Trim(),
                    Nombres = user?.Name ?? "—",
                    Documento = user?.Document ?? "—",
                    Sexo = user?.Genero ?? "—",
                    FechaNacimiento = user?.Birthdate != default ? user.Birthdate.ToLocalTime().ToString("dd/MM/yyyy") : "—",
                    Direccion = user?.Address ?? "—",
                    EstadoCivil = user?.CivilStatus ?? "—",
                    TieneDiscapacidad = hasDisability ? "SI" : "NO",
                    Discapacidad = disabilityName ?? "—",
                    Correo = user?.Email ?? "—",
                    Celular = user?.PhoneNumber ?? "—",
                    CodigoCarrera = career?.Code ?? "—",
                    CarreraProfesional = career?.Name ?? "—",
                    Tema = tematicAreas.TryGetValue(i.CareerId, out var tema) ? tema : "—",
                    Ciclo = i.Modality?.Term?.Name ?? "—",
                    UbigeoColegio = school?.Distrit?.Code ?? "—",
                    NombreColegio = school?.Name ?? "—",
                    DistritoColegio = school?.Distrit?.Name ?? "—",
                    ProvinciaColegio = school?.Distrit?.Province?.Name ?? "—",
                    DepartamentoColegio = school?.Distrit?.Province?.Department?.Name ?? "—",
                    Pais = countries.TryGetValue(i.CountryId, out var pais) ? pais : "—",
                    Ubigeo = ubigeoCode,
                    DistritoProcedencia = isForeigner ? "—" : (dist?.Name ?? "—"),
                    ProvinciaProcedencia = isForeigner ? "—" : (dist?.Province?.Name ?? "—"),
                    DepartamentoProcedencia = isForeigner ? "—" : (dist?.Province?.Department?.Name ?? "—")
                });
            }

            return result;
        }

        private static Guid? GetPeruCountryId(Dictionary<Guid, string> countries)
        {
            var entry = countries.FirstOrDefault(c =>
                c.Value.Equals("Perú", StringComparison.OrdinalIgnoreCase) ||
                c.Value.Equals("Peru", StringComparison.OrdinalIgnoreCase));
            return entry.Key != default ? entry.Key : null;
        }
    }
}
