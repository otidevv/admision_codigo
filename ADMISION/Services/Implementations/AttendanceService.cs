using System.Net.Http.Json;
using System.Text.Json;
using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Biometrics;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class AttendanceService : IAttendanceService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _http;
        private readonly ILogger<AttendanceService> _logger;

        public AttendanceService(AppDbContext context, IConfiguration configuration, HttpClient http, ILogger<AttendanceService> logger)
        {
            _context = context;
            _configuration = configuration;
            _http = http;
            _logger = logger;
        }

        public async Task<AttendanceLookupResult?> SearchByCodeAsync(string code, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            var today = DateOnly.FromDateTime(DateTime.Today);
            var activeTerm = await _context.Terms
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IsActive && t.EndDate >= today, ct);

            if (activeTerm == null) return null;

            var inscription = await _context.Inscriptions
                .AsNoTracking()
                .Include(i => i.Postulant).ThenInclude(p => p!.User)
                .Include(i => i.Career)
                .Include(i => i.Modality).ThenInclude(m => m!.Term)
                .Where(i => i.CodePostulant == code && i.Modality != null && i.Modality.TermId == activeTerm.Id)
                .OrderByDescending(i => i.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (inscription == null) return null;

            var fingerprintsCount = await _context.Fingerprints
                .CountAsync(f => f.PostulantId == inscription.PostulantId, ct);

            var attendance = await _context.PostulantAttendances
                .AsNoTracking()
                .FirstOrDefaultAsync(pa => pa.InscriptionId == inscription.Id, ct);

            var info = new InscriptionInfo(
                inscription.Id,
                inscription.CodePostulant,
                inscription.Postulant?.User?.FullName ?? "S/N",
                inscription.Postulant?.User?.Document ?? "-",
                inscription.Career?.Name ?? "-",
                inscription.Modality?.Term?.Name ?? "-",
                inscription.Postulant?.User?.PhotoUrl,
                fingerprintsCount,
                inscription.State);

            AttendanceInfo? attendanceInfo = attendance == null
                ? null
                : new AttendanceInfo(
                    attendance.VerifiedAt.ToString("dd/MM/yyyy HH:mm"),
                    attendance.VerifiedBy,
                    attendance.BiometricStatus,
                    attendance.Notes);

            return new AttendanceLookupResult(info, attendanceInfo);
        }

        public async Task<BiometricVerifyOutcome> VerifyBiometricAsync(Guid inscriptionId, string actor, CancellationToken ct = default)
        {
            if (inscriptionId == Guid.Empty)
                return BiometricVerifyOutcome.Fail(BiometricVerifyError.InvalidId, "ID Inscription inválido.");

            var inscription = await _context.Inscriptions
                .Include(i => i.Postulant)
                .FirstOrDefaultAsync(i => i.Id == inscriptionId, ct);
            if (inscription == null)
                return BiometricVerifyOutcome.Fail(BiometricVerifyError.InscriptionNotFound, "Inscripción no encontrada.");

            if (inscription.State != AppConstants.InscripcionState.Aprobado)
                return BiometricVerifyOutcome.Fail(BiometricVerifyError.NotApproved, "No se puede registrar asistencia: la inscripción se encuentra en estado " + inscription.State + ". Solo se permite para inscripciones Aprobadas.");

            if (await _context.PostulantAttendances.AnyAsync(pa => pa.InscriptionId == inscription.Id, ct))
                return BiometricVerifyOutcome.Fail(BiometricVerifyError.AlreadyMarked, "El postulante ya tiene asistencia marcada para esta inscripción.");

            var templates = await _context.Fingerprints
                .Where(f => f.PostulantId == inscription.PostulantId)
                .Select(f => f.Template)
                .ToListAsync(ct);
            if (!templates.Any())
                return BiometricVerifyOutcome.Fail(BiometricVerifyError.NoFingerprints, "El postulante no tiene huellas registradas en el sistema.");

            try
            {
                var baseUrl = _configuration["BiometricBridge:BaseUrl"] ?? "http://localhost:5000";
                var payload = new { StoredTemplates = templates.ToArray() };
                using var response = await _http.PostAsJsonAsync($"{baseUrl}/api/biometric/verify", payload, ct);
                var content = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(content);
                    var msg = doc.RootElement.TryGetProperty("message", out var msgProp)
                        ? msgProp.GetString()
                        : "Lector no disponible o huella no detectada.";
                    return BiometricVerifyOutcome.Fail(BiometricVerifyError.BridgeUnavailable, msg ?? "Lector no disponible.");
                }

                using var okDoc = JsonDocument.Parse(content);
                var matched = okDoc.RootElement.GetProperty("matched").GetBoolean();
                var score = okDoc.RootElement.GetProperty("score").GetInt32();

                if (!matched)
                    return BiometricVerifyOutcome.Fail(BiometricVerifyError.NotMatched, "Identidad no verificada. La huella no coincide con los registros.");

                _context.PostulantAttendances.Add(new PostulantAttendance
                {
                    Id = Guid.NewGuid(),
                    InscriptionId = inscription.Id,
                    BiometricStatus = "Verificado",
                    BiometricScore = score,
                    VerifiedAt = DateTimeOffset.UtcNow,
                    VerifiedBy = actor
                });
                await _context.SaveChangesAsync(ct);
                return BiometricVerifyOutcome.Ok(score);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallo al verificar biométrico contra BiometricBridge para inscripción {InscriptionId}", inscriptionId);
                return BiometricVerifyOutcome.Fail(BiometricVerifyError.BridgeUnavailable,
                    "Error de conexión con el lector biométrico. Asegúrese de que BiometricBridge esté en ejecución.");
            }
        }

        public async Task<List<string>> GetVerifyTemplatesAsync(Guid inscriptionId, CancellationToken ct = default)
        {
            if (inscriptionId == Guid.Empty) return new List<string>();

            var postulantId = await _context.Inscriptions
                .Where(i => i.Id == inscriptionId)
                .Select(i => i.PostulantId)
                .FirstOrDefaultAsync(ct);

            if (postulantId == null) return new List<string>();

            var templates = await _context.Fingerprints
                .Where(f => f.PostulantId == postulantId)
                .Select(f => f.Template)
                .Where(t => t != null)
                .ToListAsync(ct);

            return templates!;
        }

        public async Task<BiometricVerifyOutcome> RecordLocalVerifyAsync(Guid inscriptionId, int score, string actor, CancellationToken ct = default)
        {
            if (inscriptionId == Guid.Empty)
                return BiometricVerifyOutcome.Fail(BiometricVerifyError.InvalidId, "ID Inscription inválido.");

            var inscription = await _context.Inscriptions.FindAsync(new object[] { inscriptionId }, ct);
            if (inscription == null)
                return BiometricVerifyOutcome.Fail(BiometricVerifyError.InscriptionNotFound, "Inscripción no encontrada.");

            if (inscription.State != AppConstants.InscripcionState.Aprobado)
                return BiometricVerifyOutcome.Fail(BiometricVerifyError.NotApproved, "No se puede registrar asistencia: la inscripción se encuentra en estado " + inscription.State + ". Solo se permite para inscripciones Aprobadas.");

            if (await _context.PostulantAttendances.AnyAsync(pa => pa.InscriptionId == inscription.Id, ct))
                return BiometricVerifyOutcome.Fail(BiometricVerifyError.AlreadyMarked, "El postulante ya tiene asistencia marcada para esta inscripción.");

            _context.PostulantAttendances.Add(new PostulantAttendance
            {
                Id = Guid.NewGuid(),
                InscriptionId = inscription.Id,
                BiometricStatus = "Verificado",
                BiometricScore = score,
                VerifiedAt = DateTimeOffset.UtcNow,
                VerifiedBy = actor
            });
            await _context.SaveChangesAsync(ct);
            return BiometricVerifyOutcome.Ok(score);
        }

        public async Task<List<AttendanceHistoryItem>> GetAttendanceHistoryAsync(string code, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(code)) return new();

            return await _context.PostulantAttendances
                .AsNoTracking()
                .Include(pa => pa.Inscription)
                    .ThenInclude(i => i!.Postulant).ThenInclude(p => p!.User)
                .Include(pa => pa.Inscription)
                    .ThenInclude(i => i!.Career)
                .Include(pa => pa.Inscription)
                    .ThenInclude(i => i!.Modality).ThenInclude(m => m!.Term)
                .Where(pa => pa.Inscription != null
                    && pa.Inscription.CodePostulant == code
                    && pa.Inscription.Modality != null)
                .OrderByDescending(pa => pa.VerifiedAt)
                .Select(pa => new AttendanceHistoryItem(
                    pa.Inscription!.Id,
                    pa.Inscription.CodePostulant,
                    pa.Inscription.Postulant!.User!.FullName,
                    pa.Inscription.Postulant.User.Document,
                    pa.Inscription.Career!.Name,
                    pa.Inscription.Modality!.Name,
                    pa.Inscription.Modality.Term!.Name,
                    pa.BiometricStatus,
                    pa.BiometricScore,
                    pa.VerifiedAt,
                    pa.VerifiedBy,
                    pa.Notes))
                .ToListAsync(ct);
        }

        public async Task<List<AttendanceHistoryItem>> GetAttendanceHistoryByPostulantAsync(Guid postulantId, CancellationToken ct = default)
        {
            if (postulantId == Guid.Empty) return new();

            return await _context.PostulantAttendances
                .AsNoTracking()
                .Include(pa => pa.Inscription)
                    .ThenInclude(i => i!.Postulant).ThenInclude(p => p!.User)
                .Include(pa => pa.Inscription)
                    .ThenInclude(i => i!.Career)
                .Include(pa => pa.Inscription)
                    .ThenInclude(i => i!.Modality).ThenInclude(m => m!.Term)
                .Where(pa => pa.Inscription != null
                    && pa.Inscription.PostulantId == postulantId
                    && pa.Inscription.Modality != null)
                .OrderByDescending(pa => pa.VerifiedAt)
                .Select(pa => new AttendanceHistoryItem(
                    pa.Inscription!.Id,
                    pa.Inscription.CodePostulant,
                    pa.Inscription.Postulant!.User!.FullName,
                    pa.Inscription.Postulant.User.Document,
                    pa.Inscription.Career!.Name,
                    pa.Inscription.Modality!.Name,
                    pa.Inscription.Modality.Term!.Name,
                    pa.BiometricStatus,
                    pa.BiometricScore,
                    pa.VerifiedAt,
                    pa.VerifiedBy,
                    pa.Notes))
                .ToListAsync(ct);
        }

        public async Task<ManualVerifyOutcome> RegisterManualAsync(Guid inscriptionId, string notes, string actor, CancellationToken ct = default)
        {
            if (inscriptionId == Guid.Empty)
                return ManualVerifyOutcome.Fail(ManualVerifyError.InvalidId, "ID Inscription inválido.");
            if (string.IsNullOrWhiteSpace(notes))
                return ManualVerifyOutcome.Fail(ManualVerifyError.NotesRequired, "Es obligatorio ingresar un motivo para la validación manual.");

            var inscription = await _context.Inscriptions.FindAsync(new object[] { inscriptionId }, ct);
            if (inscription == null)
                return ManualVerifyOutcome.Fail(ManualVerifyError.InscriptionNotFound, "Inscripción no encontrada.");

            if (inscription.State != AppConstants.InscripcionState.Aprobado)
                return ManualVerifyOutcome.Fail(ManualVerifyError.NotApproved, "No se puede registrar asistencia: la inscripción se encuentra en estado " + inscription.State + ". Solo se permite para inscripciones Aprobadas.");

            if (await _context.PostulantAttendances.AnyAsync(pa => pa.InscriptionId == inscription.Id, ct))
                return ManualVerifyOutcome.Fail(ManualVerifyError.AlreadyMarked, "El postulante ya tiene asistencia marcada para esta inscripción.");

            _context.PostulantAttendances.Add(new PostulantAttendance
            {
                Id = Guid.NewGuid(),
                InscriptionId = inscription.Id,
                BiometricStatus = "Manual",
                BiometricScore = null,
                VerifiedAt = DateTimeOffset.UtcNow,
                VerifiedBy = actor,
                Notes = notes
            });
            await _context.SaveChangesAsync(ct);
            return ManualVerifyOutcome.Ok();
        }
    }
}
