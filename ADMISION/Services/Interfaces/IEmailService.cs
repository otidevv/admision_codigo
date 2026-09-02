using System.Threading;
using System.Threading.Tasks;

namespace ADMISION.Services.Interfaces
{
    public readonly record struct EmailSendResult(bool Success, string? Error);

    public interface IEmailService
    {
        Task<bool> IsConfiguredAsync(CancellationToken ct = default);
        Task<EmailSendResult> SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
    }
}
