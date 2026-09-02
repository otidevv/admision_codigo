using ADMISION.ENTITIES.Constants;
using ADMISION.Services.Interfaces;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace ADMISION.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IConfigService _config;

        public EmailService(IConfigService config)
        {
            _config = config;
        }

        public async Task<bool> IsConfiguredAsync(CancellationToken ct = default)
        {
            var settings = await LoadSettingsAsync();
            return !string.IsNullOrWhiteSpace(settings.Host)
                && !string.IsNullOrWhiteSpace(settings.From)
                && !string.IsNullOrWhiteSpace(settings.Password);
        }

        public async Task<EmailSendResult> SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            var settings = await LoadSettingsAsync();
            if (string.IsNullOrWhiteSpace(settings.Host)
                || string.IsNullOrWhiteSpace(settings.From)
                || string.IsNullOrWhiteSpace(settings.Password))
            {
                return new EmailSendResult(false, "El servidor SMTP no está configurado. Revise Configuración > Información del sistema.");
            }

            try
            {
#pragma warning disable SYSLIB0016
                using var smtp = new SmtpClient(settings.Host, settings.Port)
                {
                    EnableSsl = settings.EnableSsl,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(settings.From, settings.Password),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 30000
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(settings.From, settings.SenderName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                message.To.Add(to);

                await smtp.SendMailAsync(message, ct);
#pragma warning restore SYSLIB0016
                return new EmailSendResult(true, null);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new EmailSendResult(false, ex.Message);
            }
        }

        private async Task<SmtpSettings> LoadSettingsAsync()
        {
            var host = await _config.GetConfigValueAsync(ConfigGeneral.SmtpHost);
            var portText = await _config.GetConfigValueAsync(ConfigGeneral.SmtpPort);
            var sslText = await _config.GetConfigValueAsync(ConfigGeneral.SmtpEnableSsl);
            var senderName = await _config.GetConfigValueAsync(ConfigGeneral.SmtpSenderName);
            var from = await _config.GetConfigValueAsync(ConfigGeneral.SmtpEmail);
            var password = await _config.GetConfigValueAsync(ConfigGeneral.SmtpPassword);

            return new SmtpSettings(
                host,
                int.TryParse(portText, out var port) ? port : 587,
                !string.Equals(sslText, "false", StringComparison.OrdinalIgnoreCase),
                string.IsNullOrWhiteSpace(senderName) ? from : senderName,
                from,
                password);
        }

        private readonly record struct SmtpSettings(string Host, int Port, bool EnableSsl, string SenderName, string From, string Password);
    }
}
