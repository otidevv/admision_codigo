using System.Globalization;
using ADMISION.ENTITIES.Data;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ADMISION.Services.Implementations
{
    /// <summary>
    /// Genera la Constancia de Inscripción en PDF usando QuestPDF (nativo .NET).
    /// Se prefiere QuestPDF sobre Scriban + PuppeteerSharp porque:
    ///   - No lanza un proceso Chromium (~200MB RAM cada uno).
    ///   - 10–100× más rápido por documento.
    ///   - Thread-safe: soporta múltiples generaciones concurrentes sin pool.
    /// Los documentos administrativos esporádicos (oficios, constancias de
    /// ingreso) siguen usando DocumentService con plantillas HTML; este servicio
    /// es para el flujo de alto volumen del portal público.
    /// </summary>
    public class InscriptionDocumentService : IInscriptionDocumentService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly IConfigService _configService;
        private static readonly CultureInfo _esPE = new("es-PE");

        // Colores institucionales (UNAMAD).
        private static readonly string PrimaryColor   = "#f54477";
        private static readonly string PrimaryDeep    = "#d10e49";
        private static readonly string SecondaryColor = "#716aca";
        private static readonly string TextDark       = "#111827";
        private static readonly string TextMid        = "#4b5563";
        private static readonly string TextLight      = "#6b7280";
        private static readonly string BorderColor    = "#e5e7eb";
        private static readonly string BgSoft         = "#fafafa";

        public InscriptionDocumentService(
            AppDbContext context,
            IWebHostEnvironment env,
            IConfigService configService,
            IConfiguration configuration)
        {
            _context = context;
            _env = env;
            _configService = configService;
            _configuration = configuration;
        }

        public async Task<DocumentResult?> BuildConstanciaAsync(Guid inscriptionId, string? verificationBaseUrl, bool onlyIfMockExam = false, CancellationToken ct = default)
        {
            var inscription = await _context.Inscriptions.AsNoTracking()
                .Include(i => i.Postulant!).ThenInclude(p => p!.User)
                .Include(i => i.Career!).ThenInclude(c => c!.Faculty)
                .Include(i => i.Modality!).ThenInclude(m => m!.Term)
                .Include(i => i.TypeModality)
                .FirstOrDefaultAsync(i => i.Id == inscriptionId, ct);
            if (inscription == null) return null;
            if (onlyIfMockExam && inscription.Modality is { IsMockExam: false }) return null;

            string? tematicAreaCode = null;
            if (inscription.Modality?.TermId != null)
            {
                tematicAreaCode = await (
                    from tac in _context.TematicAreaCareers.AsNoTracking()
                    join ta in _context.TematicAreas.AsNoTracking() on tac.TematicAreaId equals ta.Id
                    where tac.TermId == inscription.Modality.TermId && tac.CareerId == inscription.CareerId
                    select ta.Code
                ).FirstOrDefaultAsync(ct);
            }

            byte[]? photoBytes = null;
            if (inscription.PostulantId != Guid.Empty)
            {
                var photoUrl = await _context.PostulantPhotos.AsNoTracking()
                    .Where(p => p.PostulantId == inscription.PostulantId)
                    .OrderByDescending(p => p.IsPrimary)
                    .ThenByDescending(p => p.CreatedAt)
                    .Select(p => p.PhotoUrl)
                    .FirstOrDefaultAsync(ct);
                photoBytes = TryReadImage(photoUrl);
            }

            var directorCommissionName = await _configService.GetConfigValueAsync(ADMISION.ENTITIES.Constants.ConfigGeneral.DirecctorComision);

            // QR contiene el código de postulante para escaneo de asistencia
            var qrBytes = GenerateQrPng(inscription.CodePostulant);

            var user = inscription.Postulant?.User;
            var textInfo = CultureInfo.GetCultureInfo("es-PE").TextInfo;

            var nombres = textInfo.ToTitleCase((user?.Name ?? string.Empty).Trim().ToLowerInvariant());
            var apellidoPaterno = (user?.FirstNameFather ?? string.Empty).Trim().ToUpperInvariant();
            var apellidoMaterno = (user?.FirstNameMother ?? string.Empty).Trim().ToUpperInvariant();

            var fullName = string.Join(" ", new[]
            {
                apellidoPaterno,
                apellidoMaterno
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

            if (!string.IsNullOrWhiteSpace(nombres))
            {
                fullName += $", {nombres}";
            }

            var model = new ConstanciaModel
            {
                FullName = fullName.ToUpperInvariant(),
                DocumentType = user?.DocumentType ?? "DNI",
                DocumentNumber = user?.Document ?? "",
                BirthDate = user?.Birthdate.LocalDateTime.ToString("dd/MM/yyyy"),
                Email = user?.Email,
                Phone = user?.PhoneNumber,
                PostulantCode = inscription.CodePostulant,
                CareerName = inscription.Career?.Name ?? "",
                FacultyName = inscription.Career?.Faculty?.Name,
                TematicAreaCode = tematicAreaCode,
                ModalityName = inscription.Modality?.Name ?? "",
                TypeModalityName = inscription.TypeModality?.Name,
                TermName = inscription.Modality?.Term != null ? $"{inscription.Modality.Term.Name}".Trim() : "",
                InscriptionDate = inscription.CreatedAt.LocalDateTime,
                ExamDate = inscription.Modality?.ExamDate?.ToString("dd 'de' MMMM 'de' yyyy", _esPE),
                PhotoBytes = photoBytes,
                QrBytes = qrBytes,
                InstitutionName = "Universidad Nacional Amazónica de Madre de Dios",
                DirectorCommissionName = directorCommissionName
            };

            // ── 3. Generar PDF ───────────────────────────────────────────────
            var pdfBytes = inscription.Modality.IsMockExam
                ? BuildPdf(model)
                : BuildOrdinalPdf(model);

            return new DocumentResult
            {
                PdfBytes = pdfBytes,
                FileName = $"Constancia_{Sanitize(inscription.CodePostulant)}.pdf"
            };
        }

        public async Task<InscriptionVerificationDto?> GetVerificationAsync(string codePostulant, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(codePostulant)) return null;
            var code = codePostulant.Trim();

            var inscription = await _context.Inscriptions.AsNoTracking()
                .Include(i => i.Postulant!).ThenInclude(p => p!.User)
                .Include(i => i.Career)
                .Include(i => i.Modality!).ThenInclude(m => m!.Term)
                .Include(i => i.TypeModality)
                .FirstOrDefaultAsync(i => i.CodePostulant == code, ct);
            if (inscription == null) return null;

            string? tematicAreaCode = null;
            if (inscription.Modality?.TermId != null)
            {
                tematicAreaCode = await (
                    from tac in _context.TematicAreaCareers.AsNoTracking()
                    join ta in _context.TematicAreas.AsNoTracking() on tac.TematicAreaId equals ta.Id
                    where tac.TermId == inscription.Modality.TermId && tac.CareerId == inscription.CareerId
                    select ta.Code
                ).FirstOrDefaultAsync(ct);
            }

            var user = inscription.Postulant?.User;
            return new InscriptionVerificationDto
            {
                Found = true,
                CodePostulant = inscription.CodePostulant,
                FullName = user?.FullName ?? "",
                DocumentNumber = user?.Document ?? "",
                CareerName = inscription.Career?.Name ?? "",
                ModalityName = inscription.Modality?.Name ?? "",
                TypeModalityName = inscription.TypeModality?.Name,
                TermName = inscription.Modality?.Term != null
                    ? $"{inscription.Modality.Term.Name} {inscription.Modality.Term.Year}".Trim()
                    : "",
                TematicAreaCode = tematicAreaCode,
                State = inscription.State,
                InscriptionDate = inscription.CreatedAt
            };
        }
        //plantilla para simulacro de examen
        private byte[] BuildPdf(ConstanciaModel m)
        {
            const string Red = "#C8102E";
            const string RedLight = "#f4d0d7";
            const string TextDark = "#0d0d0d";
            const string TextMid = "#333333";
            const string TextLight = "#666666";
            const string BorderGray = "#cccccc";

            var backgroundPath = Path.Combine(
                _env.WebRootPath,
                "img",
                "simulacro_bg.png");

            byte[]? backgroundBytes = null;

            if (File.Exists(backgroundPath))
                backgroundBytes = File.ReadAllBytes(backgroundPath);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginTop(1, Unit.Millimetre);
                    page.MarginBottom(15, Unit.Millimetre);
                    page.MarginLeft(13, Unit.Millimetre);
                    page.MarginRight(13, Unit.Millimetre);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(10).FontColor(TextDark));

                    page.Background().Layers(layers =>
                    {
                        if (backgroundBytes != null)
                        {
                            layers.PrimaryLayer()
                                .Image(backgroundBytes)
                                .FitArea();
                        }
                    });


                    // ── CONTENT ───────────────────────────────────────────────────
                    page.Content().PaddingVertical(4).Column(col =>
                    {
                        col.Item().Height(70);
                        // Título principal
                        col.Item().PaddingTop(0).AlignCenter()
                            .Text("CONSTANCIA DE INSCRIPCIÓN")
                            .FontSize(30).Bold().FontColor(TextDark).FontFamily("Arial");

                        col.Item().PaddingTop(0).AlignCenter()
                            .Text(string.IsNullOrEmpty(m.TypeModalityName)
                                ? $"{m.ModalityName}"
                                : $"{m.ModalityName}")
                            .FontSize(20)
                            .FontFamily("Impact");


                        // ── Código postulante + TEMA + Foto ───────────────────────
                        col.Item().PaddingTop(6).Row(r =>
                        {
                            // Lado izquierdo: código + tema
                            r.RelativeItem().Column(c =>
                            {
                                c.Item()
                                    .AlignCenter()
                                    .Text(text =>
                                    {
                                        text.Span("CÓDIGO DE POSTULANTE: ")
                                            .FontSize(20)
                                            .Bold()
                                            .FontColor(TextDark)
                                            .FontFamily("Impact");

                                        text.Span(m.PostulantCode)
                                            .FontSize(32)
                                            .Bold()
                                            .FontFamily("Impact");
                                    });

                                c.Item().PaddingTop(2)
                                    .AlignCenter()
                                    .Text($"TEMA: {m.TematicAreaCode}")
                                    .FontSize(30)
                                    .Bold()
                                    .FontColor(TextDark)
                                    .FontFamily("Impact");
                            });

                            // Foto del postulante
                            r.ConstantItem(100).Column(c =>
                            {
                                var photoCell = c.Item()
                                                .Height(100)
                                                .Width(80);
                                if (m.PhotoBytes != null)
                                    photoCell.AlignCenter().AlignMiddle().Image(m.PhotoBytes).FitArea();
                                else
                                    photoCell.AlignCenter().AlignMiddle()
                                        .Text("FOTO").FontSize(9).FontColor(TextLight).AlignCenter();
                            });
                        });


                        // ── I. Datos del postulante y evaluación ─────────────────
                        col.Item().PaddingTop(-8)
                            .Text("DATOS DE POSTULANTE Y EVALUACIÓN:")
                            .FontSize(12).Bold().FontColor(TextDark).FontFamily("Arial");

                        col.Item().PaddingTop(4).Row(r =>
                        {
                            // Tabla de datos
                            r.RelativeItem().Column(c =>
                            {
                                DataRow(c, "APELLIDOS Y NOMBRES:", m.FullName, "#000", true);
                                DataRow(c, $"{m.DocumentType} / C.E.:", m.DocumentNumber, "#000", true);
                                DataRow(c, "CARRERA PROFESIONAL:", m.CareerName, "#000", true);
                                DataRow(c, "LUGAR DE EVALUACIÓN:", "Ciudad Universitaria UNAMAD, Puerta principal ", "#000", false);
                                DataRow(c, "FECHA DE EXAMEN:", m.ExamDate ?? "", "#000", true);
                                DataRow(c, "HORARIO DE INGRESO:", "07:00 a.m. a 08:50 a.m.", "#000", false);

                                c.Item().PaddingTop(4)
                                    .Text("NOTA: La asignación de pabellón y aula se realizará mediante sorteo interno.")
                                    .FontSize(8.5f).Italic().FontColor(TextLight);
                            });

                            r.ConstantItem(10);

                            // QR
                            r.ConstantItem(85).Column(c =>
                            {
                                var qrCell = c.Item()
                                    .Height(85)
                                    .Width(85);
                                if (m.QrBytes != null)
                                    qrCell.Image(m.QrBytes).FitArea();
                                else
                                    qrCell.AlignCenter().AlignMiddle()
                                        .Text("QR").FontSize(8).FontColor(TextLight).AlignCenter();
                            });
                        });


                        // ── II. Protocolo de seguridad ────────────────────────────
                        col.Item().PaddingTop(6)
                            .Text("PROTOCOLO DE SEGURIDAD:")
                            .FontSize(12).Bold().FontColor(TextDark).FontFamily("Arial");

                        col.Item().PaddingTop(4).Column(c =>
                        {
                            ProtocolItem(c, "IDENTIFICACIÓN:", "Presentar esta Constancia impresa y DNI original vigente.", TextMid);
                            ProtocolItem(c, "ELEMENTOS PERMITIDOS:", "Lápiz 2B, borrador y tajador únicamente (Solo Simulacro).", TextMid);
                            ProtocolItem(c, "PROHIBICIONES:", "Teléfono móvil, reloj de cualquier tipo, audífono, mochila, cartera. La sola tenencia, anula el examen.", TextMid);
                            ProtocolItem(c, "VESTIMENTA:", "Polo básico (cuello redondo, manga corta), cabello recogido.", TextMid);
                        });


                        // ── III. Declaración jurada ───────────────────────────────
                        col.Item().PaddingTop(6)
                            .Text("DECLARACIÓN JURADA:")
                            .FontSize(12).Bold().FontColor(TextDark).FontFamily("Arial");

                        col.Item().PaddingTop(4).Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontSize(10).FontColor(TextMid).LineHeight(1.4f));
                            t.Span("Mediante mi firma y huella dactilar, ");
                            t.Span("DECLARO BAJO JURAMENTO").Bold().FontColor(TextDark);
                            t.Span(" que:");
                        });

                        col.Item().PaddingTop(3).Column(c =>
                        {
                            DeclItem(c, "1.", "Conozco y me someto", " irrestrictamente a las normas, protocolos y sanciones del Reglamento General de Admisión de la UNAMAD.");
                            DeclItem(c, "2.", "Autorizo el uso institucional de mis datos biométricos", " (huella/foto) para identificación institucional (conforme a la Ley N° 29733 de Protección de Datos Personales).");
                            DeclItem(c, "3.", "Acepto la anulación inmediata", " de mi examen y la Nulidad de Oficio de mi ingreso si soy sorprendido portando aparatos electrónicos, cometiendo fraude o suplantación y asumo las denuncias penales ante el Ministerio Público.");
                            DeclItem(c, "4.", "Reconozco que no procede devolución de dinero", " ni reserva de pago por concepto de inscripción bajo ninguna circunstancia.");
                        });

                        // Ciudad y fecha
                        col.Item().PaddingTop(5).AlignRight()
                            .Text($"Puerto Maldonado, {m.InscriptionDate:dd 'de' MMMM yyyy}")
                            .FontSize(10);

                        // ── Firmas ────────────────────────────────────────────────
                        col.Item().PaddingTop(20).AlignCenter().Row(r =>
                        {
                            // Bloque firma
                            r.ConstantItem(240).Column(c =>
                            {
                                c.Item().Height(40);

                                c.Item()
                                    .LineHorizontal(1f)
                                    .LineColor(TextDark);

                                c.Item()
                                    .PaddingTop(3)
                                    .AlignCenter()
                                    .Text(m.FullName)
                                    .FontSize(9.5f)
                                    .FontColor(TextDark)
                                    .FontFamily("Arial");

                                c.Item()
                                    .AlignCenter()
                                    .Text("(Firma)")
                                    .FontSize(8.5f)
                                    .Italic()
                                    .FontColor(TextLight);
                            });

                            // Separación entre firma y huella
                            r.ConstantItem(25);

                            // Huella
                            r.ConstantItem(70).Column(c =>
                            {
                                c.Item()
                                    .AlignCenter()
                                    .Height(70)
                                    .Width(70);

                                c.Item()
                                    .PaddingTop(3)
                                    .AlignCenter()
                                    .Text("")
                                    .FontSize(7f)
                                    .Italic()
                                    .FontColor(TextLight);
                            });
                        });

                        col.Item().PaddingTop(5).Width(400).Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontSize(9).FontColor(TextMid).Italic().LineHeight(1.4f));
                            t.Span("NOTA: ").Bold().FontColor(TextDark);
                            t.Span("Este documento es personal e intransferible. Debe conservarse en buen estado y ");
                            t.Span("presentarse obligatoriamente el día del examen junto con su DNI original o C4.");
                        });
                    });

                });
            }).GeneratePdf();
        }

        //plantilla para examen de admisión y otras modalidades

        private byte[] BuildOrdinalPdf(ConstanciaModel m)
        {
            const string Red = "#C8102E";
            const string RedLight = "#f4d0d7";
            const string TextDark = "#0d0d0d";
            const string TextMid = "#333333";
            const string TextLight = "#666666";
            const string BorderGray = "#cccccc";

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginTop(1, Unit.Millimetre);
                    page.MarginBottom(15, Unit.Millimetre);
                    page.MarginLeft(13, Unit.Millimetre);
                    page.MarginRight(13, Unit.Millimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(11).FontColor(TextDark));

                    // ── CONTENT ───────────────────────────────────────────────────
                    page.Content().PaddingVertical(2).Column(col =>
                    {
                        col.Item().Height(75);
                        // Título principal
                        col.Item().PaddingTop(0).AlignCenter()
                            .Text("CONSTANCIA DE INSCRIPCIÓN")
                            .FontSize(30).Bold().FontColor(TextDark).FontFamily("Arial");

                        col.Item().PaddingTop(1).AlignCenter()
                            .Text(string.IsNullOrEmpty(m.TypeModalityName)
                                ? $"{m.ModalityName}"
                                : $"{m.ModalityName}")
                            .FontSize(20)
                            .FontFamily("Impact");


                        // ── Código postulante + TEMA + Foto ───────────────────────
                        col.Item().PaddingTop(6).Row(r =>
                        {
                            // Lado izquierdo: código + tema
                            r.RelativeItem().Column(c =>
                            {
                                c.Item()
                                    .AlignCenter()
                                    .Text(text =>
                                    {
                                        text.Span("CÓDIGO DE POSTULANTE: ")
                                            .FontSize(20)
                                            .Bold()
                                            .FontColor(TextDark)
                                            .FontFamily("Impact");

                                        text.Span(m.PostulantCode)
                                            .FontSize(32)
                                            .Bold()
                                            .FontFamily("Impact");
                                    });

                                c.Item().PaddingTop(2)
                                    .AlignCenter()
                                    .Text($"TEMA: {m.TematicAreaCode}")
                                    .FontSize(30)
                                    .Bold()
                                    .FontColor(TextDark)
                                    .FontFamily("Arial");
                            });

                            // Foto del postulante
                            r.ConstantItem(100).Column(c =>
                            {
                                var photoCell = c.Item()
                                                .Height(100)
                                                .Width(80);
                                if (m.PhotoBytes != null)
                                    photoCell.AlignCenter().AlignMiddle().Image(m.PhotoBytes).FitArea();
                                else
                                    photoCell.AlignCenter().AlignMiddle()
                                        .Text("FOTO").FontSize(9).FontColor(TextLight).AlignCenter();
                            });
                        });


                        // ── I. Datos del postulante y evaluación ─────────────────
                        col.Item().PaddingTop(-8)
                            .Text("DATOS DE POSTULANTE Y EVALUACIÓN:")
                            .FontSize(12).Bold().FontColor(TextDark).FontFamily("Arial");

                        col.Item().PaddingTop(4).Row(r =>
                        {
                            // Tabla de datos
                            r.RelativeItem().Column(c =>
                            {
                                DataRow(c, "APELLIDOS Y NOMBRES:", m.FullName, "#000", true);
                                DataRow(c, $"{m.DocumentType} / C.E.:", m.DocumentNumber, "#000", true);
                                DataRow(c, "CARRERA PROFESIONAL:", m.CareerName, "#000", true);
                                DataRow(c, "MODALIDAD:", m.TypeModalityName ?? "-", "#000", true);
                                DataRow(c, "LUGAR DE EVALUACIÓN:", "Ciudad Universitaria UNAMAD, Puerta principal ", "#000", false);
                                DataRow(c, "FECHA DE EXAMEN:", m.ExamDate ?? "", "#000", true);
                                DataRow(c, "HORARIO DE INGRESO:", "07:00 a.m. a 08:50 a.m.", "#000", false);

                                c.Item().PaddingTop(4)
                                    .Text("NOTA: La asignación de pabellón y aula se realizará mediante sorteo interno.")
                                    .FontSize(8.5f).Italic().FontColor(TextLight);
                            });

                            r.ConstantItem(10);

                            // QR
                            r.ConstantItem(85).Column(c =>
                            {
                                var qrCell = c.Item()
                                    .Height(85)
                                    .Width(85);
                                if (m.QrBytes != null)
                                    qrCell.Image(m.QrBytes).FitArea();
                                else
                                    qrCell.AlignCenter().AlignMiddle()
                                        .Text("QR").FontSize(8).FontColor(TextLight).AlignCenter();
                            });
                        });


                        // ── II. Protocolo de seguridad ────────────────────────────
                        col.Item().PaddingTop(6)
                            .Text("PROTOCOLO DE SEGURIDAD:")
                            .FontSize(12).Bold().FontColor(TextDark).FontFamily("Arial");

                        col.Item().PaddingTop(4).Column(c =>
                        {
                            ProtocolItem(c, "IDENTIFICACIÓN:", "Presentar esta constancia impresa y DNI original vigente o C4.", TextMid);
                            ProtocolItem(c, "PROHIBICIONES:", "Lápices, borradores, tajadores, teléfonos móviles, relojes, audífonos, mochilas, carteras u otros objetos que no se mencionen en el primer punto. La sola tenencia anula el examen.", TextMid);
                            ProtocolItem(c, "VESTIMENTA:", "Polo básico (cuello redondo, manga corta), cabello recogido  pantalon simple.", TextMid);
                        });


                        // ── III. Declaración jurada ───────────────────────────────
                        col.Item().PaddingTop(6)
                            .Text("DECLARACIÓN JURADA:")
                            .FontSize(12).Bold().FontColor(TextDark).FontFamily("Arial");

                        col.Item().PaddingTop(4).Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontSize(10).FontColor(TextMid).LineHeight(1.4f));
                            t.Span("Mediante mi firma y huella dactilar, ");
                            t.Span("DECLARO BAJO JURAMENTO").Bold().FontColor(TextDark);
                            t.Span(" que:");
                        });

                        col.Item().PaddingTop(3).Column(c =>
                        {
                            DeclItem(c, "1.", "Conozco y me someto", " irrestrictamente a las normas, protocolos y sanciones del Reglamento General de Admisión de la UNAMAD.");
                            DeclItem(c, "2.", "Autorizo el uso institucional de mis datos biométricos", " (huella/foto) para identificación institucional (conforme a la Ley N° 29733 de Protección de Datos Personales).");
                            DeclItem(c, "3.", "Acepto la anulación inmediata", " de mi examen y la Nulidad de Oficio de mi ingreso si soy sorprendido portando aparatos electrónicos, cometiendo fraude o suplantación y asumo las denuncias penales ante el Ministerio Público.");
                            DeclItem(c, "4.", "Reconozco que no procede devolución de dinero", " ni reserva de pago por concepto de inscripción bajo ninguna circunstancia.");
                        });

                        col.Item().PaddingTop(5).Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontSize(9).FontColor(TextMid).Italic().LineHeight(1.4f));
                            t.Span("NOTA: ").Bold().FontColor(TextDark);
                            t.Span("Este documento es personal e intransferible. Debe conservarse en buen estado y presentarse obligatoriamente el día del examen junto con su DNI original o C4.");
                        });
                        // Ciudad y fecha
                        col.Item().PaddingTop(5).AlignRight()
                            .Text($"Puerto Maldonado, {m.InscriptionDate:dd 'de' MMMM yyyy}")
                            .FontSize(9);

                        // ── Firmas ────────────────────────────────────────────────
                        col.Item().PaddingTop(20).AlignCenter().Row(r =>
                        {
                            // Bloque firma
                            r.ConstantItem(240).AlignRight().Width(130).Column(c =>
                            {
                                c.Item().Height(40);

                                c.Item()
                                    .LineHorizontal(1f)
                                    .LineColor(TextDark);

                                c.Item()
                                    .PaddingTop(3)
                                    .AlignCenter()
                                    .Text(m.FullName)
                                    .FontSize(9.5f)
                                    .FontColor(TextDark)
                                    .FontFamily("Arial");

                                c.Item()
                                    .AlignCenter()
                                    .Text("(Firma)")
                                    .FontSize(8.5f)
                                    .Italic()
                                    .FontColor(TextLight);
                            });

                            // Separación entre firma y huella
                            r.ConstantItem(25);

                            // Huella
                            r.ConstantItem(70).Column(c =>
                            {
                                c.Item()
                                    .AlignCenter()
                                    .Height(70)
                                    .Width(70)
                                    .Border(1)
                                    .BorderColor(BorderGray)
                                    .Background("#fff");

                                c.Item()
                                    .PaddingTop(3)
                                    .AlignCenter()
                                    .Text("(Huella dactilar)")
                                    .FontSize(7f)
                                    .Italic()
                                    .FontColor(TextLight);
                            });
                        });

                        
                    });

                });
            }).GeneratePdf();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────

        /// Fila de datos: "Label:    Valor" con valor en color opcional
        private static void DataRow(
            ColumnDescriptor col,
            string label,
            string value,
            string valueColor,
            bool valueBold)
        {
            col.Item().PaddingBottom(1).Row(r =>
            {
                r.ConstantItem(145).Text(label)
                    .FontSize(11).FontColor("#222222");
                r.RelativeItem().Text(value)
                    .FontSize(11).FontColor(valueColor);
            });
        }

        /// Ítem de protocolo: "• LABEL texto"
        private static void ProtocolItem(
            ColumnDescriptor col,
            string label,
            string text,
            string textColor)
        {
            col.Item().PaddingBottom(2).Row(r =>
            {
                r.ConstantItem(10).Text("•").Bold().FontColor("#000");
                r.RelativeItem().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(11).FontColor(textColor).LineHeight(1.1f));
                    t.Span(label).Bold().FontColor("#000");
                    t.Span($" {text}");
                });
            });
        }

        /// Ítem numerado de declaración jurada
        private static void DeclItem(
            ColumnDescriptor col,
            string number,
            string boldPart,
            string rest)
        {
            col.Item().PaddingBottom(2).Row(r =>
            {
                r.ConstantItem(18).Text(number).Bold().FontSize(11).FontColor("#0d0d0d");
                r.RelativeItem().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(11).FontColor("#333333").LineHeight(1.1f));
                    t.Span(boldPart).Bold().FontColor("#0d0d0d");
                    t.Span(rest);
                });
            });
        }

        private static void AddRow(ColumnDescriptor c, string label, string value)
        {
            c.Item().PaddingVertical(2).BorderBottom(0.5f).BorderColor(BorderColor).Row(r =>
            {
                r.RelativeItem(3).Text(label.ToUpperInvariant())
                    .FontSize(8).LetterSpacing(0.4f).FontColor(TextLight);
                r.RelativeItem(7).Text(value).FontSize(9).Bold().FontColor(TextDark);
            });
        }

        private static void Pill(IContainer container, string text, string bgColor, string borderColor, string textColor)
        {
            container.Background(bgColor).Border(1).BorderColor(borderColor)
                .PaddingVertical(2).PaddingHorizontal(7)
                .Text(text.ToUpperInvariant()).FontSize(7.5f).Bold().LetterSpacing(0.4f).FontColor(textColor);
        }

        private static string[] IndicationsList() => new[]
        {
            "Presentar este documento impreso el día del examen, acompañado del DNI original.",
            "Ingresar al local del examen con 1 hora de anticipación; no se permitirá el acceso después del horario.",
            "Prohibido el uso de celulares, relojes inteligentes, calculadoras y dispositivos electrónicos.",
            "Solo se permite lápiz 2B, borrador, tajador y DNI.",
            "La suplantación de identidad anula la inscripción.",
            "Verifica los datos; cualquier error debe reportarse antes del examen."
        };

        // ── Helpers ─────────────────────────────────────────────────────────
        private byte[]? TryReadImage(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var storageRoot = _configuration["FileUpload:BaseStoragePath"];

                // uploads/2026/photos/... -> 2026/photos/...
                if (path.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                {
                    path = path["uploads/".Length..];
                }

                var fullPath = Path.Combine(
                    storageRoot,
                    path.Replace('/', Path.DirectorySeparatorChar));

                return File.Exists(fullPath)
                    ? File.ReadAllBytes(fullPath)
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private static byte[] GenerateQrPng(string payload)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            return new PngByteQRCode(data).GetGraphic(10);
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "documento";
            var invalid = Path.GetInvalidFileNameChars();
            var safe = new string(s.Select(c => invalid.Contains(c) || c == ' ' ? '_' : c).ToArray());
            return safe.Length > 60 ? safe[..60] : safe;
        }

        // ── Modelo plano del documento ───────────────────────────────────────
        private class ConstanciaModel
        {
            public string FullName { get; set; } = "";
            public string DocumentType { get; set; } = "DNI";
            public string DocumentNumber { get; set; } = "";
            public string? BirthDate { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string PostulantCode { get; set; } = "";
            public string CareerName { get; set; } = "";
            public string? FacultyName { get; set; }
            public string? TematicAreaCode { get; set; }
            public string ModalityName { get; set; } = "";
            public string? TypeModalityName { get; set; }
            public string TermName { get; set; } = "";
            public DateTime InscriptionDate { get; set; }
            public string? ExamDate { get; set; }
            public byte[]? PhotoBytes { get; set; }
            public byte[] QrBytes { get; set; } = Array.Empty<byte>();
            public string? YearBanner { get; set; }
            public string InstitutionName { get; set; } = "";
            public string? Dependency { get; set; }
            public string? OfficeName { get; set; }
            public string? FooterAddress { get; set; }
            public string? FooterPhone { get; set; }
            public string? FooterEmail { get; set; }
            public string? FooterExtra { get; set; }
            public byte[]? LogoBytes { get; set; }
            public byte[]? SecondaryLogoBytes { get; set; }
            public string? DirectorCommissionName { get; set; }
        }
    }
}
