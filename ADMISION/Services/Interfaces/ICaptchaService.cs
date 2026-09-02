namespace ADMISION.Services.Interfaces
{
    public interface ICaptchaService
    {
        bool IsEnabled { get; }
        string? SiteKey { get; }
        string? Provider { get; }

        Task<CaptchaResult> VerifyAsync(string? token, string? remoteIp, CancellationToken ct = default);
    }

    public record CaptchaResult(bool Success, string? ErrorCode = null);
}
