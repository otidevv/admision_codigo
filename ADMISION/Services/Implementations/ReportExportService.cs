using ADMISION.Models.ViewModels.Reports;
using ADMISION.Services.Interfaces;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ADMISION.Services.Implementations
{
    public class ReportExportService : IReportExportService
    {
        private readonly IWebHostEnvironment _env;

        public ReportExportService(IWebHostEnvironment env)
        {
            _env = env;
        }

        private byte[] LoadBackground()
        {
            var bgPath = Path.Combine(_env.WebRootPath, "img", "horizontal.png");
            return System.IO.File.Exists(bgPath)
                ? System.IO.File.ReadAllBytes(bgPath)
                : null!;
        }

        private static void ApplyPdfFooter(RowDescriptor row, DateTime now)
        {
            row.RelativeItem().AlignLeft().Text($"Impreso: {now:dd/MM/yyyy HH:mm}")
                .FontSize(7).FontColor("#64748b");
            row.RelativeItem().AlignCenter().Text("—");
            row.RelativeItem().AlignRight().Text(x =>
            {
                x.Span("Página ").FontSize(7).FontColor("#64748b");
                x.CurrentPageNumber().FontSize(7).FontColor("#64748b");
                x.Span(" de ").FontSize(7).FontColor("#64748b");
                x.TotalPages().FontSize(7).FontColor("#64748b");
            });
        }

        // ═══════════════════════════════════════════════════════
        // GENERAL — EXCEL
        // ═══════════════════════════════════════════════════════
        public byte[] BuildGeneralExcel(List<GeneralReportItem> items)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Reporte General");

            ws.Cell(1, 1).Value = "REPORTE GENERAL DE INSCRIPCIONES";
            ws.Range(1, 1, 1, 31).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(13)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = $"Fecha de impresión: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range(2, 1, 2, 31).Merge().Style
                .Font.SetFontSize(9).Font.SetFontColor(XLColor.FromHtml("#64748b"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

            int row = 4;
            var headers = new[]
            {
                "TIPO_EXAMEN", "MODALIDAD", "FECHA_INSCRIPCION", "CODIGO_POSTULANTE",
                "APELLIDOS Y NOMBRES", "DOCUMENTO", "SEXO", "FECHA_NACIMIENTO",
                "DIRECCION", "ESTADO_CIVIL", "TIENE_DISCAPACIDAD", "DISCAPACIDAD",
                "CORREO", "CELULAR", "CODIGO_CARRERA", "CARRERA_PROFESIONAL",
                "TEMA", "CICLO", "UBIGEO_COLEGIO", "NOMBRE_COLEGIO",
                "DISTRITO_COLEGIO", "PROVINCIA_COLEGIO", "DEPARTAMENTO_COLEGIO",
                "PAIS", "UBIGEO", "DISTRITO_PROCEDENCIA", "PROVINCIA_PROCEDENCIA",
                "DEPARTAMENTO_PROCEDENCIA"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#2563eb"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
            row++;

            foreach (var item in items)
            {
                var values = new[]
                {
                    item.TipoExamen, item.Modalidad, item.FechaInscripcion,
                    item.CodigoPostulante, $"{item.Apellidos}, {item.Nombres}",
                    item.Documento, item.Sexo, item.FechaNacimiento,
                    item.Direccion, item.EstadoCivil, item.TieneDiscapacidad,
                    item.Discapacidad, item.Correo, item.Celular,
                    item.CodigoCarrera, item.CarreraProfesional,
                    item.Tema, item.Ciclo, item.UbigeoColegio,
                    item.NombreColegio, item.DistritoColegio,
                    item.ProvinciaColegio, item.DepartamentoColegio,
                    item.Pais, item.Ubigeo, item.DistritoProcedencia,
                    item.ProvinciaProcedencia, item.DepartamentoProcedencia
                };

                for (int i = 0; i < values.Length; i++)
                {
                    ws.Cell(row, i + 1).Value = values[i] ?? "—";
                    ws.Cell(row, i + 1).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                    ws.Cell(row, i + 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
                }
                row++;
            }

            ws.Cell(row, 1).Value = $"TOTAL: {items.Count} registro(s)";
            ws.Range(row, 1, row, 5).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        // ═══════════════════════════════════════════════════════
        // GENERAL — PDF
        // ═══════════════════════════════════════════════════════
        public byte[] BuildGeneralPdf(List<GeneralReportItem> items)
        {
            var bgBytes = LoadBackground();
            var now = DateTime.Now;

            var headers = new[]
            {
                "TIPO_EXAMEN", "MODALIDAD", "FECHA_INSCRIPCION", "CODIGO_POSTULANTE",
                "APELLIDOS", "NOMBRES", "DOCUMENTO", "SEXO", "FECHA_NACIMIENTO",
                "DIRECCION", "ESTADO_CIVIL", "TIENE_DISCAPACIDAD", "DISCAPACIDAD",
                "CORREO", "CELULAR", "CODIGO_CARRERA", "CARRERA_PROFESIONAL",
                "TEMA", "CICLO", "UBIGEO_COLEGIO", "NOMBRE_COLEGIO",
                "DISTRITO_COLEGIO", "PROVINCIA_COLEGIO", "DEPARTAMENTO_COLEGIO",
                "PAIS", "UBIGEO", "DISTRITO_PROCEDENCIA", "PROVINCIA_PROCEDENCIA",
                "DEPARTAMENTO_PROCEDENCIA"
            };

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Portrait());
                    page.Margin(20, Unit.Millimetre);

                    if (bgBytes != null)
                    {
                        page.Background().Layers(layers =>
                        {
                            layers.PrimaryLayer().Image(bgBytes).FitArea();

                        });
                    }

                    page.DefaultTextStyle(x => x.FontSize(6.5f).FontFamily(Fonts.Calibri));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("REPORTE GENERAL DE INSCRIPCIONES")
                            .Bold().FontSize(12).FontColor("#1e3a8a");
                        col.Item().PaddingTop(2).Text($"Total: {items.Count} registro(s)")
                            .FontSize(8).FontColor("#64748b");
                        col.Item().LineHorizontal(1).LineColor("#1e3a8a");
                    });

                    page.Content().PaddingVertical(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(48); columns.ConstantColumn(42);
                            columns.ConstantColumn(45); columns.ConstantColumn(55);
                            columns.ConstantColumn(60); columns.ConstantColumn(55);
                            columns.ConstantColumn(45); columns.ConstantColumn(25);
                            columns.ConstantColumn(45); columns.ConstantColumn(60);
                            columns.ConstantColumn(35); columns.ConstantColumn(35);
                            columns.ConstantColumn(45); columns.ConstantColumn(60);
                            columns.ConstantColumn(38); columns.ConstantColumn(38);
                            columns.ConstantColumn(60); columns.ConstantColumn(20);
                            columns.ConstantColumn(40); columns.ConstantColumn(38);
                            columns.ConstantColumn(60); columns.ConstantColumn(50);
                            columns.ConstantColumn(50); columns.ConstantColumn(50);
                            columns.ConstantColumn(35); columns.ConstantColumn(38);
                            columns.ConstantColumn(50); columns.ConstantColumn(50);
                            columns.ConstantColumn(50);
                        });

                        table.Header(h =>
                        {
                            foreach (var header in headers)
                                h.Cell().Background("#2563eb").Padding(3)
                                    .Text(header).FontColor(Colors.White).Bold().FontSize(5.5f);
                        });

                        foreach (var item in items)
                        {
                            table.Cell().Padding(2).Text(item.TipoExamen).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.Modalidad).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.FechaInscripcion).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.CodigoPostulante).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.Apellidos).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.Nombres).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.Documento).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.Sexo).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.FechaNacimiento).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.Direccion).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.EstadoCivil).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.TieneDiscapacidad).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.Discapacidad).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.Correo).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.Celular).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.CodigoCarrera).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.CarreraProfesional).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.Tema).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.Ciclo).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.UbigeoColegio).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.NombreColegio).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.DistritoColegio).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.ProvinciaColegio).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.DepartamentoColegio).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.Pais).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.Ubigeo).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.DistritoProcedencia).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.ProvinciaProcedencia).FontSize(5.5f);
                            table.Cell().Padding(2).Text(item.DepartamentoProcedencia).FontSize(5.5f);
                        }
                    });

                    page.Footer().Row(row => ApplyPdfFooter(row, now));
                });
            }).GeneratePdf();
        }

        // ═══════════════════════════════════════════════════════
        // ECONÓMICO — EXCEL
        // ═══════════════════════════════════════════════════════
        public byte[] BuildEconomicoExcel(List<EconomicReportItem> items)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Reporte Económico");

            ws.Cell(1, 1).Value = "REPORTE ECONÓMICO";
            ws.Range(1, 1, 1, 9).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(13)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = $"Fecha de impresión: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range(2, 1, 2, 9).Merge().Style
                .Font.SetFontSize(9).Font.SetFontColor(XLColor.FromHtml("#64748b"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

            int row = 4;
            var headers = new[]
            {
                "CICLO", "CODIGO", "DNI", "APELLIDOS Y NOMBRES", "EXAMEN", "MODALIDAD", "TIPO_POSTULANTE", "DESCUENTO", "MONTO"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#2563eb"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
            row++;

            foreach (var item in items)
            {
                var values = new[]
                {
                    item.Ciclo, item.Codigo, item.Dni, $"{item.ApellidoPaterno} {item.ApellidoMaterno}, {item.Nombres}", item.Examen, item.Modalidad,
                    item.TipoPostulante, item.Descuento, item.Monto
                };

                for (int i = 0; i < values.Length; i++)
                {
                    ws.Cell(row, i + 1).Value = values[i] ?? "—";
                    ws.Cell(row, i + 1).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                    ws.Cell(row, i + 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
                }
                row++;
            }

            ws.Cell(row, 1).Value = $"TOTAL: {items.Count} registro(s)";
            ws.Range(row, 1, row, 5).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        // ═══════════════════════════════════════════════════════
        // ECONÓMICO — PDF
        // ═══════════════════════════════════════════════════════
        public byte[] BuildEconomicoPdf(List<EconomicReportItem> items)
        {
            var bgBytes = LoadBackground();
            var now = DateTime.Now;

            var headers = new[]
            {
                "CICLO", "CODIGO", "DNI", "APELLIDOS Y NOMBRES", "EXAMEN", "MODALIDAD", "TIPO_POSTULANTE", "DESCUENTO", "MONTO"
            };

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(15, Unit.Millimetre);

                    if (bgBytes != null)
                    {
                        page.Background().Layers(layers =>
                        {
                            layers.PrimaryLayer().Image(bgBytes).FitArea();

                        });
                    }

                    page.DefaultTextStyle(x => x.FontSize(7f).FontFamily(Fonts.Calibri));

                    page.Header().PaddingTop(25).Column(col =>
                    {
                        col.Item().Text("REPORTE ECONÓMICO")
                            .Bold().FontSize(12).FontColor("#1e3a8a");
                        col.Item().PaddingTop(2).Text($"Total: {items.Count} registro(s)")
                            .FontSize(8).FontColor("#64748b");
                        col.Item().LineHorizontal(1).LineColor("#1e3a8a");
                    });

                    page.Content().PaddingVertical(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(45);   // CICLO
                            columns.ConstantColumn(55);   // CODIGO
                            columns.ConstantColumn(55);   // DNI
                            columns.ConstantColumn(200);  // APELLIDOS Y NOMBRES
                            columns.ConstantColumn(96);   // EXAMEN
                            columns.ConstantColumn(100);   // MODALIDAD
                            columns.ConstantColumn(90);   // TIPO POSTULANTE
                            columns.ConstantColumn(60);   // DESCUENTO
                            columns.ConstantColumn(55);   // MONTO
                        });

                        table.Header(h =>
                        {
                            foreach (var header in headers)
                                h.Cell().Element(CellStyle).Background("#2563eb").Padding(3)
                                    .Text(header).FontColor(Colors.White).Bold().FontSize(9f);
                        });

                        foreach (var item in items)
                        {
                            table.Cell().Element(CellStyle).Padding(2).Text(item.Ciclo).FontSize(9f);
                            table.Cell().Element(CellStyle).Padding(2).Text(item.Codigo).FontSize(9f);
                            table.Cell().Element(CellStyle).Padding(2).Text(item.Dni).FontSize(9f);
                            table.Cell().Element(CellStyle).Padding(2).Text($"{item.ApellidoPaterno} {item.ApellidoMaterno}, {item.Nombres}").FontSize(9f);
                            table.Cell().Element(CellStyle).Padding(2).Text(item.Modalidad).FontSize(9f);
                            table.Cell().Element(CellStyle).Padding(2).Text(item.Examen).FontSize(9f);
                            table.Cell().Element(CellStyle).Padding(2).Text(item.TipoPostulante).FontSize(9f);
                            table.Cell().Element(CellStyle).Padding(2).Text(item.Descuento).FontSize(9f);
                            table.Cell().Element(CellStyle).Padding(2).Text(item.Monto).FontSize(9f);
                        }
                    });

                    page.Footer().Row(row => ApplyPdfFooter(row, now));
                });
            }).GeneratePdf();
        }

        // ═══════════════════════════════════════════════════════
        // VACANTES — EXCEL
        // ═══════════════════════════════════════════════════════
        public byte[] BuildVacantesExcel(VacantesReportViewModel vm)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Reporte Vacantes");

            var typeLabel = vm.ReportType?.ToLowerInvariant() switch
            {
                "postulantes" => "POSTULANTES",
                "ingresantes" => "INGRESANTES",
                "consolidado" => "INGRESANTES (CONSOLIDADO)",
                _ => "VACANTES"
            };

            var totalDataCols = vm.Columns.Count;

            ws.Cell(1, 1).Value = $"REPORTE DE {typeLabel}";
            ws.Range(1, 1, 1, totalDataCols + 2).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(13)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = $"Periodo: {vm.TermName ?? "—"} · Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range(2, 1, 2, totalDataCols + 2).Merge().Style
                .Font.SetFontSize(9).Font.SetFontColor(XLColor.FromHtml("#64748b"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

            int headerRow1 = 4;
            int headerRow2 = 5;
            int dataStartRow = 6;

            ws.Cell(headerRow1, 1).Value = "FACULTAD";
            ws.Range(headerRow1, 1, headerRow1, 1).Style
                .Font.SetBold(true).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#2563eb"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Range(headerRow1, 1, headerRow2, 1).Merge();

            ws.Cell(headerRow1, 2).Value = "CARRERA PROFESIONAL";
            ws.Range(headerRow1, 2, headerRow1, 2).Style
                .Font.SetBold(true).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#2563eb"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Range(headerRow1, 2, headerRow2, 2).Merge();

            int colIdx = 3;
            foreach (var group in vm.ModalityGroups)
            {
                if (group.HasSubHeaders)
                {
                    ws.Range(headerRow1, colIdx, headerRow1, colIdx + group.ColumnCount - 1).Merge().Style
                        .Font.SetBold(true).Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#2563eb"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                    var subCols = vm.Columns.Where(c => c.ModalityId == group.ModalityId).ToList();
                    for (int i = 0; i < subCols.Count; i++)
                    {
                        ws.Cell(headerRow2, colIdx + i).Value = subCols[i].Header;
                        ws.Cell(headerRow2, colIdx + i).Style
                            .Font.SetBold(true).Font.SetFontColor(XLColor.White)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#3b82f6"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                            .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                    }
                }
                else
                {
                    ws.Cell(headerRow1, colIdx).Value = group.ModalityName;
                    ws.Range(headerRow1, colIdx, headerRow1, colIdx).Style
                        .Font.SetBold(true).Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#2563eb"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                    ws.Range(headerRow1, colIdx, headerRow2, colIdx).Merge();
                }
                colIdx += group.ColumnCount;
            }

            ws.Cell(headerRow1, colIdx).Value = "TOTAL";
            ws.Range(headerRow1, colIdx, headerRow1, colIdx).Style
                .Font.SetBold(true).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Range(headerRow1, colIdx, headerRow2, colIdx).Merge();

            int currentRow = dataStartRow;
            foreach (var faculty in vm.Faculties)
            {
                for (int ci = 0; ci < faculty.Careers.Count; ci++)
                {
                    var career = faculty.Careers[ci];
                    var rowBg = ci % 2 == 0 ? XLColor.FromHtml("#f8fafc") : XLColor.White;

                    ws.Cell(currentRow, 1).Value = ci == 0 ? faculty.Name : "";
                    if (ci == 0)
                    {
                        ws.Range(currentRow, 1, currentRow + faculty.Careers.Count - 1, 1).Merge();
                        ws.Cell(currentRow, 1).Style
                            .Font.SetBold(true)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#eff6ff"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left)
                            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
                    }

                    ws.Cell(currentRow, 2).Value = career.CareerName;
                    ws.Cell(currentRow, 2).Style
                        .Fill.SetBackgroundColor(rowBg)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                    for (int vi = 0; vi < career.Values.Count; vi++)
                    {
                        ws.Cell(currentRow, 3 + vi).Value = career.Values[vi];
                        ws.Cell(currentRow, 3 + vi).Style
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                            .Fill.SetBackgroundColor(rowBg)
                            .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                    }

                    ws.Cell(currentRow, 3 + career.Values.Count).Value = career.Total;
                    ws.Cell(currentRow, 3 + career.Values.Count).Style
                        .Font.SetBold(true)
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                        .Fill.SetBackgroundColor(rowBg)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                    currentRow++;
                }
            }

            int totalRow = currentRow;
            ws.Range(totalRow, 1, totalRow, 2).Merge().Style
                .Font.SetBold(true).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Cell(totalRow, 1).Value = "TOTALES";

            for (int i = 0; i < vm.ColumnTotals.Count; i++)
            {
                ws.Cell(totalRow, 3 + i).Value = vm.ColumnTotals[i];
                ws.Cell(totalRow, 3 + i).Style
                    .Font.SetBold(true).Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            ws.Cell(totalRow, 3 + vm.ColumnTotals.Count).Value = vm.GrandTotal;
            ws.Cell(totalRow, 3 + vm.ColumnTotals.Count).Style
                .Font.SetBold(true).Font.SetFontColor(XLColor.White)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            ws.Columns().AdjustToContents();
            ws.Column(1).Width = 28;
            ws.Column(2).Width = 35;

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        // ═══════════════════════════════════════════════════════
        // VACANTES — PDF
        // ═══════════════════════════════════════════════════════
        public byte[] BuildVacantesPdf(VacantesReportViewModel vm)
        {
            var bgBytes = LoadBackground();
            var now = DateTime.Now;

            var typeLabel = vm.ReportType?.ToLowerInvariant() switch
            {
                "postulantes" => "POSTULANTES",
                "ingresantes" => "INGRESANTES",
                "consolidado" => "INGRESANTES (CONSOLIDADO)",
                _ => "VACANTES"
            };

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(12, Unit.Millimetre);

                    if (bgBytes != null)
                    {
                        page.Background().Layers(layers =>
                        {
                            layers.PrimaryLayer().Image(bgBytes).FitArea();

                        });
                    }

                    page.DefaultTextStyle(x => x.FontSize(6f).FontFamily(Fonts.Calibri));

                    page.Header().PaddingTop(35).Column(col =>
                    {
                        col.Item().Text($"REPORTE DE {typeLabel}")
                            .Bold().FontSize(11).FontColor("#1e3a8a");
                        col.Item().PaddingTop(1).Text($"Periodo: {vm.TermName ?? "—"}")
                            .FontSize(7).FontColor("#64748b");
                        col.Item().LineHorizontal(1).LineColor("#1e3a8a");
                    });

                    page.Content().PaddingVertical(2).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(110);
                            for (int i = 0; i < vm.Columns.Count; i++)
                                columns.RelativeColumn(1);
                            columns.ConstantColumn(35);
                        });

                        table.Header(h =>
                        {
                            // Estas columnas ocupan las dos filas del encabezado
                            h.Cell().RowSpan(2).Element(CellStyle)
                                .Background("#2563eb")
                                .Padding(2)
                                .AlignMiddle()
                                .Text("FACULTAD")
                                .FontColor(Colors.White)
                                .Bold()
                                .FontSize(9f);

                            h.Cell().RowSpan(2).Element(CellStyle)
                                .Background("#2563eb")
                                .Padding(2)
                                .AlignMiddle()
                                .Text("CARRERA")
                                .FontColor(Colors.White)
                                .Bold()
                                .FontSize(9f);

                            // Primera fila: modalidades
                            foreach (var group in vm.ModalityGroups)
                            {
                                if (group.HasSubHeaders)
                                {
                                    h.Cell()
                                        .ColumnSpan((uint)group.ColumnCount)
                                        .Element(CellStyle)
                                        .Background("#2563eb")
                                        .Padding(2)
                                        .AlignCenter()
                                        .AlignMiddle()
                                        .Text(group.ModalityName)
                                        .FontColor(Colors.White)
                                        .Bold()

                                        .FontSize(7f);
                                }
                                else
                                {
                                    // También ocupa dos filas porque no tiene subencabezados
                                    h.Cell()
                                        .RowSpan(2)
                                        .Background("#2563eb")
                                        .Element(CellStyle)
                                        .Padding(2)
                                        .AlignCenter()
                                        .AlignMiddle()
                                        .Text(group.ModalityName)
                                        .FontColor(Colors.White)
                                        .Bold()
                                        .FontSize(7f);
                                }
                            }

                            // TOTAL también ocupa dos filas
                            h.Cell()
                                .RowSpan(2)
                                .Background("#1e3a8a")
                                .Padding(2)
                                .AlignCenter()
                                .AlignMiddle()
                                .Text("TOT")
                                .FontColor(Colors.White)
                                .Bold()
                                .FontSize(7f);

                            // Segunda fila: subencabezados
                            foreach (var group in vm.ModalityGroups.Where(g => g.HasSubHeaders))
                            {
                                foreach (var sub in vm.Columns.Where(c => c.ModalityId == group.ModalityId))
                                {
                                    h.Cell().Element(CellStyle)
                                        .Background("#3b82f6")
                                        .Padding(1)
                                        .AlignCenter()
                                        .AlignMiddle()
                                        .Text(sub.Header)
                                        .FontColor(Colors.White)
                                        .Bold()
                                        .FontSize(7f);
                                }
                            }
                        });

                        foreach (var faculty in vm.Faculties)
                        {
                            for (int ci = 0; ci < faculty.Careers.Count; ci++)
                            {
                                var career = faculty.Careers[ci];
                                var bgColor = ci % 2 == 0 ? Colors.White : Color.FromHex("#f8fafc");

                                // Solo en la primera carrera de la facultad
                                if (ci == 0)
                                {
                                    table.Cell()
                                        .RowSpan((uint)faculty.Careers.Count)
                                        .Element(CellStyle)
                                        .Background(Color.FromHex("#eff6ff"))
                                        .Padding(2)
                                        .AlignMiddle()
                                        .Text(faculty.Name)
                                        .Bold()
                                        .FontSize(7f);
                                }

                                table.Cell()
                                    .Element(CellStyle)
                                    .Background(bgColor)
                                    .Padding(1.5f)
                                    .Text(career.CareerName)
                                    .FontSize(9f);

                                foreach (var value in career.Values)
                                {
                                    table.Cell()
                                        .Element(CellStyle)
                                        .Background(bgColor)
                                        .Padding(1)
                                        .AlignCenter()
                                        .AlignMiddle()
                                        .Text(value.ToString())
                                        .FontSize(9f);
                                }

                                table.Cell()
                                    .Element(CellStyle)
                                    .Background(bgColor)
                                    .Padding(1)
                                    .AlignCenter()
                                    .AlignMiddle()
                                    .Text(career.Total.ToString())
                                    .Bold()
                                    .FontSize(7f);
                            }
                        }

                        table.Cell().ColumnSpan(2).Background("#1e3a8a").Padding(2)
                            .Text("TOTALES").FontColor(Colors.White).Bold().FontSize(7f).AlignRight();

                        for (int i = 0; i < vm.ColumnTotals.Count; i++)
                        {
                            table.Cell().Background("#1e3a8a").Padding(1)
                                .Text(vm.ColumnTotals[i].ToString()).FontColor(Colors.White)
                                .Bold().FontSize(9f).AlignCenter();
                        }

                        table.Cell().Background("#1e3a8a").Padding(1)
                            .Text(vm.GrandTotal.ToString()).FontColor(Colors.White)
                            .Bold().FontSize(9f).AlignCenter();
                    });

                    page.Footer().Row(row => ApplyPdfFooter(row, now));
                });
            }).GeneratePdf();
        }

        // ═══════════════════════════════════════════════════════
        // SORTEO DE AULAS — RESUMEN — EXCEL
        // ═══════════════════════════════════════════════════════
        public byte[] BuildSorteoAulasResumenExcel(SorteoAulasReportViewModel vm)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Resumen Sorteo");

            ws.Cell(1, 1).Value = $"REPORTE RESUMEN — SORTEO DE AULAS — {vm.ModalityName}";
            ws.Range(1, 1, 1, 6).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(13)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = $"Periodo: {vm.TermName} | Modalidad: {vm.ModalityName} | Impresión: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range(2, 1, 2, 6).Merge().Style
                .Font.SetFontSize(9).Font.SetFontColor(XLColor.FromHtml("#64748b"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

            int row = 4;

            foreach (var pavilion in vm.Summary.PorPabellon)
            {
                ws.Cell(row, 1).Value = $"PABELLÓN: {pavilion.PavilionCode} — {pavilion.PavilionName}";
                ws.Range(row, 1, row, 6).Merge().Style
                    .Font.SetBold(true).Font.SetFontSize(11)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                    .Font.SetFontColor(XLColor.White);
                row++;

                foreach (var group in pavilion.Groups)
                {
                    ws.Cell(row, 1).Value = $"  Grupo: {group.GroupName}";
                    ws.Range(row, 1, row, 6).Merge().Style
                        .Font.SetBold(true).Font.SetFontSize(10)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#dbeafe"))
                        .Font.SetFontColor(XLColor.FromHtml("#1e3a8a"));
                    row++;

                    var headers = new[] { "SALÓN", "PISO", "AFORO", "ASIGNADOS", "DOCENTE", "ÁREA" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        ws.Cell(row, i + 1).Value = headers[i];
                        ws.Cell(row, i + 1).Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                            .Fill.SetBackgroundColor(XLColor.FromHtml("#2563eb"))
                            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                            .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                    }
                    row++;

                    foreach (var classroom in group.Classrooms)
                    {
                        ws.Cell(row, 1).Value = classroom.ClassroomName;
                        ws.Cell(row, 2).Value = classroom.Piso;
                        ws.Cell(row, 3).Value = classroom.Capacidad;
                        ws.Cell(row, 4).Value = classroom.Asignados;
                        ws.Cell(row, 5).Value = classroom.Docente ?? "—";
                        ws.Cell(row, 6).Value = classroom.AreaTematica ?? "—";
                        for (int c = 1; c <= 6; c++)
                            ws.Cell(row, c).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                        row++;
                    }

                    ws.Cell(row, 1).Value = $"TOTAL GRUPO {group.GroupName}";
                    ws.Cell(row, 3).Value = group.Capacidad;
                    ws.Cell(row, 4).Value = group.TotalAsignados;
                    ws.Range(row, 1, row, 6).Merge().Style
                        .Font.SetBold(true)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#e0e7ff"))
                        .Font.SetFontColor(XLColor.FromHtml("#1e3a8a"));
                    row += 2;
                }

                ws.Cell(row, 1).Value = $"TOTAL PABELLÓN {pavilion.PavilionCode}: {pavilion.TotalAsignados} asignados / {pavilion.TotalAforo} aforo";
                ws.Range(row, 1, row, 6).Merge().Style
                    .Font.SetBold(true).Font.SetFontSize(10)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                    .Font.SetFontColor(XLColor.White);
                row += 2;
            }

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        // ═══════════════════════════════════════════════════════
        // SORTEO DE AULAS — RESUMEN — PDF
        // ═══════════════════════════════════════════════════════
        public byte[] BuildSorteoAulasResumenPdf(SorteoAulasReportViewModel vm)
        {
            var bgBytes = LoadBackground();
            var now = DateTime.Now;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(15, Unit.Millimetre);

                    if (bgBytes != null)
                    {
                        page.Background().Layers(layers =>
                        {
                            layers.PrimaryLayer().Image(bgBytes).FitArea();
                        });
                    }

                    page.DefaultTextStyle(x => x.FontSize(7f).FontFamily(Fonts.Calibri));

                    page.Header().PaddingTop(25).Column(col =>
                    {
                        col.Item().Text($"REPORTE RESUMEN — SORTEO DE AULAS — {vm.ModalityName}")
                            .Bold().FontSize(11).FontColor("#1e3a8a");
                        col.Item().PaddingTop(2).Text($"Periodo: {vm.TermName} | Total: {vm.Summary.TotalAsignados} asignados en {vm.Summary.TotalAulas} aulas")
                            .FontSize(7).FontColor("#64748b");
                        col.Item().LineHorizontal(1).LineColor("#1e3a8a");
                    });

                    page.Content().PaddingVertical(4).Column(col =>
                    {
                        foreach (var pavilion in vm.Summary.PorPabellon)
                        {
                            col.Item().PaddingTop(6).Text($"PABELLÓN {pavilion.PavilionCode} — {pavilion.PavilionName}")
                                .Bold().FontSize(10).FontColor("#1e3a8a");

                            foreach (var group in pavilion.Groups)
                            {
                                col.Item().PaddingTop(4).Text($"Grupo {group.GroupName}")
                                    .Bold().FontSize(8).FontColor("#2563eb");

                                col.Item().PaddingTop(2).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(80);
                                        columns.ConstantColumn(40);
                                        columns.ConstantColumn(60);
                                        columns.ConstantColumn(70);
                                        columns.ConstantColumn(120);
                                        columns.ConstantColumn(60);
                                    });

                                    table.Header(h =>
                                    {
                                        var hdrs = new[] { "SALÓN", "PISO", "AFORO", "ASIGNADOS", "DOCENTE", "ÁREA" };
                                        foreach (var hdr in hdrs)
                                            h.Cell().Element(CellStyle).Background("#2563eb").Padding(2)
                                                .Text(hdr).FontColor(Colors.White).Bold().FontSize(6f);
                                    });

                                    foreach (var c in group.Classrooms)
                                    {
                                        table.Cell().Element(CellStyle).Padding(2).Text(c.ClassroomName).FontSize(6.5f);
                                        table.Cell().Element(CellStyle).Padding(2).Text(c.Piso.ToString()).FontSize(6.5f);
                                        table.Cell().Element(CellStyle).Padding(2).Text(c.Capacidad.ToString()).FontSize(6.5f);
                                        table.Cell().Element(CellStyle).Padding(2).Text(c.Asignados.ToString()).FontSize(6.5f);
                                        table.Cell().Element(CellStyle).Padding(2).Text(c.Docente ?? "—").FontSize(6.5f);
                                        table.Cell().Element(CellStyle).Padding(2).Text(c.AreaTematica ?? "—").FontSize(6.5f);
                                    }

                                    table.Cell().Padding(2).Text($"TOTAL").Bold().FontSize(6.5f);
                                    table.Cell().Padding(2).Text("").FontSize(6.5f);
                                    table.Cell().Padding(2).Text(group.Capacidad.ToString()).Bold().FontSize(6.5f);
                                    table.Cell().Padding(2).Text(group.TotalAsignados.ToString()).Bold().FontSize(6.5f);
                                    table.Cell().Padding(2).Text("").FontSize(6.5f);
                                    table.Cell().Padding(2).Text("").FontSize(6.5f);
                                });
                            }

                            col.Item().PaddingTop(2).Text($"Total Pabellón {pavilion.PavilionCode}: {pavilion.TotalAsignados} asignados / {pavilion.TotalAforo} aforo")
                                .Bold().FontSize(8).FontColor("#1e3a8a");
                        }
                    });

                    page.Footer().Row(row => ApplyPdfFooter(row, now));
                });
            }).GeneratePdf();
        }

        // ═══════════════════════════════════════════════════════
        // SORTEO DE AULAS — LISTADO — PDF
        // ═══════════════════════════════════════════════════════
        public byte[] BuildSorteoAulasListadoPdf(SorteoAulasReportViewModel vm)
        {
            var bgPath = Path.Combine(_env.WebRootPath, "img", "background.jpg");
            var bgBytes = System.IO.File.Exists(bgPath) ? System.IO.File.ReadAllBytes(bgPath) : null;
            var now = DateTime.Now;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(15, Unit.Millimetre);

                    if (bgBytes != null)
                    {
                        page.Background().Layers(layers =>
                        {
                            layers.PrimaryLayer().Image(bgBytes).FitArea();
                        });
                    }

                    page.DefaultTextStyle(x => x.FontSize(6.5f).FontFamily(Fonts.Calibri));

                    page.Header().PaddingTop(60).Column(col =>
                    {
                        col.Item().Text($"LISTADO DE ASIGNACIONES — SORTEO DE AULAS — {vm.ModalityName}")
                            .Bold().FontSize(11).FontColor("#1e3a8a");
                        col.Item().PaddingTop(2).Text($"Periodo: {vm.TermName} | Total: {vm.Details.Count} postulantes")
                            .FontSize(7).FontColor("#64748b");
                        col.Item().LineHorizontal(1).LineColor("#1e3a8a");
                    });

                    page.Content().PaddingVertical(4).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            
                            columns.ConstantColumn(36);  // Silla
                            columns.ConstantColumn(45);  // Código
                            columns.ConstantColumn(175);  // Apellidos
                            columns.ConstantColumn(125);  // Carrera
                            columns.ConstantColumn(52);  // Aula
                            columns.ConstantColumn(76);  // Foto
                        });

                        table.Header(h =>
                        {
                            var hdrs = new[] { "SILLA", "CÓDIGO", "APELLIDOS Y NOMBRES", "CARRERA", "AULA", "FOTO" };
                            foreach (var hdr in hdrs)
                                h.Cell().Element(CellStyle).Background("#2563eb").Padding(2)
                                    .Text(hdr).FontColor(Colors.White).Bold().FontSize(9f);
                        });

                        foreach (var item in vm.Details)
                        {

                            table.Cell().Element(CellStyle).Padding(1).AlignCenter().AlignMiddle().Text(item.Silla.ToString()).FontSize(11f);
                            table.Cell().Element(CellStyle).Padding(1).AlignCenter().AlignMiddle().Text(item.CodigoPostulante).FontSize(11f);
                            table.Cell().Element(CellStyle).Padding(1).AlignMiddle().Text($"{item.Apellidos}, {item.Nombres}").FontSize(11f);
                            table.Cell().Element(CellStyle).Padding(1).AlignMiddle().Text(item.Carrera).FontSize(11f);
                            table.Cell().Element(CellStyle).Padding(1).AlignCenter().AlignMiddle().Text(item.Aula).FontSize(11f);
                            table.Cell().Element(CellStyle).Padding(1).Column(col =>
                            {
                                if (item.PhotoBytes is { Length: > 0 })
                                    col.Item().Height(66).Width(72).Image(item.PhotoBytes).FitArea();
                                else
                                    col.Item().Height(66).Width(72).AlignCenter().AlignMiddle()
                                        .Text("—").FontSize(9f).FontColor("#94a3b8");
                            });
                        }
                    });

                    page.Footer().Row(row => ApplyPdfFooter(row, now));
                });
            }).GeneratePdf();
        }

        // ═══════════════════════════════════════════════════════
        // SORTEO DE AULAS — LISTADO — EXCEL
        // ═══════════════════════════════════════════════════════
        public byte[] BuildSorteoAulasListadoExcel(SorteoAulasReportViewModel vm)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Listado Sorteo");

            ws.Cell(1, 1).Value = $"LISTADO DE ASIGNACIONES — SORTEO DE AULAS — {vm.ModalityName}";
            ws.Range(1, 1, 1, 8).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(13)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = $"Periodo: {vm.TermName} | Impresión: {DateTime.Now:dd/MM/yyyy HH:mm}";
            ws.Range(2, 1, 2, 8).Merge().Style
                .Font.SetFontSize(9).Font.SetFontColor(XLColor.FromHtml("#64748b"));

            int row = 4;
            var headers = new[] { "FOTO", "SILLA", "CÓDIGO POSTULANTE", "APELLIDOS Y NOMBRES", "CARRERA", "AULA", "PABELLÓN" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cell(row, i + 1).Value = headers[i];
                ws.Cell(row, i + 1).Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#2563eb"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
            row++;

            foreach (var item in vm.Details)
            {
                ws.Row(row).Height = 50;

                if (item.PhotoBytes is { Length: > 0 })
                {
                    using var stream = new MemoryStream(item.PhotoBytes);
                    ws.AddPicture(stream)
                        .MoveTo(ws.Cell(row, 1))
                        .WithSize(42, 42);
                }

                ws.Cell(row, 2).Value = item.Silla;
                ws.Cell(row, 3).Value = item.CodigoPostulante;
                ws.Cell(row, 4).Value = $"{item.Apellidos}, {item.Nombres}";
                ws.Cell(row, 5).Value = item.Carrera;
                ws.Cell(row, 6).Value = item.Aula;
                ws.Cell(row, 7).Value = item.Pabellon ?? "—";
                for (int c = 2; c <= 7; c++)
                    ws.Cell(row, c).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                row++;
            }

            ws.Cell(row, 1).Value = $"TOTAL: {vm.Details.Count} registro(s)";
            ws.Range(row, 1, row, 8).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(11)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }
        static IContainer CellStyle(IContainer container)
        {
            return container
                .Border(0.1f)
                .BorderColor("000")
                .Padding(0);
        }

        // ═══════════════════════════════════════════════════════
        // ASISTENCIAS — EXCEL
        // ═══════════════════════════════════════════════════════
        public byte[] BuildAsistenciasExcel(AttendanceReportViewModel vm)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Asistencias");

            var title = $"REPORTE DE ASISTENCIAS — {vm.ModalityName ?? "Todas las modalidades"}";
            ws.Cell(1, 1).Value = title;
            ws.Range(1, 1, 1, 10).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(13)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value = $"Periodo: {vm.TermName} | Total asignados: {vm.TotalAssigned} | Asistieron: {vm.TotalAttended} | Faltaron: {vm.TotalMissing}";
            ws.Range(2, 1, 2, 10).Merge().Style
                .Font.SetFontSize(9).Font.SetFontColor(XLColor.FromHtml("#64748b"))
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

            var headers = new[] { "N° Silla", "Código", "Apellidos y Nombres", "Modalidad", "Tipo Modalidad", "Discapacidad", "Aula", "Docente", "Estado" };
            var headerRow = 4;
            for (int c = 0; c < headers.Length; c++)
            {
                ws.Cell(headerRow, c + 1).Value = headers[c];
                ws.Cell(headerRow, c + 1).Style
                    .Font.SetBold(true).Font.SetFontSize(8)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#2563eb"))
                    .Font.SetFontColor(XLColor.White)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            int row = headerRow + 1;
            int num = 1;
            foreach (var item in vm.Items)
            {
                ws.Cell(row, 1).Value = num++;
                ws.Cell(row, 2).Value = item.CodePostulant;
                ws.Cell(row, 3).Value = $"{item.Apellidos}, {item.Nombres}";
                ws.Cell(row, 4).Value = item.Modality;
                ws.Cell(row, 5).Value = item.TypeModality;
                ws.Cell(row, 6).Value = item.Disability;
                ws.Cell(row, 7).Value = item.Classroom;
                ws.Cell(row, 8).Value = item.Docente;
                ws.Cell(row, 9).Value = item.AttendanceStatus;

                for (int c = 1; c <= 9; c++)
                {
                    ws.Cell(row, c).Style.Font.SetFontSize(8)
                        .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                        .Alignment.SetHorizontal(c <= 2 ? XLAlignmentHorizontalValues.Center : XLAlignmentHorizontalValues.Left);
                }

                if (item.AttendanceStatus == "No asistió")
                {
                    for (int c = 1; c <= 9; c++)
                        ws.Cell(row, c).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#fef2f2"));
                }

                row++;
            }

            // Summary by classroom
            row += 2;
            ws.Cell(row, 1).Value = "RESUMEN POR AULA";
            ws.Range(row, 1, row, 4).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(10)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White);
            row++;

            var sumHeaders = new[] { "Aula", "Asistieron", "Faltaron", "Total" };
            for (int c = 0; c < sumHeaders.Length; c++)
            {
                ws.Cell(row, c + 1).Value = sumHeaders[c];
                ws.Cell(row, c + 1).Style.Font.SetBold(true).Font.SetFontSize(8)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#e5e7eb"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
            row++;

            foreach (var s in vm.SummaryByClassroom)
            {
                ws.Cell(row, 1).Value = s.Classroom;
                ws.Cell(row, 2).Value = s.Attended;
                ws.Cell(row, 3).Value = s.Missing;
                ws.Cell(row, 4).Value = s.Total;
                for (int c = 1; c <= 4; c++)
                    ws.Cell(row, c).Style.Font.SetFontSize(8).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                row++;
            }

            ws.Cell(row, 1).Value = "TOTAL";
            ws.Cell(row, 2).Value = vm.TotalAttended;
            ws.Cell(row, 3).Value = vm.TotalMissing;
            ws.Cell(row, 4).Value = vm.TotalAssigned;
            for (int c = 1; c <= 4; c++)
                ws.Cell(row, c).Style.Font.SetBold(true).Font.SetFontSize(8)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#dbeafe"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);

            // Summary by area
            row += 2;
            ws.Cell(row, 1).Value = "RESUMEN POR ÁREA TEMÁTICA";
            ws.Range(row, 1, row, 4).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(10)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White);
            row++;

            for (int c = 0; c < sumHeaders.Length; c++)
            {
                ws.Cell(row, c + 1).Value = sumHeaders[c].Replace("Aula", "Área");
                ws.Cell(row, c + 1).Style.Font.SetBold(true).Font.SetFontSize(8)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#e5e7eb"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
            row++;

            foreach (var s in vm.SummaryByArea)
            {
                ws.Cell(row, 1).Value = s.Area;
                ws.Cell(row, 2).Value = s.Attended;
                ws.Cell(row, 3).Value = s.Missing;
                ws.Cell(row, 4).Value = s.Total;
                for (int c = 1; c <= 4; c++)
                    ws.Cell(row, c).Style.Font.SetFontSize(8).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                row++;
            }

            // Summary by career
            row += 2;
            ws.Cell(row, 1).Value = "RESUMEN POR CARRERA PROFESIONAL";
            ws.Range(row, 1, row, 4).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(10)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White);
            row++;

            var careerHeaders = new[] { "Carrera", "Asistieron", "Faltaron", "Total" };
            for (int c = 0; c < careerHeaders.Length; c++)
            {
                ws.Cell(row, c + 1).Value = careerHeaders[c];
                ws.Cell(row, c + 1).Style.Font.SetBold(true).Font.SetFontSize(8)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#e5e7eb"))
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
            row++;

            foreach (var s in vm.SummaryByCareer)
            {
                ws.Cell(row, 1).Value = s.Career;
                ws.Cell(row, 2).Value = s.Attended;
                ws.Cell(row, 3).Value = s.Missing;
                ws.Cell(row, 4).Value = s.Total;
                for (int c = 1; c <= 4; c++)
                    ws.Cell(row, c).Style.Font.SetFontSize(8).Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                row++;
            }

            ws.Columns().AdjustToContents();
            ws.Column(1).Width = 12;
            ws.Column(2).Width = 14;
            ws.Column(3).Width = 50;
            ws.Column(4).Width = 20;
            ws.Column(5).Width = 20;
            ws.Column(6).Width = 20;
            ws.Column(7).Width = 15;
            ws.Column(8).Width = 20;
            ws.Column(9).Width = 14;

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }

        // ═══════════════════════════════════════════════════════
        // ASISTENCIAS — PDF (LANDSCAPE)
        // ═══════════════════════════════════════════════════════
        public byte[] BuildAsistenciasPdf(AttendanceReportViewModel vm)
        {
            var bgBytes = LoadBackground();
            var now = DateTime.Now;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(15, Unit.Millimetre);

                    if (bgBytes != null)
                    {
                        page.Background().Layers(layers =>
                        {
                            layers.PrimaryLayer().Image(bgBytes).FitArea();
                        });
                    }

                    page.DefaultTextStyle(x => x.FontSize(6.5f).FontFamily(Fonts.Calibri));

                    page.Header().PaddingTop(25).Column(col =>
                    {
                        col.Item().Text($"REPORTE DE ASISTENCIAS — {vm.ModalityName ?? "Todas las modalidades"}")
                            .Bold().FontSize(11).FontColor("#1e3a8a");
                        col.Item().PaddingTop(2).Text($"Periodo: {vm.TermName} | Asignados: {vm.TotalAssigned} | Asistieron: {vm.TotalAttended} | Faltaron: {vm.TotalMissing}")
                            .FontSize(7).FontColor("#64748b");
                        col.Item().LineHorizontal(1).LineColor("#1e3a8a");
                    });

                    page.Content().PaddingVertical(4).Column(col =>
                    {
                        // Main data table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(28);   // N°
                                columns.ConstantColumn(50);   // Código
                                columns.RelativeColumn(2);    // Apellidos y Nombres
                                columns.ConstantColumn(105);   // Modalidad
                                columns.ConstantColumn(85);   // Tipo
                                columns.ConstantColumn(75);   // Discapacidad
                                columns.ConstantColumn(45);   // Aula
                                columns.ConstantColumn(95);   // Docente
                                columns.ConstantColumn(45);   // Estado
                            });

                            table.Header(h =>
                            {
                                var hdrs = new[] { "N°", "Código", "Apellidos y Nombres", "Modalidad", "Tipo Mod.", "Discapacidad", "Aula", "Docente", "Estado" };
                                foreach (var hdr in hdrs)
                                    h.Cell().Element(CellStyle).Background("#2563eb").Padding(2)
                                        .Text(hdr).FontColor(Colors.White).Bold().FontSize(9f);
                            });

                            int num = 1;
                            foreach (var item in vm.Items)
                            {
                                table.Cell().Element(CellStyle).Padding(2).Text(num++.ToString()).FontSize(9f).AlignCenter();
                                table.Cell().Element(CellStyle).Padding(2).Text(item.CodePostulant).FontSize(9f);
                                table.Cell().Element(CellStyle).Padding(2).Text($"{item.Apellidos}, {item.Nombres}").FontSize(9f);
                                table.Cell().Element(CellStyle).Padding(2).Text(item.Modality).FontSize(9f);
                                table.Cell().Element(CellStyle).Padding(2).Text(item.TypeModality).FontSize(9f);
                                table.Cell().Element(CellStyle).Padding(2).Text(item.Disability).FontSize(9f);
                                table.Cell().Element(CellStyle).Padding(2).Text(item.Classroom).FontSize(9f);
                                table.Cell().Element(CellStyle).Padding(2).Text(item.Docente).FontSize(9f);
                                table.Cell().Element(CellStyle).Padding(2).Text(item.AttendanceStatus).FontSize(9f);
                            }
                        });

                        // Summary by classroom
                        col.Item().PaddingTop(10).Text("RESUMEN POR AULA").Bold().FontSize(8).FontColor("#1e3a8a");
                        col.Item().PaddingTop(2).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                            });
                            table.Header(h =>
                            {
                                foreach (var hdr in new[] { "Aula", "Asistieron", "Faltaron", "Total" })
                                    h.Cell().Element(CellStyle).Background("#2563eb").Padding(2)
                                        .Text(hdr).FontColor(Colors.White).Bold().FontSize(6f);
                            });
                            foreach (var s in vm.SummaryByClassroom)
                            {
                                table.Cell().Element(CellStyle).Padding(2).Text(s.Classroom).FontSize(9f);
                                table.Cell().Element(CellStyle).Padding(2).Text(s.Attended.ToString()).FontSize(9f).AlignCenter();
                                table.Cell().Element(CellStyle).Padding(2).Text(s.Missing.ToString()).FontSize(9f).AlignCenter();
                                table.Cell().Element(CellStyle).Padding(2).Text(s.Total.ToString()).FontSize(9f).AlignCenter();
                            }
                            table.Cell().Element(CellStyle).Padding(2).Text("TOTAL").Bold().FontSize(6f);
                            table.Cell().Element(CellStyle).Padding(2).Text(vm.TotalAttended.ToString()).Bold().FontSize(9f).AlignCenter();
                            table.Cell().Element(CellStyle).Padding(2).Text(vm.TotalMissing.ToString()).Bold().FontSize(9f).AlignCenter();
                            table.Cell().Element(CellStyle).Padding(2).Text(vm.TotalAssigned.ToString()).Bold().FontSize(9f).AlignCenter();
                        });

                        // Summary by area
                        col.Item().PaddingTop(8).Text("RESUMEN POR ÁREA TEMÁTICA").Bold().FontSize(8).FontColor("#1e3a8a");
                        col.Item().PaddingTop(2).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                            });
                            table.Header(h =>
                            {
                                foreach (var hdr in new[] { "Área", "Asistieron", "Faltaron", "Total" })
                                    h.Cell().Element(CellStyle).Background("#2563eb").Padding(2)
                                        .Text(hdr).FontColor(Colors.White).Bold().FontSize(9f);
                            });
                            foreach (var s in vm.SummaryByArea)
                            {
                                table.Cell().Element(CellStyle).Padding(2).Text(s.Area).FontSize(6f);
                                table.Cell().Element(CellStyle).Padding(2).Text(s.Attended.ToString()).FontSize(9f).AlignCenter();
                                table.Cell().Element(CellStyle).Padding(2).Text(s.Missing.ToString()).FontSize(9f).AlignCenter();
                                table.Cell().Element(CellStyle).Padding(2).Text(s.Total.ToString()).FontSize(9f).AlignCenter();
                            }
                        });

                        // Summary by career
                        col.Item().PaddingTop(8).Text("RESUMEN POR CARRERA PROFESIONAL").Bold().FontSize(8).FontColor("#1e3a8a");
                        col.Item().PaddingTop(2).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(60);
                            });
                            table.Header(h =>
                            {
                                foreach (var hdr in new[] { "Carrera", "Asistieron", "Faltaron", "Total" })
                                    h.Cell().Element(CellStyle).Background("#2563eb").Padding(2)
                                        .Text(hdr).FontColor(Colors.White).Bold().FontSize(6f);
                            });
                            foreach (var s in vm.SummaryByCareer)
                            {
                                table.Cell().Element(CellStyle).Padding(2).Text(s.Career).FontSize(9f);
                                table.Cell().Element(CellStyle).Padding(2).Text(s.Attended.ToString()).FontSize(9f).AlignCenter();
                                table.Cell().Element(CellStyle).Padding(2).Text(s.Missing.ToString()).FontSize(9f).AlignCenter();
                                table.Cell().Element(CellStyle).Padding(2).Text(s.Total.ToString()).FontSize(9f).AlignCenter();
                            }
                        });
                    });

                    page.Footer().Row(row => ApplyPdfFooter(row, now));
                });
            }).GeneratePdf();
        }
    }
}
