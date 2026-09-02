using ADMISION.ENTITIES.Constants;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.InfoPostulant.AttendanceController
{
    [Route("admin/info-postulant/attendance")]
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin + "," + AppConstants.Roles.Consultor)]
    public class AttendanceController : Controller
    {
        private readonly IAttendanceService _attendance;

        public AttendanceController(IAttendanceService attendance)
        {
            _attendance = attendance;
        }

        [HttpGet("")]
        public IActionResult Index() => View("~/Pages/Admin/InfoPostulant/Attendance/Index.cshtml");

        [HttpGet("search")]
        public async Task<IActionResult> Search(string code, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(code)) return BadRequest("Código requerido.");

            var result = await _attendance.SearchByCodeAsync(code, ct);
            if (result == null)
                return NotFound(new { success = false, message = "Postulante no encontrado o código incorrecto." });

            var i = result.Inscription;
            return Json(new
            {
                success = true,
                inscription = new
                {
                    id = i.Id,
                    code = i.Code,
                    fullName = i.FullName,
                    document = i.Document,
                    careerName = i.CareerName,
                    termName = i.TermName,
                    photoUrl = i.PhotoUrl,
                    fingerprintsCount = i.FingerprintsCount,
                    state = i.State
                },
                attendance = result.Attendance == null ? null : new
                {
                    verifiedAt = result.Attendance.VerifiedAt,
                    verifiedBy = result.Attendance.VerifiedBy,
                    status = result.Attendance.BiometricStatus,
                    notes = result.Attendance.Notes
                }
            });
        }

        [HttpPost("verify-biometric")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyBiometric([FromBody] VerifyRequest request, CancellationToken ct)
        {
            var outcome = await _attendance.VerifyBiometricAsync(request.InscriptionId, User.Identity?.Name ?? "Sistema", ct);
            if (outcome.Success)
                return Json(new { success = true, score = outcome.Score, message = outcome.Message });

            return outcome.Error switch
            {
                BiometricVerifyError.InscriptionNotFound => NotFound(outcome.Message),
                BiometricVerifyError.NotApproved => BadRequest(new { success = false, notApproved = true, message = outcome.Message }),
                _ => BadRequest(new { success = false, message = outcome.Message })
            };
        }

        [HttpPost("manual")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManualVerify([FromBody] ManualRequest request, CancellationToken ct)
        {
            var outcome = await _attendance.RegisterManualAsync(request.InscriptionId, request.Notes, User.Identity?.Name ?? "Sistema", ct);
            if (outcome.Success)
                return Json(new { success = true, message = outcome.Message });

            return outcome.Error switch
            {
                ManualVerifyError.InscriptionNotFound => NotFound(outcome.Message),
                ManualVerifyError.NotApproved => BadRequest(new { success = false, notApproved = true, message = outcome.Message }),
                _ => BadRequest(new { success = false, message = outcome.Message })
            };
        }

        [HttpGet("{inscriptionId:guid}/verify-templates")]
        public async Task<IActionResult> GetVerifyTemplates(Guid inscriptionId, CancellationToken ct)
        {
            var templates = await _attendance.GetVerifyTemplatesAsync(inscriptionId, ct);
            if (templates.Count == 0)
                return NotFound(new { success = false, message = "El postulante no tiene huellas registradas en el sistema." });

            return Json(new { success = true, templates });
        }

        [HttpPost("record-local-verify")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordLocalVerify([FromBody] LocalVerifyRequest request, CancellationToken ct)
        {
            var outcome = await _attendance.RecordLocalVerifyAsync(request.InscriptionId, request.Score, User.Identity?.Name ?? "Sistema", ct);
            if (outcome.Success)
                return Json(new { success = true, score = outcome.Score, message = outcome.Message });

            return outcome.Error switch
            {
                BiometricVerifyError.InscriptionNotFound => NotFound(outcome.Message),
                BiometricVerifyError.NotApproved => BadRequest(new { success = false, notApproved = true, message = outcome.Message }),
                _ => BadRequest(new { success = false, message = outcome.Message })
            };
        }

        [HttpGet("history")]
        public IActionResult History() => View("~/Pages/Admin/InfoPostulant/AttendanceHistory/Index.cshtml");

        [HttpGet("history/search")]
        public async Task<IActionResult> HistorySearch(string code, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { success = false, message = "Código requerido." });

            var items = await _attendance.GetAttendanceHistoryAsync(code.Trim(), ct);
            return Json(new { success = true, items });
        }

        [HttpPost("qr-register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QrRegister([FromBody] QrRegisterRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                return BadRequest(new { success = false, message = "Código requerido." });

            var result = await _attendance.SearchByCodeAsync(request.Code.Trim(), ct);
            if (result == null)
                return NotFound(new { success = false, message = "Postulante no encontrado." });

            if (result.Attendance != null)
            {
                var i = result.Inscription;
                return Json(new
                {
                    success = true,
                    alreadyRegistered = true,
                    message = "Este postulante ya registró su asistencia.",
                    inscription = new
                    {
                        id = i.Id,
                        code = i.Code,
                        fullName = i.FullName,
                        document = i.Document,
                        careerName = i.CareerName,
                        termName = i.TermName
                    },
                    attendance = new
                    {
                        verifiedAt = result.Attendance.VerifiedAt,
                        verifiedBy = result.Attendance.VerifiedBy,
                        notes = result.Attendance.Notes
                    }
                });
            }

            var outcome = await _attendance.RegisterManualAsync(result.Inscription.Id, "Registro por código QR de constancia", User.Identity?.Name ?? "Sistema", ct);
            if (!outcome.Success)
            {
                return outcome.Error switch
                {
                    ManualVerifyError.InscriptionNotFound => NotFound(new { success = false, message = outcome.Message }),
                    ManualVerifyError.AlreadyMarked => Json(new { success = true, alreadyRegistered = true, message = "La asistencia ya fue registrada." }),
                    ManualVerifyError.NotApproved => Json(new { success = false, notApproved = true, message = outcome.Message }),
                    _ => BadRequest(new { success = false, message = outcome.Message })
                };
            }

            var ins = result.Inscription;
            return Json(new
            {
                success = true,
                alreadyRegistered = false,
                message = "Asistencia registrada correctamente mediante código QR.",
                inscription = new
                {
                    id = ins.Id,
                    code = ins.Code,
                    fullName = ins.FullName,
                    document = ins.Document,
                    careerName = ins.CareerName,
                    termName = ins.TermName
                }
            });
        }

        public class VerifyRequest
        {
            public Guid InscriptionId { get; set; }
        }

        public class ManualRequest
        {
            public Guid InscriptionId { get; set; }
            public string Notes { get; set; } = string.Empty;
        }

        public class QrRegisterRequest
        {
            public string Code { get; set; } = string.Empty;
        }

        public class LocalVerifyRequest
        {
            public Guid InscriptionId { get; set; }
            public int Score { get; set; }
        }
    }
}
