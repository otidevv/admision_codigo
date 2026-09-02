using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using Scriban;
using Scriban.Runtime;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ADMISION.Services.Implementations
{
    public class DocumentService : IDocumentService
    {
        private readonly AppDbContext _context;
        private readonly IConfigService _configService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<DocumentService> _logger;
        private readonly IConstanciaIngresoPdfRenderer _constanciaRenderer;

        private static readonly ConcurrentDictionary<string, (DateTime stamp, Template tpl)> _templateCache
            = new(StringComparer.OrdinalIgnoreCase);

        private static readonly SemaphoreSlim _browserInitLock = new(1, 1);
        private static bool _browserDownloaded;

        private static readonly CultureInfo _esPE = new("es-PE");

        public DocumentService(
            AppDbContext context,
            IConfigService configService,
            IWebHostEnvironment env,
            ILogger<DocumentService> logger,
            IConstanciaIngresoPdfRenderer constanciaRenderer)
        {
            _context = context;
            _configService = configService;
            _env = env;
            _logger = logger;
            _constanciaRenderer = constanciaRenderer;
        }

        public async Task<DocumentResult> GenerateConstanciaIngresoPdfAsync(
            ConstanciaIngresoModel model,
            DocumentOptions? options = null,
            string? userName = null)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            options ??= new DocumentOptions();

            model.DirectorCommissionName ??= await _configService.GetConfigValueAsync(ConfigGeneral.DirecctorComision);
            model.IssuedAt = DateTimeOffset.Now;

            if (string.IsNullOrWhiteSpace(model.InstitutionName))
                model.InstitutionName = "Universidad Nacional Amazónica de Madre de Dios";

            var pdfBytes = _constanciaRenderer.Render(model);

            var fileName = !string.IsNullOrWhiteSpace(options.FileName)
                ? options.FileName!
                : $"ConstanciaIngreso_{Sanitize(model.PostulantCode)}_{Sanitize(model.FullName)}";

            return new DocumentResult
            {
                PdfBytes = pdfBytes,
                FileName = fileName + ".pdf"
            };
        }

        public async Task<DocumentResult> GeneratePdfFromTemplateAsync(
            string templateName,
            IDictionary<string, object?> data,
            DocumentOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(templateName))
                throw new ArgumentException("Nombre de plantilla requerido.", nameof(templateName));

            options ??= new DocumentOptions();
            data ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            var html = await RenderHtmlAsync(templateName, data, options);

            if (!string.IsNullOrWhiteSpace(options.WatermarkText))
                html = InjectWatermark(html, options.WatermarkText!);

            var pdfBytes = await RenderPdfAsync(html, options);

            var fileName = !string.IsNullOrWhiteSpace(options.FileName)
                ? options.FileName!
                : Sanitize(templateName);

            return new DocumentResult
            {
                PdfBytes = pdfBytes,
                FileName = fileName + ".pdf"
            };
        }

        public async Task<string> RenderHtmlAsync(
            string templateName,
            IDictionary<string, object?> data,
            DocumentOptions? options = null)
        {
            options ??= new DocumentOptions();
            data ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            var template = await GetCompiledTemplateAsync(templateName);

            var ctx = new TemplateContext
            {
                MemberRenamer = m => m.Name,
                LoopLimit = 5000,
                StrictVariables = false
            };

            var globals = new ScriptObject();
            globals["data"] = NormalizeData(data);
            ctx.PushGlobal(globals);

            try
            {
                return await template.RenderAsync(ctx);
            }
            finally
            {
                ctx.PopGlobal();
            }
        }

        private object NormalizeData(IDictionary<string, object?> data)
        {
            var so = new ScriptObject();
            foreach (var kv in data)
            {
                if (kv.Value is string s && LooksLikeAssetPath(s))
                    so[kv.Key] = ResolveAssetUri(s);
                else
                    so[kv.Key] = kv.Value;
            }
            return so;
        }

        private async Task<Template> GetCompiledTemplateAsync(string templateName)
        {
            var path = ResolveTemplatePath(templateName);
            var info = new FileInfo(path);
            if (!info.Exists)
                throw new FileNotFoundException($"No se encontró la plantilla '{templateName}.html' en {path}");

            if (_templateCache.TryGetValue(path, out var cached) && cached.stamp == info.LastWriteTimeUtc)
                return cached.tpl;

            var raw = await File.ReadAllTextAsync(path);
            var tpl = Template.Parse(raw, path);
            if (tpl.HasErrors)
            {
                var msg = string.Join(" | ", tpl.Messages);
                throw new InvalidOperationException($"La plantilla '{templateName}' tiene errores de sintaxis: {msg}");
            }
            _templateCache[path] = (info.LastWriteTimeUtc, tpl);
            return tpl;
        }

        private string ResolveTemplatePath(string templateName)
        {
            var safe = templateName.Replace("..", string.Empty).TrimStart('/', '\\');
            if (!safe.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                safe += ".html";
            return Path.Combine(_env.ContentRootPath, "Templates", "Documents", safe);
        }

        private static bool LooksLikeAssetPath(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (s.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return false;
            if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) return false;
            if (s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return false;
            if (s.StartsWith("file://", StringComparison.OrdinalIgnoreCase)) return false;
            return s.StartsWith("/") || s.StartsWith("~/") || s.StartsWith("img/") || s.StartsWith("uploads/") || s.StartsWith("assets/");
        }

        private string? ResolveAssetUri(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;
            if (path.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return path;
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) return path;
            if (path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return path;
            if (path.StartsWith("file://", StringComparison.OrdinalIgnoreCase)) return path;

            var trimmed = path.TrimStart('~').TrimStart('/', '\\');
            string? full = null;

            if (trimmed.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
            {
                full = Path.Combine(_env.ContentRootPath, "Templates", "Documents", trimmed.Replace('/', Path.DirectorySeparatorChar));
            }
            else if (trimmed.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            {
                var webRoot = string.IsNullOrEmpty(_env.WebRootPath)
                    ? Path.Combine(_env.ContentRootPath, "wwwroot")
                    : _env.WebRootPath;
                full = Path.Combine(webRoot, trimmed.Replace('/', Path.DirectorySeparatorChar));
            }
            else
            {
                var webRoot = string.IsNullOrEmpty(_env.WebRootPath)
                    ? Path.Combine(_env.ContentRootPath, "wwwroot")
                    : _env.WebRootPath;
                full = Path.Combine(webRoot, trimmed.Replace('/', Path.DirectorySeparatorChar));
            }

            if (!string.IsNullOrEmpty(full) && File.Exists(full))
                return new Uri(full).AbsoluteUri;

            return path;
        }

        internal static string InjectWatermark(string html, string watermarkText)
        {
            var safeText = System.Net.WebUtility.HtmlEncode(watermarkText);
            var watermarkCss = @"
<style id=""__doc_watermark__"">
    body { position: relative; }
    body::before {
        content: """ + safeText + @""";
        position: fixed;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%) rotate(-30deg);
        font-size: 88pt;
        font-weight: 800;
        color: rgba(245, 68, 119, 0.10);
        letter-spacing: 6px;
        white-space: nowrap;
        text-transform: uppercase;
        pointer-events: none;
        z-index: 9999;
        -webkit-print-color-adjust: exact;
        print-color-adjust: exact;
    }
</style>";

            var headClose = Regex.Match(html, @"</head\s*>", RegexOptions.IgnoreCase);
            if (headClose.Success)
                return html.Insert(headClose.Index, watermarkCss);

            var bodyOpen = Regex.Match(html, @"<body[^>]*>", RegexOptions.IgnoreCase);
            if (bodyOpen.Success)
                return html.Insert(bodyOpen.Index + bodyOpen.Length, watermarkCss);

            return watermarkCss + html;
        }

        internal async Task<byte[]> RenderPdfAsync(string html, DocumentOptions options)
        {
            await EnsureBrowserAsync();

            var launchOptions = new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            };

            await using var browser = await Puppeteer.LaunchAsync(launchOptions);
            await using var page = await browser.NewPageAsync();

            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                Timeout = 30000
            });

            var pdfOptions = new PdfOptions
            {
                Format = ParsePaperFormat(options.PageSize),
                Landscape = options.Landscape,
                PrintBackground = true,
                PreferCSSPageSize = string.IsNullOrWhiteSpace(options.Margin)
            };

            if (!string.IsNullOrWhiteSpace(options.Margin))
            {
                pdfOptions.MarginOptions = new MarginOptions
                {
                    Top = options.Margin,
                    Bottom = options.Margin,
                    Left = options.Margin,
                    Right = options.Margin
                };
            }

            return await page.PdfDataAsync(pdfOptions);
        }

        private static PaperFormat ParsePaperFormat(string size)
        {
            return (size ?? "A4").Trim().ToUpperInvariant() switch
            {
                "A3" => PaperFormat.A3,
                "A5" => PaperFormat.A5,
                "LETTER" => PaperFormat.Letter,
                "LEGAL" => PaperFormat.Legal,
                _ => PaperFormat.A4
            };
        }

        private async Task EnsureBrowserAsync()
        {
            if (_browserDownloaded) return;

            await _browserInitLock.WaitAsync();
            try
            {
                if (_browserDownloaded) return;

                var browserFetcher = new BrowserFetcher();
                _logger.LogInformation("Verificando/Descargando Chromium para PuppeteerSharp…");
                await browserFetcher.DownloadAsync();
                _browserDownloaded = true;
                _logger.LogInformation("Chromium listo.");
            }
            finally
            {
                _browserInitLock.Release();
            }
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "documento";
            var bad = Path.GetInvalidFileNameChars();
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var ch in s)
                sb.Append(System.Array.IndexOf(bad, ch) >= 0 ? '_' : ch);
            return sb.ToString();
        }

        internal byte[]? TryReadImageBytes(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            if (path.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return null;
            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) return null;
            if (path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return null;
            if (path.StartsWith("file://", StringComparison.OrdinalIgnoreCase)) return null;

            try
            {
                var trimmed = path.TrimStart('~').TrimStart('/', '\\');
                string full;

                if (trimmed.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
                {
                    full = Path.Combine(_env.ContentRootPath, "Templates", "Documents",
                        trimmed.Replace('/', Path.DirectorySeparatorChar));
                }
                else
                {
                    var webRoot = string.IsNullOrEmpty(_env.WebRootPath)
                        ? Path.Combine(_env.ContentRootPath, "wwwroot")
                        : _env.WebRootPath;
                    full = Path.Combine(webRoot, trimmed.Replace('/', Path.DirectorySeparatorChar));
                }

                return File.Exists(full) ? File.ReadAllBytes(full) : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
