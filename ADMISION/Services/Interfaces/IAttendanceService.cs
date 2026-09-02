namespace ADMISION.Services.Interfaces
{
    /// <summary>
    /// Verificación de asistencia biométrica de postulantes (consume el bridge ZK
    /// vía HTTP local) y registro de asistencia manual con observación.
    /// </summary>
    public interface IAttendanceService
    {
        Task<AttendanceLookupResult?> SearchByCodeAsync(string code, CancellationToken ct = default);
        Task<BiometricVerifyOutcome> VerifyBiometricAsync(Guid inscriptionId, string actor, CancellationToken ct = default);
        Task<List<string>> GetVerifyTemplatesAsync(Guid inscriptionId, CancellationToken ct = default);
        Task<ManualVerifyOutcome> RegisterManualAsync(Guid inscriptionId, string notes, string actor, CancellationToken ct = default);
        Task<BiometricVerifyOutcome> RecordLocalVerifyAsync(Guid inscriptionId, int score, string actor, CancellationToken ct = default);
        Task<List<AttendanceHistoryItem>> GetAttendanceHistoryAsync(string code, CancellationToken ct = default);
        Task<List<AttendanceHistoryItem>> GetAttendanceHistoryByPostulantAsync(Guid postulantId, CancellationToken ct = default);
    }

    public record AttendanceHistoryItem(
        Guid InscriptionId,
        string CodePostulant,
        string FullName,
        string Document,
        string CareerName,
        string ModalityName,
        string TermName,
        string BiometricStatus,
        int? BiometricScore,
        DateTimeOffset VerifiedAt,
        string VerifiedBy,
        string? Notes);

    public record AttendanceLookupResult(
        InscriptionInfo Inscription,
        AttendanceInfo? Attendance);

    public record InscriptionInfo(
        Guid Id,
        string Code,
        string FullName,
        string Document,
        string CareerName,
        string TermName,
        string? PhotoUrl,
        int FingerprintsCount,
        string State);

    public record AttendanceInfo(
        string VerifiedAt,
        string? VerifiedBy,
        string BiometricStatus,
        string? Notes);

    public class BiometricVerifyOutcome
    {
        public bool Success { get; init; }
        public int? Score { get; init; }
        public string? Message { get; init; }
        public BiometricVerifyError? Error { get; init; }

        public static BiometricVerifyOutcome Ok(int score) => new()
        {
            Success = true,
            Score = score,
            Message = "Asistencia biométrica verificada correctamente."
        };
        public static BiometricVerifyOutcome Fail(BiometricVerifyError error, string message) => new()
        {
            Success = false,
            Error = error,
            Message = message
        };
    }

    public enum BiometricVerifyError
    {
        InvalidId,
        InscriptionNotFound,
        AlreadyMarked,
        NoFingerprints,
        BridgeUnavailable,
        NotMatched,
        NotApproved
    }

    public class ManualVerifyOutcome
    {
        public bool Success { get; init; }
        public string? Message { get; init; }
        public ManualVerifyError? Error { get; init; }

        public static ManualVerifyOutcome Ok() => new() { Success = true, Message = "Asistencia manual registrada correctamente." };
        public static ManualVerifyOutcome Fail(ManualVerifyError error, string message) => new() { Success = false, Error = error, Message = message };
    }

    public enum ManualVerifyError
    {
        InvalidId,
        NotesRequired,
        InscriptionNotFound,
        AlreadyMarked,
        NotApproved
    }
}
