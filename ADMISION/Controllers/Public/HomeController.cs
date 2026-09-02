using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ADMISION.Models.ViewModels.Public;
using ADMISION.Services.Interfaces;

namespace ADMISION.Controllers.Public
{
    [Route("public")]
    public class HomeController : Controller
    {
        // El controller solo orquesta HTTP/UI. Toda la consulta y lógica de negocio
        // viven en los servicios inyectados.
        private readonly IPublicPortalService _portal;
        private readonly IInscriptionLookupService _lookups;
        private readonly IInscriptionService _inscription;
        private readonly IUbigeoService _ubigeo;
        private readonly ICaptchaService _captcha;
        private readonly IConfigService _config;
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IInscriptionDocumentService _documents;
        private readonly IBrochureService _brochures;

        public HomeController(
            IPublicPortalService portal,
            IInscriptionLookupService lookups,
            IInscriptionService inscription,
            IUbigeoService ubigeo,
            ICaptchaService captcha,
            IConfigService config,
            ILogger<HomeController> logger,
            IWebHostEnvironment env,
            IInscriptionDocumentService documents,
            IBrochureService brochures)
        {
            _portal = portal;
            _lookups = lookups;
            _inscription = inscription;
            _ubigeo = ubigeo;
            _captcha = captcha;
            _config = config;
            _logger = logger;
            _env = env;
            _documents = documents;
            _brochures = brochures;
        }

        // ============================================================
        //  Páginas públicas (read-only) — IPublicPortalService
        // ============================================================

        [HttpGet("~/")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var vm = await _portal.GetHomeAsync(ct);
            return View("~/Pages/Public/Index.cshtml", vm);
        }

        [HttpGet("~/exam")]
        public IActionResult Exams() => View("~/Pages/Public/Exams.cshtml");

        [HttpGet("~/mission")]
        public IActionResult Mission() => View("~/Pages/Public/Mission.cshtml");

        [HttpGet("~/documentos/{category}")]
        public async Task<IActionResult> Documents(string category, CancellationToken ct)
        {
            var vm = await _portal.GetDocumentsPageAsync(category, ct);
            if (vm == null) return NotFound();
            return View("~/Pages/Public/Documents.cshtml", vm);
        }

        [HttpGet("~/resultados")]
        public async Task<IActionResult> Results(Guid? termId, CancellationToken ct)
        {
            var vm = await _portal.GetResultsAsync(termId, ct);
            return View("~/Pages/Public/Results.cshtml", vm);
        }

        [HttpGet("~/vacantes")]
        public async Task<IActionResult> Vacantes(CancellationToken ct)
        {
            var vm = await _portal.GetVacanciesAsync(null, ct);
            return View("~/Pages/Public/Vacancies.cshtml", vm);
        }

        [HttpGet("~/carreras")]
        public async Task<IActionResult> Careers(CancellationToken ct)
        {
            var vm = await _portal.GetCareersAsync(ct);
            return View("~/Pages/Public/Careers.cshtml", vm);
        }

        [HttpGet("~/carreras/{id:guid}")]
        public async Task<IActionResult> CareerDetail(Guid id, CancellationToken ct)
        {
            var detail = await _portal.GetCareerDetailAsync(id, ct);
            if (detail == null) return NotFound();

            ViewBag.LatestTerm = detail.LatestTerm;
            ViewBag.TotalVacancies = detail.TotalVacancies;
            return View("~/Pages/Public/CareerDetail.cshtml", detail.Career);
        }

        [HttpGet("~/cronograma")]
        public async Task<IActionResult> Cronograma(CancellationToken ct)
        {
            var vm = await _portal.GetScheduleAsync(null, ct);
            return View("~/Pages/Public/Cronogram.cshtml", vm);
        }

        [HttpGet("~/modalidad")]
        public async Task<IActionResult> Modality(CancellationToken ct)
        {
            var vm = await _portal.GetModalityAsync(null, ct);
            return View("~/Pages/Public/Modality.cshtml", vm);
        }

        // ============================================================
        //  Inscripción — GET (formulario) + lookups AJAX
        // ============================================================

        [HttpGet("~/inscription")]
        public async Task<IActionResult> Inscription(Guid? modalityId, CancellationToken ct)
        {
            var examEndDate = await _lookups.GetExamEndDateAsync(modalityId, ct);
            var registrationsClosed = examEndDate <= DateTime.Now;

            ViewBag.RegistrationsClosed = examEndDate <= DateTime.Now;

            if (ViewBag.RegistrationsClosed)
            {
                return View("~/Pages/Public/Inscription.cshtml");
            }

            ViewBag.ExamEndDate = examEndDate;
            var data = await _lookups.GetFormDataAsync(ct);
            ViewBag.Modalities = data.Modalities;
            ViewBag.TypePostulants = data.TypePostulants;
            ViewBag.Careers = data.Careers;
            ViewBag.MethodPayments = data.MethodPayments;
            ViewBag.Countries = data.Countries;
            ViewBag.Departments = data.Departments;
            ViewBag.DisabilityTypes = data.DisabilityTypes;
            ViewBag.Universities = data.Universities;
            ViewBag.CareersAll = data.CareersAll;

            ViewBag.ModalityCareerMap = await _lookups.GetModalityCareerMapAsync(ct);
            ViewBag.TypeModalityCareerMap = await _lookups.GetTypeModalityCareerMapAsync(ct);

            var modalityFlags = await _lookups.GetModalityFlagsAsync(ct);
            ViewBag.ModalityFlags = modalityFlags;

            ViewBag.CaptchaEnabled = _captcha.IsEnabled;
            ViewBag.CaptchaSiteKey = _captcha.SiteKey;
            ViewBag.CaptchaProvider = _captcha.Provider;

            var model = new EnrollmentViewModel();
            if (modalityId.HasValue)
            {
                model.ModalityId = modalityId.Value;
                ViewBag.FixedModality = true;
            }

            return View("~/Pages/Public/Inscription.cshtml", model);
        }

        [HttpGet("check-user")]
        [EnableRateLimiting("public-lookup")]
        public async Task<IActionResult> CheckUser(string docType, string docNumber, CancellationToken ct)
        {
            // Anti-scraping: requiere captcha cuando está habilitado.
            if (_captcha.IsEnabled)
            {
                var token = Request.Headers["X-Captcha-Token"].FirstOrDefault();
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var captchaResult = await _captcha.VerifyAsync(token, ip, ct);
                if (!captchaResult.Success)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, new
                    {
                        error = "captcha_required",
                        message = "Verificación anti-bot requerida."
                    });
                }
            }

            var user = await _lookups.CheckUserAsync(docType, docNumber, ct);
            return Json(user);
        }

        [HttpGet("departments/{countryId}")]
        public async Task<IActionResult> GetDepartments(Guid countryId, CancellationToken ct)
        {
            var departments = await _ubigeo.GetDepartmentsAsync(countryId, ct);
            return Json(departments.Select(d => new { d.Id, d.Name }));
        }

        [HttpGet("provinces/{departmentId}")]
        public async Task<IActionResult> GetProvinces(Guid departmentId, CancellationToken ct)
        {
            var provinces = await _ubigeo.GetProvincesAsync(departmentId, ct);
            return Json(provinces.Select(p => new { p.Id, p.Name }));
        }

        [HttpGet("districts/{provinceId}")]
        public async Task<IActionResult> GetDistricts(Guid provinceId, CancellationToken ct)
        {
            var districts = await _ubigeo.GetDistrictsAsync(provinceId, ct);
            return Json(districts.Select(d => new { d.Id, d.Name }));
        }

        [HttpGet("ubigeo-by-code/{code}")]
        public async Task<IActionResult> GetUbigeoByCode(string code, CancellationToken ct)
        {
            var hit = await _ubigeo.FindByCodeAsync(code, ct);
            if (hit == null) return Json(new { found = false });
            return Json(new
            {
                found = true,
                distritId = hit.DistritId,
                distritName = hit.DistritName,
                provinceId = hit.ProvinceId,
                provinceName = hit.ProvinceName,
                departmentId = hit.DepartmentId,
                departmentName = hit.DepartmentName
            });
        }

        [HttpGet("type-modalities/{modalityId}")]
        public async Task<IActionResult> GetTypesByModality(Guid modalityId, CancellationToken ct)
        {
            var types = await _lookups.GetTypeModalitiesAsync(modalityId, ct);
            return Json(types);
        }

        [HttpGet("modality-info/{modalityId}")]
        public async Task<IActionResult> GetModalityInfo(Guid modalityId, CancellationToken ct)
        {
            var info = await _lookups.GetModalityInfoAsync(modalityId, ct);
            if (info == null) return NotFound();
            return Json(info);
        }

        [HttpGet("universities")]
        public async Task<IActionResult> GetUniversities(CancellationToken ct)
        {
            var list = await _lookups.GetUniversitiesAsync(ct);
            return Json(list);
        }

        [HttpGet("careers-list")]
        public async Task<IActionResult> GetCareersList(CancellationToken ct)
        {
            var list = await _lookups.GetCareersListAsync(ct);
            return Json(list);
        }


        [HttpGet("~/consulta-inscripcion")]
        public IActionResult ConsultarInscripcion()
        {
            ViewBag.CaptchaEnabled = _captcha.IsEnabled;
            ViewBag.CaptchaSiteKey = _captcha.SiteKey;
            ViewBag.CaptchaProvider = _captcha.Provider;
            return View("~/Pages/Public/ConsultInscription.cshtml");
        }

        [HttpPost("~/consulta-inscripcion/buscar")]
        [EnableRateLimiting("public-lookup")]
        public async Task<IActionResult> BuscarInscripcion([FromForm] string docType, [FromForm] string docNumber, CancellationToken ct)
        {
            if (_captcha.IsEnabled)
            {
                var captchaToken = Request.Headers["X-Captcha-Token"].FirstOrDefault();
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var captchaResult = await _captcha.VerifyAsync(captchaToken, ip, ct);
                if (!captchaResult.Success)
                {
                    return Json(new
                    {
                        found = false,
                        captchaRequired = true,
                        message = "Verificación anti-bot requerida. Recargue la página e intente nuevamente."
                    });
                }
            }

            var effectiveDocType = string.IsNullOrWhiteSpace(docType) ? "DNI" : docType.Trim().ToUpperInvariant();
            if (effectiveDocType is not ("DNI" or "CE" or "PASAPORTE"))
            {
                return Json(new { found = false, message = "Tipo de documento no válido." });
            }

            var minLen = effectiveDocType == "DNI" ? 8 : 8;
            var maxLen = effectiveDocType == "DNI" ? 8 : effectiveDocType == "CE" ? 15 : 20;
            if (string.IsNullOrWhiteSpace(docNumber) || docNumber.Length < minLen || docNumber.Length > maxLen)
            {
                var label = effectiveDocType == "DNI" ? "DNI" : effectiveDocType == "CE" ? "C.E." : "Pasaporte";
                return Json(new { found = false, message = $"Ingrese un {label} válido ({minLen}–{maxLen} caracteres)." });
            }

            var results = await _lookups.FindByDocumentAsync(effectiveDocType, docNumber.Trim(), ct);

            if (results.Count == 0)
            {
                var label = effectiveDocType == "DNI" ? "DNI" : effectiveDocType == "CE" ? "C.E." : "Pasaporte";
                return Json(new { found = false, message = $"No se encontró una inscripción activa para este {label} en el periodo actual." });
            }

            var phone = await _config.GetConfigValueAsync(ADMISION.ENTITIES.Constants.ConfigGeneral.Telefono);
            var whatsappPhone = phone?.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "") ?? "";

            return Json(new
            {
                found = true,
                whatsappPhone,
                inscriptions = results.Select(r => new
                {
                    inscriptionId = r.InscriptionId,
                    codePostulant = r.CodePostulant,
                    fullName = r.FullName,
                    documentNumber = r.DocumentNumber,
                    documentType = r.DocumentType,
                    careerName = r.CareerName,
                    modalityName = r.ModalityName,
                    typeModalityName = r.TypeModalityName,
                    termName = r.TermName,
                    state = r.State,
                    inscriptionDate = r.InscriptionDate.ToString("dd/MM/yyyy HH:mm"),
                    canDownload = r.CanDownload,
                    isModalityActive = r.IsModalityActive,
                    isMockExam = r.IsMockExam,
                    files = r.Files.Select(f => new
                    {
                        name = f.Name,
                        kind = f.Kind,
                        isValidated = f.IsValidated,
                        observation = f.Observation
                    }),
                    observations = r.Observations.Select(o => new
                    {
                        observation = o.Observation,
                        createdAt = o.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                        createdBy = o.CreatedBy
                    })
                })
            });
        }

        [HttpGet("~/consulta-inscripcion/{inscriptionId:guid}/descargar")]
        [EnableRateLimiting("public-lookup")]
        public async Task<IActionResult> DescargarFicha(Guid inscriptionId, CancellationToken ct)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _documents.BuildConstanciaAsync(inscriptionId, baseUrl, onlyIfMockExam: true, ct);
            if (result == null) return NotFound();

            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{result.FileName}\"";
            return File(result.PdfBytes, "application/pdf");
        }

        [HttpGet("requirements")]
        public async Task<IActionResult> GetRequirements(Guid modalityId, Guid? typeModalityId, Guid? typePostulantId, CancellationToken ct)
        {
            var list = await _lookups.GetRequirementsAsync(modalityId, typeModalityId, typePostulantId, ct);
            return Json(list);
        }

        [HttpGet("type-postulant-requirement/{typePostulantId}")]
        public async Task<IActionResult> GetTypePostulantRequirement(Guid typePostulantId, CancellationToken ct)
        {
            var req = await _lookups.GetTypePostulantRequirementAsync(typePostulantId, ct);
            return Json(req);
        }

        [HttpGet("schools/{districtId}")]
        public async Task<IActionResult> GetSchools(Guid districtId, CancellationToken ct)
        {
            var schools = await _lookups.GetSchoolsByDistrictAsync(districtId, ct);
            return Json(schools.Select(s => new { s.Id, s.Name, s.Management }));
        }

        [HttpGet("payment-info")]
        public async Task<IActionResult> GetPaymentInfo(Guid modalityId, Guid? typeModalityId, Guid? typePostulantId, CancellationToken ct)
        {
            var info = await _lookups.GetPaymentInfoAsync(modalityId, typeModalityId, typePostulantId, ct);
            if (!info.RequiresPayment) return Json(new { requiresPayment = false });

            return Json(new
            {
                requiresPayment = true,
                baseAmount = info.BaseAmount,
                discountPercentage = info.DiscountPercentage,
                finalAmount = info.FinalAmount,
                conceptDescription = info.ConceptDescription,
                conceptCode = info.ConceptCode
            });
        }

        // ============================================================
        //  Inscripción — POST (registro)
        //  Toda la persistencia + uploads vive en IInscriptionService.
        //  El controller solo verifica captcha, parsea archivos y mapea Outcome a JSON.
        // ============================================================

        [HttpPost("~/inscription/register")]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("public-post")]
        [RequestSizeLimit(104_857_600)]
        [RequestFormLimits(MultipartBodyLengthLimit = 104_857_600)]
        public async Task<IActionResult> InscriptionRegister(EnrollmentViewModel model)
        {
            var userAgent = Request.Headers.UserAgent.ToString();
            _logger.LogInformation(
                "[Inscription] POST received — IP={IP} UA={UA}",
                HttpContext.Connection.RemoteIpAddress,
                userAgent);

            // Captcha — bloquea bots antes de tocar BD.
            if (_captcha.IsEnabled)
            {
                var captchaToken = Request.Form["cf-turnstile-response"].FirstOrDefault()
                    ?? Request.Form["g-recaptcha-response"].FirstOrDefault();
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var captchaResult = await _captcha.VerifyAsync(captchaToken, ipAddress, HttpContext.RequestAborted);
                if (!captchaResult.Success)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Verificación anti-bot fallida. Recargue la página y vuelva a intentarlo.",
                        captchaError = captchaResult.ErrorCode
                    });
                }
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = "Datos inválidos", errors });
            }

            // Server-side: reject registration if the modality is past its EndDate
            var endDate = await _lookups.GetExamEndDateAsync(model.ModalityId, HttpContext.RequestAborted);
            if (endDate <= DateTime.Now)
            {
                return Json(new { success = false, message = "Las inscripciones para esta modalidad han finalizado. Ya no se aceptan más registros." });
            }

            // Parsear archivos dinámicos del form (Requirements_{guid}).
            var requirementFiles = new List<RequirementFile>();
            foreach (var file in Request.Form.Files)
            {
                if (!file.Name.StartsWith("Requirements_")) continue;
                var idStr = file.Name.Replace("Requirements_", "");
                if (Guid.TryParse(idStr, out var requirementId))
                {
                    requirementFiles.Add(new RequirementFile(requirementId, file));
                }
            }

            var result = await _inscription.RegisterAsync(new InscriptionRegisterInput
            {
                Model = model,
                RequirementFiles = requirementFiles,
                RemoteIp = HttpContext.Connection.RemoteIpAddress?.ToString()
            }, HttpContext.RequestAborted);

            _logger.LogInformation(
                "[Inscription] Outcome={Outcome} Id={Id} IP={IP} UA={UA}",
                result.Outcome,
                result.InscriptionId,
                HttpContext.Connection.RemoteIpAddress,
                userAgent);

            string? downloadUrl = null;
            if (result.Outcome == InscriptionOutcome.Success)
            {
                var activeBrochure = await _brochures.GetActiveAsync(HttpContext.RequestAborted);
                if (activeBrochure != null)
                {
                    downloadUrl = "/" + activeBrochure.FileUrl;
                }
                else
                {
                    downloadUrl = "/broshure/broshure.pdf";
                }
            }

            return result.Outcome switch
            {
                InscriptionOutcome.Success => Json(new
                {
                    success = true,
                    message = "¡Inscripción recibida correctamente!",
                    inscriptionId = result.InscriptionId,
                    downloadUrl
                }),

                InscriptionOutcome.Duplicate => Json(new
                {
                    success = false,
                    message = result.Message
                }),

                InscriptionOutcome.InvalidFile => Json(new
                {
                    success = false,
                    message = $"El archivo \"{result.FileName}\" no es válido.",
                    fileName = result.FileName,
                    fileReason = result.FileReason,
                    fileContext = result.FileContextLabel
                }),

                InscriptionOutcome.Blocked => Json(new
                {
                    success = false,
                    message = result.Message
                }),

                InscriptionOutcome.Error => result.Message != null
                    ? Json(new { success = false, message = result.Message })
                    : BuildErrorResponse(result),

                _ => BuildErrorResponse(result)
            };
        }

        private IActionResult BuildErrorResponse(InscriptionRegisterResult result)
        {
            var correlationId = result.CorrelationId ?? Guid.NewGuid().ToString("N");
            var message = $"Ocurrió un error al procesar la inscripción. Código de referencia: {correlationId}";

            if (_env.IsDevelopment() && result.Exception != null)
            {
                return Json(new
                {
                    success = false,
                    message,
                    debug = new
                    {
                        exceptionType = result.Exception.GetType().FullName,
                        exceptionMessage = result.Exception.Message,
                        innerMessage = result.Exception.InnerException?.Message,
                        stackTrace = result.Exception.StackTrace
                    }
                });
            }

            return Json(new { success = false, message });
        }
    }
}
