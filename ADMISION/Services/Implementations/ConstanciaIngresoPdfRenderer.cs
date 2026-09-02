using ADMISION.ENTITIES.Data;
using ADMISION.Services.Interfaces;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SixLabors.ImageSharp;
using System.Globalization;

namespace ADMISION.Services.Implementations
{
    public class ConstanciaIngresoPdfRenderer : IConstanciaIngresoPdfRenderer
    {
        private const string TitleFontFamilyName = "Franklin Gothic";
        private const string BodyFontFamilyName = "SQR721B";
        private const string Squere = "SQR721B_";

        private readonly IWebHostEnvironment _env;
        private static readonly CultureInfo _esPE = new("es-PE");

        private static readonly object _fontLock = new();
        private static bool _fontRegistered;

        private const string TextDark  = "#111827";
        private const string TextMid   = "#4b5563";
        private const string TextLight = "#6b7280";
        private const string PrimaryColor = "#f54477";
        public ConstanciaIngresoPdfRenderer(IWebHostEnvironment env)
        {
            _env = env;
            EnsureFontRegistered();
        }

        private void EnsureFontRegistered()
        {
            if (_fontRegistered) return;
            lock (_fontLock)
            {
                if (_fontRegistered) return;

                try
                {
                    var fontsPath = Path.Combine(
                        _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"),
                        "fonts");

                    RegisterFont(TitleFontFamilyName, Path.Combine(fontsPath, "Franklin_Gothic_Heavy_Regular.ttf"));
                    RegisterFont(BodyFontFamilyName, Path.Combine(fontsPath, "SQR721B.TTF"));
                    RegisterFont(Squere, Path.Combine(fontsPath, "SQR721B_.TTF"));
                }
                catch
                {
                    // Si la fuente no se puede cargar, se usa la fuente por defecto.
                }

                _fontRegistered = true;
            }
        }

        private static void RegisterFont(string familyName, string fontPath)
        {
            if (!File.Exists(fontPath)) return;
            using var fontStream = File.OpenRead(fontPath);
            FontManager.RegisterFontWithCustomName(familyName, fontStream);
        }

        public byte[] Render(ConstanciaIngresoModel m)
        {
            //var backgroundPath = Path.Combine(
            //    _env.WebRootPath,
            //    "img",
            //    "ingreso.png");

            //byte[]? backgroundBytes = null;

            //if (File.Exists(backgroundPath))
            //    backgroundBytes = File.ReadAllBytes(backgroundPath);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(13, Unit.Millimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(t => t.FontFamily(BodyFontFamilyName).FontSize(11).FontColor(TextDark));

                    //page.Background().Layers(layers =>
                    //{
                    //    if (backgroundBytes != null)
                    //    {
                    //        layers.PrimaryLayer()
                    //            .Image(backgroundBytes)
                    //            .FitArea();
                    //    }
                    //});

                    page.Header().Column(col =>
                    {

                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Layers(layers =>
                        {
                            // Texto principal
                            layers.PrimaryLayer()
                                .PaddingTop(80)
                                .AlignCenter()
                                .Text("CONSTANCIA DE INGRESO")
                                .FontFamily(TitleFontFamilyName)
                                .Bold()
                                .FontSize(43)
                                .FontColor(TextDark);
                        });

                        col.Item().PaddingTop(20).Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontFamily(BodyFontFamilyName).FontSize(15).FontColor(TextDark).LineHeight(1.6f));
                            t.Span("La Dirección de Admisión de la ");
                            t.Span(m.InstitutionName);
                            t.Span(" deja constancia que:");
                        });

                        col.Item().PaddingTop(18).AlignCenter().Text(m.FullName)
                            .FontFamily(Squere).FontSize(22).Bold().FontColor(TextDark);

                        col.Item()
                            .PaddingTop(10)
                            .Text(t =>
                            {
                                t.Justify();

                                t.DefaultTextStyle(s => s
                                    .FontFamily(BodyFontFamilyName)
                                    .FontSize(15)
                                    .FontColor(TextDark)
                                    .LineHeight(1.6f));

                                t.Span("Identificado(a) con ");
                                t.Span(m.DocumentType);
                                t.Span(" N° ");
                                t.Span(m.DocumentNumber);
                                t.Span(" y código universitario ");
                                t.Span(m.PostulantCode).Bold().FontFamily(Squere).FontSize(18);
                                t.Span(", ingresó a la ");
                                t.Span(m.InstitutionName);
                                t.Span(" ocupando una vacante para la carrera profesional de:");
                            });

                        col.Item().PaddingTop(14).AlignCenter().Text(m.CareerName)
                            .FontFamily(Squere).FontSize(23).Bold().FontColor(TextDark);

                        col.Item().PaddingTop(10).Text(t =>
                        {
                            t.Justify();
                            t.DefaultTextStyle(s => s.FontFamily(BodyFontFamilyName).FontSize(15).FontColor(TextDark).LineHeight(1.6f));
                            t.Span("Por la modalidad ");
                            t.Span(m.ModalityName).Bold().FontFamily(Squere).FontSize(21);
                            t.Span(" en el semestre académico ");
                            t.Span(m.TermName).Bold().FontFamily(Squere).FontSize(19);
                            t.Span(".");
                        });

                        col.Item().PaddingTop(14).Text("Se expide la presente constancia para los fines correspondientes.")
                            .FontSize(15).FontColor(TextDark).LineHeight(1.5f);

                        col.Item().PaddingTop(15).AlignRight().Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontFamily(BodyFontFamilyName).FontSize(15).FontColor(TextDark));
                            t.Span("Puerto Maldonado, ");
                            t.Span(m.IssuedAt.ToString("dd 'de' MMMM 'de' yyyy", _esPE));
                        });

                    });

                    page.Footer().PaddingTop(5).Column(col =>
                    {
                        // Línea doble decorativa
                        col.Item().LineHorizontal(1.2f).LineColor(TextDark);
                        col.Item().PaddingTop(1).LineHorizontal(0.4f).LineColor(TextDark);

                        col.Item().PaddingTop(6).AlignCenter().Text(
                            m.FooterAddress ?? "Jr. Jorge Chávez N° 1160 – Ciudad Universitaria – Auditorio Principal – Interior Cel: 993 170 418")
                            .FontSize(9f)
                            .FontColor(TextDark);

                        col.Item().AlignCenter().Text("MADRE DE DIOS – PUERTO MALDONADO")
                            .FontSize(9f)
                            .FontColor(TextDark);
                    });
                });
            }).GeneratePdf();
        }
    }
}
