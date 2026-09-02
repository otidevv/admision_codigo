using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Postulante;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.InfoPostulant.PostulantController
{
    [Route("admin/info-postulant/list")]
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin + "," + AppConstants.Roles.Consultor)]
    public class PostulantsController : Controller
    {
        private static readonly List<string> _inscriptionStates = new()
        {
            AppConstants.InscripcionState.Pendiente,
            AppConstants.InscripcionState.Aprobado,
            AppConstants.InscripcionState.Observado,
            AppConstants.InscripcionState.Rechazado,
            AppConstants.InscripcionState.Retirado
        };

        private readonly IPostulantQueryService _postulants;
        private readonly ICatalogService _catalog;
        private readonly IUbigeoService _ubigeo;

        public PostulantsController(IPostulantQueryService postulants, ICatalogService catalog, IUbigeoService ubigeo)
        {
            _postulants = postulants;
            _catalog = catalog;
            _ubigeo = ubigeo;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            Guid? areaId, Guid? termId, Guid? careerId, Guid? facultyId,
            Guid? modalityId, Guid? typeModalityId, Guid? typePostulantId,
            string? state, string? search,
            int page = 1, int pageSize = 20, string? sortBy = null, string? sortDir = "desc",
            CancellationToken ct = default)
        {
            var query = new PostulantInscriptionListQuery
            {
                AreaId = areaId,
                TermId = termId,
                CareerId = careerId,
                FacultyId = facultyId,
                ModalityId = modalityId,
                TypeModalityId = typeModalityId,
                TypePostulantId = typePostulantId,
                State = state,
                Search = search,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                SortDir = sortDir
            };

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var paged = await _postulants.ListAsync(query, ct);
                var response = new DataTableResponse<object>
                {
                    Data = paged.Items.Select(i => (object)new
                    {
                        i.Id,
                        i.PostulantId,
                        i.CodePostulant,
                        CreatedAt = i.CreatedAt.ToOffset(TimeSpan.FromHours(-5)),
                        i.State,
                        Postulant = i.FullName == null ? null : new
                        {
                            User = new { FullName = i.FullName, Document = i.Document, DocumentType = i.DocumentType }
                        },
                        Career = i.CareerName == null ? null : new { Name = i.CareerName, TematicArea = i.CareerArea },
                        Modality = i.ModalityName == null ? null : new { Name = i.ModalityName },
                        TypeModality = i.TypeModalityName == null ? null : new { Name = i.TypeModalityName }
                    }).ToList(),
                    TotalItems = paged.TotalItems,
                    TotalPages = paged.TotalPages,
                    PageSize = paged.PageSize,
                    CurrentPage = paged.Page
                };
                return Json(response);
            }

            // Datos para filtros del listado.
            var allTerms = await _catalog.GetTermsAsync(ct: ct);
            var activeTerms = await _catalog.GetTermsAsync(onlyActive: true, ct: ct);
            ViewBag.Terms = allTerms;
            // Periodo por defecto: el activo; si no existe, el último registrado.
            ViewBag.DefaultTermId = activeTerms.FirstOrDefault()?.Id ?? allTerms.FirstOrDefault()?.Id;
            ViewBag.Faculties = await _catalog.GetFacultiesAsync(ct);
            ViewBag.Modalities = await _catalog.GetModalitiesAsync(onlyActive: true, ct: ct);
            ViewBag.TypePostulants = await _catalog.GetTypePostulantsAsync(ct);
            ViewBag.States = _inscriptionStates;

            return View("~/Pages/Admin/InfoPostulant/Postulants/Index.cshtml");
        }

        // ============ Lookups dependientes (cascada de filtros) ============
        [HttpGet("GetCareersByFaculty/{facultyId}")]
        public async Task<IActionResult> GetCareersByFaculty(Guid facultyId, CancellationToken ct)
        {
            var careers = await _catalog.GetCareersAsync(facultyId, ct: ct);
            return Json(careers.Select(c => new { id = c.Id, name = c.Name }));
        }

        [HttpGet("GetModalitiesByTerm/{termId}")]
        public async Task<IActionResult> GetModalitiesByTerm(Guid termId, CancellationToken ct)
        {
            var modalities = await _catalog.GetModalitiesAsync(termId, ct: ct);
            return Json(modalities.Select(m => new { id = m.Id, name = m.Name }));
        }

        [HttpGet("GetAreasByTerm/{termId}")]
        public async Task<IActionResult> GetAreasByTerm(Guid termId, CancellationToken ct)
        {
            var areas = await _catalog.GetTematicAreasByTermAsync(termId, ct);
            return Json(areas.Select(a => new { id = a.Id, name = a.Name }));
        }

        // ============ Edición ============
        // La edición se movió al expediente del postulante; cualquier llamada
        // a la ruta antigua se redirige al editor del expediente para no
        // duplicar la lógica del formulario.
        [HttpGet("editar/{id}")]
        public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
        {
            var data = await _postulants.GetForEditAsync(id, ct);
            if (data == null) return NotFound();
            var postulantId = data.Inscription.PostulantId;
            return RedirectToAction(
                "EditInscription",
                "Report",
                new { postulantId, inscriptionId = id });
        }

        // ============ Lookups de ubigeo (delegan a IUbigeoService) ============
        [HttpGet("ubigeo/departments/{countryId:guid}")]
        public async Task<IActionResult> GetDepartments(Guid countryId, CancellationToken ct)
        {
            var data = await _ubigeo.GetDepartmentsAsync(countryId, ct);
            return Json(data.Select(d => new { id = d.Id, name = d.Name }));
        }

        [HttpGet("ubigeo/provinces/{departmentId:guid}")]
        public async Task<IActionResult> GetProvincesByDepartment(Guid departmentId, CancellationToken ct)
        {
            var data = await _ubigeo.GetProvincesAsync(departmentId, ct);
            return Json(data.Select(p => new { id = p.Id, name = p.Name }));
        }

        [HttpGet("ubigeo/districts/{provinceId:guid}")]
        public async Task<IActionResult> GetDistrictsByProvince(Guid provinceId, CancellationToken ct)
        {
            var data = await _ubigeo.GetDistrictsAsync(provinceId, ct);
            return Json(data.Select(d => new { id = d.Id, name = d.Name }));
        }
    }
}
