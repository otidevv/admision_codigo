using System.Text.Json;
using ADMISION.Services.Interfaces;

namespace ADMISION.Services.Implementations
{
    public class CaptchaService : ICaptchaService
    {
        private const string TurnstileVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
        private const string RecaptchaVerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

        private readonly HttpClient _http;
        private readonly ILogger<CaptchaService> _logger;
        private readonly string _provider;
        private readonly string? _secretKey;
        private readonly bool _enabled;

        public CaptchaService(HttpClient http, IConfiguration configuration, ILogger<CaptchaService> logger)
        {
            _http = http;
            _logger = logger;

            _enabled = configuration.GetValue("Captcha:Enabled", false);
            _provider = (configuration["Captcha:Provider"] ?? "Turnstile").Trim();
            SiteKey = configuration["Captcha:SiteKey"];
            _secretKey = configuration["Captcha:SecretKey"];
        }

        public bool IsEnabled => _enabled;
        public string? SiteKey { get; }
        public string? Provider => _provider;

        public async Task<CaptchaResult> VerifyAsync(string? token, string? remoteIp, CancellationToken ct = default)
        {
            if (!_enabled)
            {
                return new CaptchaResult(true);
            }

            if (string.IsNullOrWhiteSpace(_secretKey))
            {
                _logger.LogError("Captcha está habilitado pero no se configuró Captcha:SecretKey. Rechazando solicitud.");
                return new CaptchaResult(false, "missing-secret");
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return new CaptchaResult(false, "missing-token");
            }

            var verifyUrl = string.Equals(_provider, "ReCaptcha", StringComparison.OrdinalIgnoreCase)
                ? RecaptchaVerifyUrl
                : TurnstileVerifyUrl;

            var form = new List<KeyValuePair<string, string>>
            {
                new("secret", _secretKey),
                new("response", token)
            };
            if (!string.IsNullOrWhiteSpace(remoteIp))
            {
                form.Add(new KeyValuePair<string, string>("remoteip", remoteIp));
            }

            try
            {
                using var content = new FormUrlEncodedContent(form);
                using var response = await _http.PostAsync(verifyUrl, content, ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Captcha verify HTTP {Status} desde {Provider}", (int)response.StatusCode, _provider);
                    return new CaptchaResult(false, "verify-http-error");
                }

                using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

                var success = doc.RootElement.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
                if (success)
                {
                    return new CaptchaResult(true);
                }

                string? firstError = null;
                if (doc.RootElement.TryGetProperty("error-codes", out var errors) && errors.ValueKind == JsonValueKind.Array)
                {
                    if (errors.GetArrayLength() > 0)
                    {
                        firstError = errors[0].GetString();
                    }
                }
                _logger.LogInformation("Captcha rechazado por {Provider}. Error={Error}", _provider, firstError ?? "unknown");
                return new CaptchaResult(false, firstError ?? "verification-failed");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fallo verificando Captcha contra {Provider}", _provider);
                return new CaptchaResult(false, "verify-exception");
            }
        }
    }
}
