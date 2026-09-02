using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Integrations;
using ADMISION.ENTITIES.Models.Postulant;
using ADMISION.ENTITIES.Models.Postulante;
using ADMISION.Extensions;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace admision.Controllers.Admin.InfoPostulant.ReportController
{
    [Route("admin/info-postulant/postulant")]
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin + "," + AppConstants.Roles.Consultor)]
    public class ReportController : Controller
    {
        private static readonly List<string> _inscriptionStates = new()
        {
            AppConstants.InscripcionState.Pendiente,
            AppConstants.InscripcionState.Aprobado,
            AppConstants.InscripcionState.Observado,
            AppConstants.InscripcionState.Rechazado,
            AppConstants.InscripcionState.Retirado
        };

        private readonly IPostulantResumeService _resume;
        private readonly ICatalogService _catalog;
        private readonly IUbigeoService _ubigeo;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ReportController> _logger;
        private readonly IExternalApiService _apis;
        private readonly IFileService _files;
        private readonly IAttendanceService _attendance;
        private readonly IDisabilityTypeService _disabilityTypes;

        public ReportController(
            IPostulantResumeService resume,
            ICatalogService catalog,
            IUbigeoService ubigeo,
            IWebHostEnvironment env,
            ILogger<ReportController> logger,
            IExternalApiService apis,
            IFileService files,
            IAttendanceService attendance,
            IDisabilityTypeService disabilityTypes)
        {
            _resume = resume;
            _catalog = catalog;
            _ubigeo = ubigeo;
            _env = env;
            _logger = logger;
            _apis = apis;
            _files = files;
            _attendance = attendance;
            _disabilityTypes = disabilityTypes;
        }
        [HttpGet("")]
        public IActionResult Index()
        {
            return Redirect("/admin/info-postulant");
        }

        // ============ Search / Detail ============
        [HttpGet("postulant-resum")]
        public IActionResult PostulantResumIndex()
        {
            return Redirect("/admin/info-postulant/list");
        }

        [HttpGet("postulant-resum/{postulantId:guid}")]
        public async Task<IActionResult> PostulantResumDetail(Guid postulantId, CancellationToken ct)
        {
            var postulant = await _resume.GetByIdAsync(postulantId, ct);
            if (postulant == null) return NotFound();

            return View("~/Pages/Admin/InfoPostulant/PostulantResum/Detail.cshtml", postulant);
        }

        [HttpPost("postulant-resum/{postulantId:guid}/edit-personal-data")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPersonalData(Guid postulantId, ADMISION.ENTITIES.Models.Users.Users model, [FromForm] List<Guid>? DisabilityTypeIds, [FromForm] string? ConadisNumber, CancellationToken ct)
        {
            var postulant = await _resume.GetByIdAsync(postulantId, ct);
            if (postulant == null)
            {
                if (Request.IsAjaxRequest()) return NotFound(new { success = false, message = "Postulante no encontrado." });
                return NotFound();
            }

            var ok = await _resume.UpdatePersonalDataAsync(postulantId, model, DisabilityTypeIds, ConadisNumber, User.Identity?.Name ?? "Admin", ct);
            if (!ok)
            {
                if (Request.IsAjaxRequest()) return BadRequest(new { success = false, message = "No se pudieron actualizar los datos." });
                return BadRequest();
            }

            if (Request.IsAjaxRequest()) return Ok(new { success = true, message = "Datos personales actualizados correctamente." });

            TempData["Success"] = "Datos personales actualizados correctamente.";
            return RedirectToAction(nameof(PostulantResumDetail), new { postulantId });
        }

        // ============ Secciones externas ============
        [HttpGet("postulant-resum/{postulantId:guid}/external-payments")]
        public async Task<IActionResult> PostulantResumExternalPayments(Guid postulantId, CancellationToken ct)
        {
            var postulant = await _resume.GetByIdAsync(postulantId, ct);
            if (postulant == null) return NotFound();

            var dni = postulant.User?.Document;
            if (!string.IsNullOrWhiteSpace(dni))
            {
                // 1. Consultar API externa y actualizar DB
                var api = await _apis.FindApiByCategoryAsync("Payment", ct);
                if (api != null)
                {
                    var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _apis.FetchAndSavePaymentsAsync(api.Id, dni, User, ip, ct);
                }

                // 2. Leer todos los registros de la DB (incluye los recién insertados)
                var paymentData = await _apis.GetPaymentVouchersByDniAsync(dni, ct);
                ViewBag.Postulant = postulant;
                return View("~/Pages/Admin/InfoPostulant/PostulantResum/ExternalPayments.cshtml", paymentData ?? new List<ExternalPaymentVoucher>());
            }

            ViewBag.Postulant = postulant;
            return View("~/Pages/Admin/InfoPostulant/PostulantResum/ExternalPayments.cshtml", new List<ExternalPaymentVoucher>());
        }

        // ============ Consultas externas (refresh JSON) ============
        [HttpGet("postulant-resum/{postulantId:guid}/external-academic/refresh")]
        public async Task<IActionResult> RefreshExternalAcademic(Guid postulantId, CancellationToken ct)
        {
            var postulant = await _resume.GetByIdAsync(postulantId, ct);
            if (postulant == null) return NotFound(new { success = false, message = "Postulante no encontrado." });

            var dni = postulant.User?.Document;
            if (string.IsNullOrWhiteSpace(dni))
                return BadRequest(new { success = false, message = "Postulante sin documento." });

            var api = await _apis.FindApiByCategoryAsync("Academic", ct);
            if (api == null)
                return Ok(new { success = false, message = "sin_api", data = Array.Empty<object>() });

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var fetchResult = await _apis.FetchAndSaveAcademicAsync(api.Id, dni, User, ip, ct);

            if (!fetchResult.Success)
            {
                var data = await _apis.GetAcademicInfoByDniAsync(dni, ct);
                return Ok(new { success = false, message = fetchResult.Error, data });
            }

            return Ok(new { success = true, data = fetchResult.Records });
        }

        [HttpGet("postulant-resum/{postulantId:guid}/external-payments/refresh")]
        public async Task<IActionResult> RefreshExternalPayments(Guid postulantId, CancellationToken ct)
        {
            var postulant = await _resume.GetByIdAsync(postulantId, ct);
            if (postulant == null) return NotFound(new { success = false, message = "Postulante no encontrado." });

            var dni = postulant.User?.Document;
            if (string.IsNullOrWhiteSpace(dni))
                return BadRequest(new { success = false, message = "Postulante sin documento." });

            var api = await _apis.FindApiByCategoryAsync("Payment", ct);
            if (api == null)
                return Ok(new { success = false, message = "sin_api", data = Array.Empty<object>() });

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var fetchResult = await _apis.FetchAndSavePaymentsAsync(api.Id, dni, User, ip, ct);

            // Leer todos los registros de la DB (incluye los recién insertados y los anteriores)
            var allRecords = await _apis.GetPaymentVouchersByDniAsync(dni, ct);

            if (!fetchResult.Success)
                return Ok(new { success = false, message = fetchResult.Error, data = allRecords });

            return Ok(new { success = true, data = allRecords });
        }

        // ============ Secciones del resumen ============
        [HttpGet("postulant-resum/{postulantId:guid}/inscriptions")]
        public async Task<IActionResult> PostulantResumInscriptions(Guid postulantId, CancellationToken ct)
        {
            var postulant = await _resume.GetByIdAsync(postulantId, ct);
            if (postulant == null) return NotFound();

            var inscriptions = await _resume.GetInscriptionsAsync(postulantId, ct);
            ViewBag.Postulant = postulant;
            return View("~/Pages/Admin/InfoPostulant/PostulantResum/Inscriptions.cshtml", inscriptions);
        }

        [HttpGet("postulant-resum/{postulantId:guid}/payments")]
        public async Task<IActionResult> PostulantResumPayments(Guid postulantId, CancellationToken ct)
        {
            var postulant = await _resume.GetByIdAsync(postulantId, ct);
            if (postulant == null) return NotFound();

            var inscriptions = await _resume.GetPaymentsAsync(postulantId, ct);

            // Cargar pagos externos (API)
            var dni = postulant.User?.Document;
            IReadOnlyList<ADMISION.ENTITIES.Models.Integrations.ExternalPaymentVoucher> externalVouchers
                = new List<ADMISION.ENTITIES.Models.Integrations.ExternalPaymentVoucher>();
            if (!string.IsNullOrWhiteSpace(dni))
            {
                var api = await _apis.FindApiByCategoryAsync("Payment", ct);
                if (api != null)
                {
                    var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                    await _apis.FetchAndSavePaymentsAsync(api.Id, dni, User, ip, ct);
                }
                externalVouchers = await _apis.GetPaymentVouchersByDniAsync(dni, ct)
                    ?? new List<ADMISION.ENTITIES.Models.Integrations.ExternalPaymentVoucher>();
            }

            ViewBag.Postulant = postulant;
            ViewBag.ExternalVouchers = externalVouchers;
            return View("~/Pages/Admin/InfoPostulant/PostulantResum/Payments.cshtml", inscriptions);
        }

        [HttpGet("postulant-resum/{postulantId:guid}/observations")]
        public async Task<IActionResult> PostulantResumObservations(Guid postulantId, CancellationToken ct)
        {
            var postulant = await _resume.GetByIdAsync(postulantId, ct);
            if (postulant == null) return NotFound();

            var data = await _resume.GetObservationsAsync(postulantId, ct);
            ViewBag.Postulant = postulant;
            ViewBag.UserObservations = data.UserObservations;
            return View("~/Pages/Admin/InfoPostulant/PostulantResum/Observations.cshtml", data.Inscriptions);
        }

        [HttpPost("postulant-resum/{postulantId:guid}/observations/add")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddObservation(Guid postulantId, string scope, Guid? inscriptionId, string observation, string? tipoObservacion, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(observation))
            {
                TempData["Error"] = "La observación no puede estar vacía.";
                return RedirectToAction(nameof(PostulantResumObservations), new { postulantId });
            }

            try
            {
                var ok = await _resume.AddObservationAsync(postulantId, scope, inscriptionId, observation, User.Identity?.Name ?? "Admin", tipoObservacion, ct);
                if (!ok)
                {
                    TempData["Error"] = "No se encontró el postulante. La observación no pudo registrarse.";
                    return RedirectToAction(nameof(PostulantResumObservations), new { postulantId });
                }

                TempData["Success"] = "Observación registrada correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar observación para postulante {PostulantId}", postulantId);
                TempData["Error"] = "Ocurrió un error al registrar la observación. Intente nuevamente.";
            }

            return RedirectToAction(nameof(PostulantResumObservations), new { postulantId });
        }

        [HttpPost("postulant-resum/{postulantId:guid}/observations/{observationId:guid}/edit")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
        public async Task<IActionResult> EditObservation(Guid postulantId, Guid observationId, string observation, string? tipoObservacion, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(observation))
            {
                TempData["Error"] = "La observación no puede estar vacía.";
                return RedirectToAction(nameof(PostulantResumObservations), new { postulantId });
            }

            try
            {
                var ok = await _resume.UpdateInscriptionObservationAsync(observationId, postulantId, observation, tipoObservacion, User.Identity?.Name ?? "SuperAdmin", ct);
                if (!ok)
                {
                    TempData["Error"] = "No se encontró la observación de la inscripción. No pudo actualizarse.";
                    return RedirectToAction(nameof(PostulantResumObservations), new { postulantId });
                }

                TempData["Success"] = "Observación actualizada correctamente.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar observación {ObservationId} del postulante {PostulantId}", observationId, postulantId);
                TempData["Error"] = "Ocurrió un error al editar la observación. Intente nuevamente.";
            }

            return RedirectToAction(nameof(PostulantResumObservations), new { postulantId });
        }

        [HttpGet("postulant-resum/{postulantId:guid}/observations/search")]
        public async Task<IActionResult> SearchObservations(Guid postulantId, string? searchTerm, CancellationToken ct)
        {
            var results = await _resume.SearchObservationsAsync(postulantId, searchTerm, ct);
            return Json(results);
        }

        [HttpGet("postulant-resum/{postulantId:guid}/resignations")]
        public async Task<IActionResult> PostulantResumResignations(Guid postulantId, CancellationToken ct)
        {
            var postulant = await _resume.GetByIdAsync(postulantId, ct);
            if (postulant == null) return NotFound();

            var inscriptions = await _resume.GetResignationsAsync(postulantId, ct);
            var allInscriptions = await _resume.GetInscriptionsAsync(postulantId, ct);
            ViewBag.Postulant = postulant;
            ViewBag.AllInscriptions = allInscriptions;
            return View("~/Pages/Admin/InfoPostulant/PostulantResum/Resignations.cshtml", inscriptions);
        }

        [HttpPost("postulant-resum/{postulantId:guid}/resignations")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateResignation(Guid postulantId, [FromForm] SaveResignationForm body, CancellationToken ct)
        {
            var postulant = await _resume.GetByIdAsync(postulantId, ct);
            if (postulant == null) return NotFound();

            var result = await _resume.SaveResignationAsync(body.InscriptionId, body.DateResignation, body.Description, body.File, User.Identity?.Name ?? "Admin", ct);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(PostulantResumResignations), new { postulantId });
        }

        [HttpPost("postulant-resum/{postulantId:guid}/annulments")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnulment(Guid postulantId, [FromForm] SaveAnnulmentForm body, Guid? returnInscriptionId, CancellationToken ct)
        {
            var postulant = await _resume.GetByIdAsync(postulantId, ct);
            if (postulant == null) return NotFound();

            var result = await _resume.SaveAnnulmentAsync(postulantId, body.StartDate, body.EndDate, body.Description, body.File, User.Identity?.Name ?? "Admin", ct);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
            }
            else
            {
                TempData["Success"] = result.Message;
            }

            if (returnInscriptionId.HasValue)
                return RedirectToAction(nameof(InscriptionValidation), new { postulantId, inscriptionId = returnInscriptionId });

            return RedirectToAction(nameof(PostulantResumInscriptions), new { postulantId });
        }

        [HttpPost("postulant-resum/{postulantId:guid}/annulments/{annulmentId:guid}/delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
        public async Task<IActionResult> DeleteAnnulment(Guid postulantId, Guid annulmentId, Guid? returnInscriptionId, CancellationToken ct)
        {
            var result = await _resume.DeleteAnnulmentAsync(postulantId, annulmentId, ct);
            if (!result.Success)
            {
                TempData["Error"] = result.Message;
            }
            else
            {
                TempData["Success"] = result.Message;
            }

            if (returnInscriptionId.HasValue)
                return RedirectToAction(nameof(InscriptionValidation), new { postulantId, inscriptionId = returnInscriptionId });

            return RedirectToAction(nameof(PostulantResumInscriptions), new { postulantId });
        }

        [HttpGet("postulant-resum/{postulantId:guid}/results")]
        public async Task<IActionResult> PostulantResumResults(Guid postulantId, CancellationToken ct)
        {
            var postulant = await _resume.GetByIdAsync(postulantId, ct);
            if (postulant == null) return NotFound();

            var inscriptions = await _resume.GetResultsAsync(postulantId, ct);
            var tematicAreaCodes = await _resume.GetTematicAreaCodesAsync(postulantId, ct);

            ViewBag.Postulant = postulant;
            ViewBag.TematicAreaLookup = tematicAreaCodes;
            return View("~/Pages/Admin/InfoPostulant/PostulantResum/Results.cshtml", inscriptions);
        }

        // ============ Edición manual de nota (SuperAdmin) ============
        [HttpPost("postulant-resum/{postulantId:guid}/inscription/{inscriptionId:guid}/grade")]
        [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
        public async Task<IActionResult> SetInscriptionGrade(
            Guid postulantId, Guid inscriptionId, decimal? gradeAdmission, bool isAdmission, CancellationToken ct)
        {
            var outcome = await _resume.SetInscriptionGradeAsync(
                postulantId, inscriptionId, gradeAdmission, isAdmission,
                User.Identity?.Name ?? "SuperAdmin", ct);

            switch (outcome)
            {
                case GradeUpdateOutcome.Updated:
                    TempData["Success"] = isAdmission && gradeAdmission.HasValue
                        ? $"Nota registrada ({gradeAdmission.Value:F3}) — postulante marcado como ADMITIDO."
                        : "Nota actualizada.";
                    break;
                case GradeUpdateOutcome.InvalidGrade:
                    TempData["Error"] = "La nota no puede ser negativa.";
                    break;
                case GradeUpdateOutcome.NotFound:
                    TempData["Error"] = "Inscripción no encontrada para este postulante.";
                    break;
            }
            return RedirectToAction(nameof(PostulantResumResults), new { postulantId });
        }

        [HttpPost("postulant-resum/{postulantId:guid}/inscription/{inscriptionId:guid}/clear-grade")]
        [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
        public async Task<IActionResult> ClearInscriptionGrade(Guid postulantId, Guid inscriptionId, CancellationToken ct)
        {
            var ok = await _resume.ClearInscriptionGradeAsync(postulantId, inscriptionId, User.Identity?.Name ?? "SuperAdmin", ct);
            TempData[ok ? "Success" : "Error"] = ok
                ? "Nota eliminada — postulante marcado como NO admitido."
                : "Inscripción no encontrada para este postulante.";
            return RedirectToAction(nameof(PostulantResumResults), new { postulantId });
        }

        [HttpGet("postulant-resum/{postulantId:guid}/attendance-history")]
        public async Task<IActionResult> PostulantResumAttendanceHistory(Guid postulantId, CancellationToken ct)
        {
            var postulant = await _resume.GetByIdAsync(postulantId, ct);
            if (postulant == null) return NotFound();

            var attendances = await _attendance.GetAttendanceHistoryByPostulantAsync(postulantId, ct);
            ViewBag.Postulant = postulant;
            return View("~/Pages/Admin/InfoPostulant/PostulantResum/AttendanceHistory.cshtml", attendances);
        }

        [HttpGet("postulant-resum/{postulantId:guid}/parents")]
        public async Task<IActionResult> PostulantResumParents(Guid postulantId, CancellationToken ct)
        {
            var postulant = await _resume.GetByIdAsync(postulantId, ct);
            if (postulant == null) return NotFound();

            var parents = await _resume.GetParentsAsync(postulantId, ct);
            ViewBag.Postulant = postulant;
            return View("~/Pages/Admin/InfoPostulant/PostulantResum/Parents.cshtml", parents);
        }

        //[HttpGet("postulant-resum/{postulantId:guid}/documents")]
        //public async Task<IActionResult> PostulantResumDocuments(Guid postulantId, CancellationToken ct)
        //{
        //    var postulant = await _resume.GetByIdAsync(postulantId, ct);
        //    if (postulant == null) return NotFound();

        //    var documents = await _resume.GetIssuedDocumentsAsync(postulantId, ct);
        //    ViewBag.Postulant = postulant;
        //    return View("~/Pages/Admin/InfoPostulant/PostulantResum/Documents.cshtml", documents);
        //}

        // ============ ModalityResum ============
        [HttpGet("modality-resum")]
        public IActionResult ModalityResumIndex() => View("~/Pages/Admin/InfoPostulant/ModalityResum/Index.cshtml");

        // ============ Biométricos: foto ============
        [HttpPost("postulant-resum/{postulantId:guid}/capture-photo")]
        public async Task<IActionResult> CapturePhoto(Guid postulantId, [FromBody] PhotoCaptureRequest request, CancellationToken ct)
        {
            var photosRoot = Path.Combine(_files.GetBaseStoragePath(), DateTime.UtcNow.Year.ToString(), "photos");
            var result = await _resume.SavePhotoAsync(postulantId, request.Image, User.Identity?.Name ?? "System", photosRoot, ct);

            if (result.PostulantNotFound) return NotFound(new { message = result.ErrorMessage });
            if (!result.Success) return BadRequest(new { message = result.ErrorMessage });

            return Json(new { success = true, photoUrl = result.PhotoUrl });
        }

        [HttpGet("postulant-resum/{postulantId:guid}/photos")]
        public async Task<IActionResult> GetPostulantPhotos(Guid postulantId, CancellationToken ct)
        {
            var photos = await _resume.GetPhotosAsync(postulantId, ct);
            return Json(photos);
        }

        [HttpPost("postulant-resum/{postulantId:guid}/set-primary-photo/{photoId:guid}")]
        public async Task<IActionResult> SetPrimaryPhoto(Guid postulantId, Guid photoId, CancellationToken ct)
        {
            var ok = await _resume.SetPrimaryPhotoAsync(postulantId, photoId, ct);
            return ok ? Json(new { success = true }) : NotFound(new { message = "Foto no encontrada o postulante inválido." });
        }

        [HttpDelete("postulant-resum/{postulantId:guid}/photo/{photoId:guid}")]
        public async Task<IActionResult> DeletePostulantPhoto(Guid postulantId, Guid photoId, CancellationToken ct)
        {
            var result = await _resume.DeletePhotoAsync(postulantId, photoId, _files.GetBaseStoragePath(), ct);
            if (result.NotFound) return NotFound(new { message = "Foto no encontrada." });
            return Json(new
            {
                success = result.Success,
                deletedPrimary = result.DeletedPrimary,
                newPrimaryPhotoUrl = result.NewPrimaryPhotoUrl
            });
        }

        // ============ Biométricos: huellas ============
        [HttpGet("postulant-resum/{postulantId:guid}/fingerprints")]
        public async Task<IActionResult> GetPostulantFingerprints(Guid postulantId, CancellationToken ct)
        {
            var list = await _resume.GetFingerprintsAsync(postulantId, ct);
            return Json(list);
        }

        [HttpPost("postulant-resum/{postulantId:guid}/capture-fingerprint")]
        public async Task<IActionResult> CaptureFingerprint(Guid postulantId, [FromBody] FingerprintCaptureRequest request, CancellationToken ct)
        {
            var deviceIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString();
            var outcome = await _resume.SaveFingerprintAsync(User.Identity?.Name ?? "SuperAdmin", postulantId, request.Template, request.ImageBase64, deviceIp, ct);

            if (outcome.PostulantNotFound) return NotFound(new { message = outcome.ErrorMessage });
            if (!outcome.Success) return BadRequest(new { message = outcome.ErrorMessage });

            return Json(new { success = true });
        }

        [HttpDelete("postulant-resum/{postulantId:guid}/fingerprint/{fingerId:guid}")]
        public async Task<IActionResult> DeleteFingerprint(Guid postulantId, Guid fingerId, CancellationToken ct)
        {
            var ok = await _resume.DeleteFingerprintAsync(postulantId, fingerId, ct);
            return ok ? Json(new { success = true }) : NotFound();
        }

        // ============ Validación de archivos del expediente ============

        [HttpGet("postulant-resum/{postulantId:guid}/inscriptions/{inscriptionId:guid}/validate")]
        public async Task<IActionResult> InscriptionValidation(Guid postulantId, Guid inscriptionId, CancellationToken ct)
        {
            var postulant = await _resume.GetByIdAsync(postulantId, ct);
            if (postulant == null) return NotFound();

            var dto = await _resume.GetValidationAsync(postulantId, ct);
            var group = dto?.Inscriptions.FirstOrDefault(g => g.InscriptionId == inscriptionId);
            if (group == null) return NotFound();

            var dni = postulant.User?.Document;
            IReadOnlyList<ExternalAcademicInfo>? academicData = null;
            if (!string.IsNullOrWhiteSpace(dni))
                academicData = await _apis.GetAcademicInfoByDniAsync(dni, ct);

            var user = await _resume.GetUserForEditAsync(postulantId, ct);
            var inscription = await _resume.GetInscriptionForEditAsync(postulantId, inscriptionId, ct);
            if (inscription != null)
                await PopulateEditDataAsync(inscription, ct);

            var photos = await _resume.GetPhotosAsync(postulantId, ct);
            var fingerprints = await _resume.GetFingerprintsAsync(postulantId, ct);

            var disabilityTypes = await _disabilityTypes.ListAsync(null, true, ct);
            var pendingRequirements = await _resume.GetPendingRequirementsAsync(inscriptionId, postulantId, ct);
            var annulments = await _resume.GetAnnulmentsAsync(postulantId, ct);

            ViewBag.Postulant = postulant;
            ViewBag.ExternalAcademic = academicData ?? new List<ExternalAcademicInfo>();
            ViewBag.User = user;
            ViewBag.Inscription = inscription;
            ViewBag.Photos = photos;
            ViewBag.Fingerprints = fingerprints;
            ViewBag.DisabilityTypes = disabilityTypes;
            ViewBag.PendingRequirements = pendingRequirements;
            ViewBag.Annulments = annulments;

            return View("~/Pages/Admin/InfoPostulant/PostulantResum/InscriptionValidation.cshtml", group);
        }

        [HttpPost("postulant-resum/{postulantId:guid}/file/{fileId:guid}/validate")]
        public async Task<IActionResult> ToggleFileValidation(Guid postulantId, Guid fileId, [FromBody] FileValidationRequest body, CancellationToken ct)
        {
            var result = await _resume.SetFileValidatedAsync(fileId, body?.IsValidated ?? false, body?.Note, User.Identity?.Name ?? "Admin", ct);
            if (!result.Found) return NotFound(new { message = "Archivo no encontrado." });
            return Json(ValidationJson(result));
        }

        [HttpPost("postulant-resum/{postulantId:guid}/payment/{paymentId:guid}/validate")]
        public async Task<IActionResult> TogglePaymentValidation(Guid postulantId, Guid paymentId, [FromBody] FileValidationRequest body, CancellationToken ct)
        {
            var result = await _resume.SetPaymentApprovedAsync(paymentId, body?.IsValidated ?? false, body?.Note, User.Identity?.Name ?? "Admin", ct);
            if (!result.Found) return NotFound(new { message = "Pago no encontrado." });
            return Json(ValidationJson(result));
        }

        [HttpPost("postulant-resum/{postulantId:guid}/file/{fileId:guid}/replace")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReplaceFile(Guid postulantId, Guid fileId, IFormFile newFile, CancellationToken ct)
        {
            if (newFile == null || newFile.Length == 0)
                return BadRequest(new { message = "Debe seleccionar un archivo." });

            var result = await _resume.ReplaceFileSubmissionAsync(fileId, newFile, postulantId, User.Identity?.Name ?? "Admin", ct);

            if (result.NotFound) return NotFound(new { message = "Archivo no encontrado." });
            if (!result.Success) return BadRequest(new { message = result.ErrorMessage });

            return Json(new
            {
                success = true,
                newFileName = result.NewFileName,
                newFileSize = result.NewFileSize,
                newFilePath = result.NewFilePath
            });
        }

        [HttpPost("postulant-resum/{postulantId:guid}/inscriptions/{inscriptionId:guid}/file/upload")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
        public async Task<IActionResult> UploadRequirementFile(Guid postulantId, Guid inscriptionId, Guid requirementId, IFormFile newFile, CancellationToken ct)
        {
            if (newFile == null || newFile.Length == 0)
                return BadRequest(new { message = "Debe seleccionar un archivo." });

            var result = await _resume.UploadRequirementFileAsync(
                inscriptionId, postulantId, requirementId, newFile,
                User.Identity?.Name ?? "SuperAdmin", ct);

            if (result.NotFound)
                return NotFound(new { message = "Inscripción no encontrada." });
            if (result.AlreadyExists)
                return BadRequest(new { message = "El requisito ya tiene un archivo registrado. Use reemplazar para editarlo." });
            if (result.NotRequired)
                return BadRequest(new { message = "El requisito no aplica para la modalidad / tipo de modalidad de esta inscripción." });
            if (!result.Success)
                return BadRequest(new { message = result.ErrorMessage });

            return Json(new
            {
                success = true,
                message = "Archivo subido correctamente.",
                newFileName = result.NewFileName,
                newFileSize = result.NewFileSize,
                newFilePath = result.NewFilePath
            });
        }

        [HttpPost("postulant-resum/{postulantId:guid}/payment/{paymentId:guid}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPayment(Guid postulantId, Guid paymentId, [FromForm] EditPaymentForm body, CancellationToken ct)
        {
            var result = await _resume.EditPaymentAsync(
                paymentId, postulantId,
                body.OperationCode,
                body.NewFile,
                body.ExternalPaymentVoucherId,
                body.Disassociate,
                User.Identity?.Name ?? "Admin", ct);

            if (result.NotFound) return NotFound(new { success = false, message = "Comprobante no encontrado." });
            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
                return BadRequest(new { success = false, message = result.ErrorMessage });

            return Ok(new
            {
                success = true,
                message = "Comprobante actualizado correctamente.",
                operationCode = result.OperationCode,
                hasExternalAssociation = result.HasExternalAssociation,
                newFileName = result.NewFileName,
                newFilePath = result.NewFilePath,
                newFileSize = result.NewFileSize
            });
        }

        [HttpGet("postulant-resum/{postulantId:guid}/external-payments/unassociated")]
        public async Task<IActionResult> GetUnassociatedExternalPayments(Guid postulantId, CancellationToken ct)
        {
            var vouchers = await _resume.GetUnassociatedExternalPaymentsAsync(postulantId, ct);
            return Json(vouchers.Select(v => new
            {
                id = v.Id,
                serialVoucher = v.SerialVoucher,
                fullName = v.FullName,
                userName = v.UserName,
                queriedAt = v.QueriedAt,
                payments = v.Payments?.Select(p => new
                {
                    description = p.Description,
                    total = p.Total,
                    subTotal = p.SubTotal,
                    discount = p.Discount,
                    quantity = p.Quantity,
                    termName = p.TermName
                })
            }));
        }

        private static object ValidationJson(ValidationToggleResult r) => new
        {
            success = true,
            inscriptionId = r.InscriptionId,
            previousState = r.PreviousState,
            newState = r.NewState,
            stateChanged = r.StateChanged,
            validatedCount = r.ValidatedCount,
            totalCount = r.TotalCount,
            allValidated = r.AllValidated
        };

        // ============ Edición de inscripción desde el expediente ============
        [HttpGet("postulant-resum/{postulantId:guid}/inscriptions/{inscriptionId:guid}/edit")]
        public async Task<IActionResult> EditInscription(Guid postulantId, Guid inscriptionId, CancellationToken ct)
        {
            return RedirectToAction(nameof(InscriptionValidation), new { postulantId, inscriptionId });
        }

        [HttpPost("postulant-resum/{postulantId:guid}/inscriptions/{inscriptionId:guid}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditInscription(Guid postulantId, Guid inscriptionId, Inscription model, CancellationToken ct)
        {
            if (inscriptionId != model.Id)
            {
                if (Request.IsAjaxRequest())
                    return BadRequest(new { success = false, message = "ID de inscripcion no coincide." });
                return BadRequest();
            }

            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(model.State))
                ModelState.AddModelError(nameof(model.State), "El estado es obligatorio.");
            if (model.CareerId == Guid.Empty)
                ModelState.AddModelError(nameof(model.CareerId), "La carrera profesional es obligatoria.");
            if (!model.ModalityId.HasValue || model.ModalityId.Value == Guid.Empty)
                ModelState.AddModelError(nameof(model.ModalityId), "La modalidad de ingreso es obligatoria.");
            if (model.CountryId == Guid.Empty)
                ModelState.AddModelError(nameof(model.CountryId), "El pais es obligatorio.");

            if (!ModelState.IsValid)
            {
                if (Request.IsAjaxRequest())
                {
                    var errors = ModelState.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );
                    return BadRequest(new { success = false, message = "Hay campos con errores.", errors });
                }
                return BadRequest();
            }

            var ok = await _resume.UpdateInscriptionAsync(postulantId, model, User.Identity?.Name ?? "Admin", ct);
            if (!ok)
            {
                if (Request.IsAjaxRequest())
                    return NotFound(new { success = false, message = "Inscripcion no encontrada." });
                return NotFound();
            }

            if (Request.IsAjaxRequest())
                return Ok(new { success = true, message = "Datos de la inscripcion actualizados correctamente.", redirectUrl = Url.Action(nameof(InscriptionValidation), new { postulantId, inscriptionId }) });

            TempData["Success"] = "Datos de la inscripcion actualizados correctamente.";
            return RedirectToAction(nameof(InscriptionValidation), new { postulantId, inscriptionId });
        }

        // ============ Propagacion de ubigeo a todas las inscripciones ============
        [HttpPost("postulant-resum/{postulantId:guid}/propagate-ubigeo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PropagateUbigeo(Guid postulantId, Guid currentInscriptionId, Guid? countryId, Guid? distritId, CancellationToken ct)
        {
            var postulant = await _resume.GetByIdAsync(postulantId, ct);
            if (postulant == null)
            {
                if (Request.IsAjaxRequest()) return NotFound(new { success = false, message = "Postulante no encontrado." });
                return NotFound();
            }

            var updated = await _resume.PropagateUbigeoAsync(postulantId, currentInscriptionId, countryId, distritId, User.Identity?.Name ?? "Admin", ct);

            if (Request.IsAjaxRequest())
                return Ok(new { success = true, message = $"Ubigeo propagado a {updated} inscripcion(es) adicionales." });

            TempData["Success"] = $"Ubigeo propagado a {updated} inscripcion(es) adicionales.";
            return RedirectToAction(nameof(InscriptionValidation), new { postulantId, inscriptionId = currentInscriptionId });
        }

        private async Task PopulateEditDataAsync(Inscription inscription, CancellationToken ct)
        {
            ViewBag.States = _inscriptionStates;

            // Filtrar carreras según la modalidad seleccionada
            var careerIdsForModality = inscription.ModalityId.HasValue
                ? await _resume.GetModalityCareerIdsAsync(inscription.ModalityId.Value, ct)
                : new List<Guid>();
            var allCareers = await _catalog.GetCareersAsync(ct: ct);
            ViewBag.Careers = careerIdsForModality.Count > 0
                ? allCareers.Where(c => careerIdsForModality.Contains(c.Id))
                    .Select(c => new { id = c.Id, name = c.Name }).ToList<object>()
                : allCareers.Select(c => new { id = c.Id, name = c.Name }).ToList<object>();
            ViewBag.Modalities = (await _catalog.GetModalitiesAsync(onlyActive: false, ct: ct))
                .Select(m => new { id = m.Id, name = m.Name }).ToList<object>();
            ViewBag.TypeModalities = inscription.ModalityId.HasValue
                ? (await _catalog.GetTypeModalitiesAsync(inscription.ModalityId.Value, onlyActive: false, ct))
                    .Select(t => new { id = t.Id, name = t.Name }).ToList<object>()
                : new List<object>();
            ViewBag.TypePostulants = (await _catalog.GetTypePostulantsAsync(ct))
                .Select(t => new { id = t.Id, name = t.Name }).ToList<object>();

            ViewBag.Countries = (await _ubigeo.GetCountriesAsync(ct))
                .Select(c => new { id = c.Id, name = c.Name }).ToList<object>();
            var currentDepartmentId = inscription.Distrit?.Province?.DepartmentId;
            var currentProvinceId = inscription.Distrit?.ProvinceId;

            ViewBag.Departments = currentDepartmentId.HasValue && inscription.CountryId != Guid.Empty
                ? (await _ubigeo.GetDepartmentsAsync(inscription.CountryId, ct))
                    .Select(d => new { id = d.Id, name = d.Name }).ToList<object>()
                : new List<object>();
            ViewBag.Provinces = currentDepartmentId.HasValue
                ? (await _ubigeo.GetProvincesAsync(currentDepartmentId.Value, ct))
                    .Select(p => new { id = p.Id, name = p.Name }).ToList<object>()
                : new List<object>();
            ViewBag.Districts = currentProvinceId.HasValue
                ? (await _ubigeo.GetDistrictsAsync(currentProvinceId.Value, ct))
                    .Select(d => new { id = d.Id, name = d.Name }).ToList<object>()
                : new List<object>();

            ViewBag.CurrentDepartmentId = currentDepartmentId;
            ViewBag.CurrentProvinceId = currentProvinceId;

            // Datos para la sección "Institución educativa" — pre-cargamos la
            // cadena ubigeo y la lista de colegios del distrito donde estaba
            // asignado el colegio actual, para que el formulario abra con la
            // selección visible.
            var schoolDistrictId = inscription.School?.DistritId;
            var schoolProvinceId = inscription.School?.Distrit?.ProvinceId;
            var schoolDepartmentId = inscription.School?.Distrit?.Province?.DepartmentId;

            ViewBag.SchoolDepartmentId = schoolDepartmentId;
            ViewBag.SchoolProvinceId = schoolProvinceId;
            ViewBag.SchoolDistrictId = schoolDistrictId;

            // Cargar departments del país de la inscripción (los colegios
            // normalmente viven en Perú, pero respetamos el ubigeo declarado).
            ViewBag.SchoolDepartments = inscription.CountryId != Guid.Empty
                ? (await _ubigeo.GetDepartmentsAsync(inscription.CountryId, ct))
                    .Select(d => new { id = d.Id, name = d.Name }).ToList<object>()
                : new List<object>();
            ViewBag.SchoolProvinces = schoolDepartmentId.HasValue
                ? (await _ubigeo.GetProvincesAsync(schoolDepartmentId.Value, ct))
                    .Select(p => new { id = p.Id, name = p.Name }).ToList<object>()
                : new List<object>();
            ViewBag.SchoolDistricts = schoolProvinceId.HasValue
                ? (await _ubigeo.GetDistrictsAsync(schoolProvinceId.Value, ct))
                    .Select(d => new { id = d.Id, name = d.Name }).ToList<object>()
                : new List<object>();
        }
    }

    public class FileValidationRequest
    {
        public bool IsValidated { get; set; }
        public string? Note { get; set; }
    }

    // Controller separado para lookups de la edición de inscripción dentro
    // del expediente. Vive bajo /admin/info-postulant/postulant/lookups/* para
    // evitar mezclarse con las rutas del expediente.
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin + "," + AppConstants.Roles.Consultor + "," + AppConstants.Roles.Soporte)]
    [Route("admin/info-postulant/postulant/lookups")]
    public class ReportLookupsController : Controller
    {
        private readonly IUbigeoService _ubigeo;
        private readonly IInscriptionLookupService _lookups;
        private readonly ICatalogService _catalog;
        private readonly AppDbContext _context;

        public ReportLookupsController(IUbigeoService ubigeo, IInscriptionLookupService lookups, ICatalogService catalog, AppDbContext context)
        {
            _ubigeo = ubigeo;
            _lookups = lookups;
            _catalog = catalog;
            _context = context;
        }

        [HttpGet("ubigeo/departments/{countryId:guid}")]
        public async Task<IActionResult> Departments(Guid countryId, CancellationToken ct)
            => Json((await _ubigeo.GetDepartmentsAsync(countryId, ct)).Select(d => new { id = d.Id, name = d.Name }));

        [HttpGet("ubigeo/provinces/{departmentId:guid}")]
        public async Task<IActionResult> Provinces(Guid departmentId, CancellationToken ct)
            => Json((await _ubigeo.GetProvincesAsync(departmentId, ct)).Select(p => new { id = p.Id, name = p.Name }));

        [HttpGet("ubigeo/districts/{provinceId:guid}")]
        public async Task<IActionResult> Districts(Guid provinceId, CancellationToken ct)
            => Json((await _ubigeo.GetDistrictsAsync(provinceId, ct)).Select(d => new { id = d.Id, name = d.Name }));

        [HttpGet("schools/{districtId:guid}")]
        public async Task<IActionResult> Schools(Guid districtId, CancellationToken ct)
            => Json((await _lookups.GetSchoolsByDistrictAsync(districtId, ct)).Select(s => new { id = s.Id, name = s.Name, management = s.Management, level = s.Level }));

        [HttpGet("type-modalities/{modalityId:guid}")]
        public async Task<IActionResult> TypeModalities(Guid modalityId, CancellationToken ct)
            => Json((await _catalog.GetTypeModalitiesAsync(modalityId, onlyActive: false, ct)).Select(t => new { id = t.Id, name = t.Name }));

        [HttpGet("careers-by-modality/{modalityId:guid}")]
        public async Task<IActionResult> CareersByModality(Guid modalityId, CancellationToken ct)
        {
            var careerIds = await _context.ModalityCareers
                .AsNoTracking()
                .Where(mc => mc.ModalityId == modalityId)
                .Select(mc => mc.CareerId)
                .ToListAsync(ct);

            var careers = await _context.Careers
                .AsNoTracking()
                .Where(c => careerIds.Contains(c.Id))
                .OrderBy(c => c.Name)
                .Select(c => new { id = c.Id, name = c.Name })
                .ToListAsync(ct);

            return Json(careers);
        }
    }

    public class PhotoCaptureRequest
    {
        public string Image { get; set; } = string.Empty;
    }

    public class FingerprintCaptureRequest
    {
        public string Template { get; set; } = string.Empty;
        public string? ImageBase64 { get; set; }
    }

    public class EditPaymentForm
    {
        public string? OperationCode { get; set; }
        public IFormFile? NewFile { get; set; }
        public Guid? ExternalPaymentVoucherId { get; set; }
        public bool Disassociate { get; set; }
    }

    public class SaveResignationForm
    {
        public Guid InscriptionId { get; set; }
        public DateTimeOffset DateResignation { get; set; }
        public string Description { get; set; } = string.Empty;
        public IFormFile? File { get; set; }
    }

    public class SaveAnnulmentForm
    {
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public IFormFile? File { get; set; }
    }
}
