using ADMISION.ENTITIES.Data;
using ADMISION.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ADMISION.Services.Implementations
{
    public class ExamProcessingService : IExamProcessingService
    {
        private readonly AppDbContext _context;
        private static readonly string[] ValidLetters = { "A", "B", "C", "D", "E" };

        public ExamProcessingService(AppDbContext context)
        {
            _context = context;
        }

        public ExternalScoringResult ProcessExternal(
            Stream keyStream,
            Stream answersStream,
            Stream? identificationStream,
            Stream? bdStream,
            string? bdFileName,
            ExternalScoringParameters parameters)
        {
            var result = new ExternalScoringResult();

            // 1. Parsear CLAVE
            var (keyHeader, keyRows) = ParseCsv(keyStream);
            if (keyHeader.Count < 1 || keyRows.Count == 0)
            {
                result.Errors.Add("El archivo de clave es inválido o está vacío.");
                return result;
            }
            var keyCols = DetectColumns(keyHeader);
            if (keyCols.FirstQuestionIdx < 0) { result.Errors.Add("La clave no tiene columnas de preguntas (R1..Rn)."); return result; }
            bool hasTemaCol = keyCols.TemaIdx >= 0;
            bool allTemasEmpty = !hasTemaCol || keyRows.All(r =>
                r.Count <= keyCols.TemaIdx || string.IsNullOrWhiteSpace(r[keyCols.TemaIdx]));
            bool isDirimencia = !hasTemaCol || allTemasEmpty;
            if (isDirimencia && keyRows.Count > 1)
            {
                result.Errors.Add("La clave es dirimencia (TEMA vacío) pero contiene múltiples filas. Debe haber una sola fila.");
                return result;
            }

            int totalKeyCols = keyHeader.Count - keyCols.FirstQuestionIdx;
            int preguntasEfectivas = 0;
            for (int q = 0; q < totalKeyCols; q++)
            {
                bool anyFilled = false;
                foreach (var row in keyRows)
                {
                    int col = keyCols.FirstQuestionIdx + q;
                    if (col < row.Count && !string.IsNullOrWhiteSpace(row[col])) { anyFilled = true; break; }
                }
                if (anyFilled) preguntasEfectivas = q + 1;
            }
            if (preguntasEfectivas == 0) { result.Errors.Add("La clave no tiene respuestas."); return result; }
            result.TotalPreguntas = preguntasEfectivas;

            var keysByTema = new Dictionary<string, Dictionary<int, (string Resp, bool Anulada)>>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in keyRows)
            {
                string tema;
                if (isDirimencia)
                {
                    tema = "";
                }
                else
                {
                    if (row.Count <= keyCols.TemaIdx) continue;
                    tema = Normalize(row[keyCols.TemaIdx]);
                    if (string.IsNullOrEmpty(tema)) continue;
                }
                if (keysByTema.ContainsKey(tema))
                {
                    result.Warnings.Add($"TEMA duplicado '{tema}' en clave: se mantiene la primera aparición.");
                    continue;
                }
                var dict = new Dictionary<int, (string Resp, bool Anulada)>();
                for (int q = 0; q < preguntasEfectivas; q++)
                {
                    int numero = q + 1;
                    int col = keyCols.FirstQuestionIdx + q;
                    string ans = col < row.Count ? Normalize(row[col]) : "";
                    bool valid = ValidLetters.Contains(ans);
                    dict[numero] = (valid ? ans : "", !valid);
                }
                keysByTema[tema] = dict;
            }
            result.TotalTemas = keysByTema.Count;
            if (result.TotalTemas == 0) { result.Errors.Add("No se pudo extraer ninguna clave."); return result; }

            // 1.5 Parsear IDENTIFICACIÓN opcional
            Dictionary<string, IdentInfo>? identMap = null;
            if (identificationStream != null)
            {
                identMap = ParseIdentification(identificationStream, result.Warnings);
                if (identMap.Count == 0)
                {
                    result.Warnings.Add("Archivo de identificación vacío o sin columnas LITHO/CODIGO reconocibles. Se ignora.");
                    identMap = null;
                }
            }

            // 2. Parsear BD opcional
            var bdIndex = new Dictionary<string, (string Nombre, string Carrera, string Modalidad, string TipoModalidad, string Tema)>(StringComparer.OrdinalIgnoreCase);
            if (bdStream != null)
            {
                try
                {
                    bool isExcel = !string.IsNullOrEmpty(bdFileName) &&
                                   (bdFileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                                    bdFileName.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase) ||
                                    bdFileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase));

                    List<string> bdHeader;
                    List<List<string>> bdRows;
                    if (isExcel)
                    {
                        (bdHeader, bdRows) = ParseExcel(bdStream);
                    }
                    else
                    {
                        (bdHeader, bdRows) = ParseCsv(bdStream);
                    }

                    (bdHeader, bdRows) = ResolveBdHeader(bdHeader, bdRows);
                    var bdCols = DetectBdColumns(bdHeader);
                    if (bdCols.CodigoIdx < 0)
                    {
                        result.Warnings.Add("Archivo BD: no se encontró columna CODIGO/LITHO, se ignora.");
                    }
                    else
                    {
                        foreach (var row in bdRows)
                        {
                            if (row.Count <= bdCols.CodigoIdx) continue;
                            string code = NormalizeLitho(row[bdCols.CodigoIdx]);
                            if (string.IsNullOrEmpty(code)) continue;
                            string nombre = bdCols.NombreIdx >= 0 && bdCols.NombreIdx < row.Count ? row[bdCols.NombreIdx].Trim() : "";
                            string carrera = bdCols.CarreraIdx >= 0 && bdCols.CarreraIdx < row.Count ? row[bdCols.CarreraIdx].Trim() : "";
                            string modalidad = bdCols.ModalidadIdx >= 0 && bdCols.ModalidadIdx < row.Count ? row[bdCols.ModalidadIdx].Trim() : "";
                            string tipoModalidad = bdCols.TipoModalidadIdx >= 0 && bdCols.TipoModalidadIdx < row.Count ? row[bdCols.TipoModalidadIdx].Trim() : "";
                            string tema = bdCols.TemaIdx >= 0 && bdCols.TemaIdx < row.Count ? Normalize(row[bdCols.TemaIdx]) : "";
                            if (!bdIndex.ContainsKey(code))
                                bdIndex[code] = (nombre, carrera, modalidad, tipoModalidad, tema);
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"No se pudo leer el archivo BD: {ex.Message}");
                }
            }

            // 3. Parsear respuestas de postulantes
            var (ansHeader, ansRows) = ParseCsv(answersStream);
            if (ansHeader.Count < 2 || ansRows.Count == 0)
            {
                result.Errors.Add("El archivo de respuestas está vacío o no tiene cabecera válida.");
                return result;
            }
            var ansCols = DetectColumns(ansHeader);

            var puntosPorPregunta = new decimal[preguntasEfectivas];
            for (int q = 0; q < preguntasEfectivas; q++)
                puntosPorPregunta[q] = GetCorrectaPoints(parameters, q + 1);

            var rows = new List<ExternalScoreRow>();
            var warningsAdded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in ansRows)
            {
                string litho = row.Count > 0 ? NormalizeLitho(row[0]) : "";
                if (string.IsNullOrEmpty(litho)) continue;

                string tema = "";
                if (ansCols.TemaIdx >= 0 && row.Count > ansCols.TemaIdx)
                    tema = Normalize(row[ansCols.TemaIdx]);

                string codigo = litho;
                string? identTema = null;
                if (identMap != null && identMap.TryGetValue(litho, out var ident))
                {
                    codigo = ident.Codigo ?? litho;
                    identTema = ident.Tema;
                }
                if (!string.IsNullOrEmpty(identTema) && string.IsNullOrEmpty(tema))
                    tema = identTema;

                string nombre = "", carrera = "", modalidad = "", tipoModalidad = "", bdTema = "";
                bool bdProvided = bdIndex.Count > 0;
                bool encontradoEnBd = false;
                if (bdProvided && bdIndex.TryGetValue(codigo, out var bd))
                {
                    encontradoEnBd = true;
                    nombre = bd.Nombre;
                    carrera = bd.Carrera;
                    modalidad = bd.Modalidad;
                    tipoModalidad = bd.TipoModalidad;
                    bdTema = bd.Tema;
                }
                if (!string.IsNullOrEmpty(bdTema) && string.IsNullOrEmpty(tema))
                    tema = bdTema;

                int correctas = 0, incorrectas = 0, blancas = 0, anuladas = 0, multiples = 0;
                decimal puntaje = 0m;
                decimal? nota = null;
                bool noSePresento = false;
                string? observacion = null;

                bool tieneClave = keysByTema.ContainsKey(tema) || keysByTema.ContainsKey("");
                if (!tieneClave)
                {
                    observacion = string.IsNullOrEmpty(tema)
                        ? "Sin clave de calificación (sin TEMA)"
                        : $"Sin clave para el tema '{tema}'";
                }
                else
                {
                    var effectiveKey = keysByTema.ContainsKey(tema) ? keysByTema[tema] : keysByTema[""];

                    decimal maxPuntaje = 0m;
                    for (int q = 0; q < preguntasEfectivas; q++)
                    {
                        int numero = q + 1;
                        int col = ansCols.FirstQuestionIdx + q;
                        string ans = col < row.Count ? Normalize(row[col]) : "";

                        maxPuntaje += puntosPorPregunta[q];

                        if (effectiveKey.TryGetValue(numero, out var keyInfo))
                        {
                            if (keyInfo.Anulada) { anuladas++; continue; }
                            if (string.IsNullOrEmpty(ans)) { blancas++; puntaje += parameters.PuntosBlanco; continue; }
                            if (ans.Length > 1) { multiples++; incorrectas++; puntaje -= parameters.PuntosIncorrecta; continue; }
                            if (ans == keyInfo.Resp) { correctas++; puntaje += puntosPorPregunta[q]; }
                            else { incorrectas++; puntaje -= parameters.PuntosIncorrecta; }
                        }
                    }

                    nota = maxPuntaje > 0 ? Math.Round(puntaje / maxPuntaje * 20, 4) : 0m;
                    noSePresento = correctas == 0 && incorrectas == 0 && blancas == 0;

                    if (multiples > 0)
                        observacion = "Múltiples respuestas detectadas";
                }

                bool esIngresante = nota.HasValue && puntaje >= parameters.NotaMinimaIngreso;

                if (!encontradoEnBd)
                {
                    result.TotalSinCoincidencia++;
                    if (observacion == null)
                        observacion = "Código sin coincidencia en BD";
                }

                rows.Add(new ExternalScoreRow
                {
                    Litho = litho,
                    Codigo = codigo,
                    Tema = tema,
                    Nombre = nombre,
                    Carrera = carrera,
                    Modalidad = modalidad,
                    TipoModalidad = tipoModalidad,
                    Correctas = correctas,
                    Incorrectas = incorrectas,
                    Blancas = blancas,
                    Anuladas = anuladas,
                    Multiples = multiples,
                    Puntaje = puntaje,
                    Vigesimal = nota,
                    EncontradoEnBD = encontradoEnBd,
                    EsIngresante = esIngresante,
                    NoSePresento = noSePresento,
                    Observacion = observacion
                });
            }

            // 5. Ordenar y asignar ranking
            var ranked = rows
                .OrderByDescending(r => r.Observacion == null && !r.NoSePresento)
                .ThenByDescending(r => r.Puntaje)
                .ThenBy(r => r.Incorrectas)
                .ToList();
            int rank = 0;
            foreach (var r in ranked)
                r.Ranking = (r.Observacion == null && !r.NoSePresento) ? ++rank : 0;

            result.Rows = ranked;
            result.TotalPostulantes = rows.Count;
            result.TotalIngresantes = rows.Count(r => r.EsIngresante);
            result.TotalNoPresentados = rows.Count(r => r.NoSePresento);
            result.TotalConErrores = rows.Count(r => !r.NoSePresento && r.Observacion != null);
            return result;
        }

        public byte[] BuildExternalExcel(ExternalScoringResult data, string profileName, string titulo)
        {
            using var wb = new XLWorkbook();

            var ws = wb.Worksheets.Add("Resultados");
            const int totalCols = 12;

            ws.Cell(1, 1).Value = string.IsNullOrWhiteSpace(titulo) ? "Resultados — Procesado Externo" : titulo;
            ws.Range(1, 1, 1, totalCols).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(14)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            ws.Cell(2, 1).Value =
                $"Perfil: {(string.IsNullOrWhiteSpace(profileName) ? "(sin perfil)" : profileName)}. " +
                $"Preguntas: {data.TotalPreguntas}. Temas: {data.TotalTemas}. " +
                $"Postulantes: {data.TotalPostulantes}. Ingresantes: {data.TotalIngresantes}. " +
                $"No presentados: {data.TotalNoPresentados}. Con errores: {data.TotalConErrores}. " +
                $"Sin coincidencia en BD: {data.TotalSinCoincidencia}.";
            ws.Range(2, 1, 2, totalCols).Merge().Style.Font.SetItalic(true).Font.SetFontSize(10).Fill.SetBackgroundColor(XLColor.FromHtml("#eff6ff"));

            var headers = new[] {
                "Nro", "CODIGO DE POSTULANTE", "APELLIDOS Y NOMBRES", "CARRERA PROFESIONAL",
                "MODALIDAD", "TIPO DE MODALIDAD", "TEMA", "CORRECTAS", "INCORRECTAS", "BLANCAS", "PUNTAJE", "NOTA"
            };
            for (int i = 0; i < headers.Length; i++)
            {
                var c = ws.Cell(4, i + 1);
                c.Value = headers[i];
                c.Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#374151"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }

            int r = 5;
            int nro = 0;
            foreach (var row in data.Rows)
            {
                nro++;
                ws.Cell(r, 1).Value = nro;
                ws.Cell(r, 2).Value = row.Codigo;
                ws.Cell(r, 3).Value = row.Nombre;
                ws.Cell(r, 4).Value = row.Carrera;
                ws.Cell(r, 5).Value = row.Modalidad;
                ws.Cell(r, 6).Value = row.TipoModalidad;
                ws.Cell(r, 7).Value = row.Tema;
                ws.Cell(r, 8).Value = row.Correctas;
                ws.Cell(r, 9).Value = row.Incorrectas;
                ws.Cell(r, 10).Value = row.Blancas;
                ws.Cell(r, 11).Value = row.Puntaje;
                ws.Cell(r, 12).Value = row.Vigesimal;

                if (!row.EncontradoEnBD)
                    ws.Range(r, 1, r, totalCols).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#fff7ed"));

                if (row.Observacion != null)
                    ws.Range(r, 1, r, totalCols).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#fef2f2"));

                r++;
            }

            ws.Column(11).Style.NumberFormat.SetFormat("0.0000");
            ws.Column(12).Style.NumberFormat.SetFormat("0.0000");
            ws.Columns().AdjustToContents();

            if (data.TotalSinCoincidencia > 0)
            {
                var wsM = wb.Worksheets.Add("Sin coincidencia en BD");
                wsM.Cell(1, 1).Value = "Fichas cuyo código de postulante no coincide con la BD de postulantes";
                wsM.Range(1, 1, 1, 7).Merge().Style
                    .Font.SetBold(true).Font.SetFontSize(12)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#b45309"))
                    .Font.SetFontColor(XLColor.White);

                var mHeaders = new[] { "Nro", "CODIGO DE POSTULANTE", "TEMA", "CORRECTAS", "BLANCAS", "PUNTAJE", "NOTA" };
                for (int i = 0; i < mHeaders.Length; i++)
                {
                    var c = wsM.Cell(3, i + 1);
                    c.Value = mHeaders[i];
                    c.Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                        .Fill.SetBackgroundColor(XLColor.FromHtml("#374151"))
                        .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                }

                int mr = 4;
                int mnro = 0;
                foreach (var row in data.Rows.Where(x => !x.EncontradoEnBD))
                {
                    mnro++;
                    wsM.Cell(mr, 1).Value = mnro;
                    wsM.Cell(mr, 2).Value = row.Codigo;
                    wsM.Cell(mr, 3).Value = row.Tema;
                    wsM.Cell(mr, 4).Value = row.Correctas;
                    wsM.Cell(mr, 5).Value = row.Blancas;
                    wsM.Cell(mr, 6).Value = row.Puntaje;
                    wsM.Cell(mr, 7).Value = row.Vigesimal;
                    mr++;
                }
                wsM.Column(6).Style.NumberFormat.SetFormat("0.0000");
                wsM.Column(7).Style.NumberFormat.SetFormat("0.0000");
                wsM.Columns().AdjustToContents();
            }

            if (data.Warnings.Count > 0 || data.Errors.Count > 0)
            {
                var ws2 = wb.Worksheets.Add("Avisos");
                int i = 1;
                ws2.Cell(i++, 1).Value = "Advertencias y errores del procesamiento";
                foreach (var w in data.Errors) ws2.Cell(i++, 1).Value = $"ERROR: {w}";
                foreach (var w in data.Warnings) ws2.Cell(i++, 1).Value = $"AVISO: {w}";
                ws2.Column(1).Width = 120;
            }

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        public byte[] BuildPostulantsTemplate()
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("BD_Postulantes");

            ws.Cell(1, 1).Value = "BD de postulantes - Plantilla de ejemplo";
            ws.Range(1, 1, 1, 6).Merge().Style
                .Font.SetBold(true).Font.SetFontSize(13)
                .Fill.SetBackgroundColor(XLColor.FromHtml("#1e3a8a"))
                .Font.SetFontColor(XLColor.White);

            var headers = new[] { "codigo", "Apellido_Nombre", "Carrera_Profesional", "modalidad", "tipo_modalidad", "tema" };
            for (int i = 0; i < headers.Length; i++)
            {
                var c = ws.Cell(3, i + 1);
                c.Value = headers[i];
                c.Style.Font.SetBold(true).Font.SetFontColor(XLColor.White)
                    .Fill.SetBackgroundColor(XLColor.FromHtml("#374151"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }

            ws.Cell(4, 1).Value = "20240001";
            ws.Cell(4, 2).Value = "GARCIA LOPEZ, JUAN CARLOS";
            ws.Cell(4, 3).Value = "INGENIERIA DE SISTEMAS";
            ws.Cell(4, 4).Value = "ORDINARIO";
            ws.Cell(4, 5).Value = "TERCER PUESTO";
            ws.Cell(4, 6).Value = "P";

            ws.Cell(6, 1).Value = "El codigo debe coincidir con el de la ficha optica (o con el LITHO si no usas el archivo de identificacion).";
            ws.Cell(7, 1).Value = "La columna 'tipo_modalidad' es opcional (p. ej. DEPORTISTA CALIFICADO, PRIMER PUESTO) y puede dejarse en blanco.";
            ws.Cell(8, 1).Value = "La columna 'tema' es opcional y se usa cuando el tema no viene en el archivo de respuestas.";
            ws.Range(6, 1, 8, 6).Style.Font.SetItalic(true).Font.SetFontSize(9).Font.SetFontColor(XLColor.FromHtml("#6b7280"));

            ws.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return ms.ToArray();
        }

        private static decimal GetCorrectaPoints(ExternalScoringParameters parameters, int numero)
        {
            var ranges = parameters.WeightedRanges;
            if (ranges != null && ranges.Count > 0)
            {
                foreach (var r in ranges)
                {
                    if (numero >= r.FromQuestion && numero <= r.ToQuestion)
                        return r.PuntosCorrecta;
                }
            }
            return parameters.PuntosCorrecta;
        }

        private static (List<string> Header, List<List<string>> Rows) ParseExcel(Stream stream)
        {
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();
            var header = new List<string>();
            var rows = new List<List<string>>();

            bool headerDone = false;
            foreach (var row in ws.RowsUsed())
            {
                var cells = row.CellsUsed().ToList();
                if (!headerDone)
                {
                    header = cells.Select(c => c.GetString().Trim()).ToList();
                    headerDone = true;
                    continue;
                }
                rows.Add(cells.Select(c => c.GetString().Trim()).ToList());
            }
            return (header, rows);
        }

        private static IdentificationColumnMap DetectIdentificationColumns(List<string> header)
        {
            int lithoIdx = -1, codigoIdx = -1, temaIdx = -1;
            for (int i = 0; i < header.Count; i++)
            {
                var h = Normalize(header[i]);
                if (h == "LITHO" || h == "LITOGRAFIA") lithoIdx = i;
                else if (h is "CODIGO" or "CÓDIGO" or "COD" or "COD. POSTULANTE" or "COD POSTULANTE" or "COD.POSTULANTE") codigoIdx = i;
                else if (h == "TEMA") temaIdx = i;
            }
            return new IdentificationColumnMap { LithoIdx = lithoIdx, CodigoIdx = codigoIdx, TemaIdx = temaIdx };
        }

        private static Dictionary<string, IdentInfo> ParseIdentification(Stream stream, List<string>? warnings)
        {
            var map = new Dictionary<string, IdentInfo>(StringComparer.OrdinalIgnoreCase);
            var (header, rows) = ParseCsv(stream);
            var cols = DetectIdentificationColumns(header);
            if (cols.LithoIdx < 0 && cols.CodigoIdx < 0)
            {
                warnings?.Add("Archivo de identificación: no se encontró columna LITHO ni CODIGO.");
                return map;
            }

            foreach (var row in rows)
            {
                string litho = cols.LithoIdx >= 0 && row.Count > cols.LithoIdx ? NormalizeLitho(row[cols.LithoIdx]) : "";
                string? codigo = null;
                if (cols.CodigoIdx >= 0 && row.Count > cols.CodigoIdx)
                {
                    var c = NormalizeLitho(row[cols.CodigoIdx]);
                    if (!string.IsNullOrEmpty(c)) codigo = c;
                }
                string? tema = cols.TemaIdx >= 0 && row.Count > cols.TemaIdx ? Normalize(row[cols.TemaIdx]) : null;
                if (!string.IsNullOrEmpty(litho) && !map.ContainsKey(litho))
                    map[litho] = new IdentInfo { Codigo = codigo, Tema = tema };
            }
            return map;
        }

        private static (List<string> Header, List<List<string>> Rows) ResolveBdHeader(List<string> header, List<List<string>> rows)
        {
            if (DetectBdColumns(header).CodigoIdx >= 0)
                return (header, rows);

            for (int i = 0; i < rows.Count; i++)
            {
                if (DetectBdColumns(rows[i]).CodigoIdx >= 0)
                    return (rows[i], rows.Skip(i + 1).ToList());
            }
            return (header, rows);
        }

        private static BdColumnMap DetectBdColumns(List<string> header)
        {
            int codigoIdx = -1, nombreIdx = -1, carreraIdx = -1, modalidadIdx = -1, tipoModalidadIdx = -1, temaIdx = -1;
            for (int i = 0; i < header.Count; i++)
            {
                var h = Normalize(header[i]);
                if (h is "CODIGO" or "CÓDIGO" or "COD" or "LITHO") codigoIdx = i;
                else if (h is "NOMBRE" or "APELLIDOS Y NOMBRES" or "NOMBRES" or "APELLIDOS NOMBRES" or "APELLIDO_NOMBRE" or "APELLIDOS_NOMBRE" or "APELLIDOS_NOMBRES") nombreIdx = i;
                else if (h is "CARRERA" or "CARRERA PROFESIONAL" or "CARRERA_PROFESIONAL") carreraIdx = i;
                else if (h is "MODALIDAD") modalidadIdx = i;
                else if (h is "TIPO MODALIDAD" or "TIPO_MODALIDAD" or "TIPO DE MODALIDAD") tipoModalidadIdx = i;
                else if (h is "TEMA") temaIdx = i;
            }
            return new BdColumnMap { CodigoIdx = codigoIdx, NombreIdx = nombreIdx, CarreraIdx = carreraIdx, ModalidadIdx = modalidadIdx, TipoModalidadIdx = tipoModalidadIdx, TemaIdx = temaIdx };
        }

        private static ColumnMap DetectColumns(List<string> header)
        {
            int temaIdx = -1, firstQuestionIdx = -1;
            for (int i = 0; i < header.Count; i++)
            {
                var h = Normalize(header[i]);
                if (h == "TEMA") { temaIdx = i; continue; }
                if (IsQuestionHeader(h))
                {
                    if (firstQuestionIdx < 0) firstQuestionIdx = i;
                }
            }
            return new ColumnMap { TemaIdx = temaIdx, FirstQuestionIdx = firstQuestionIdx };
        }

        private static bool IsQuestionHeader(string h)
        {
            h = h.Trim();
            if (string.IsNullOrEmpty(h)) return false;
            if (h.StartsWith("RESP", StringComparison.OrdinalIgnoreCase) && int.TryParse(h[4..], out _)) return true;
            if (h.StartsWith("R", StringComparison.OrdinalIgnoreCase) && int.TryParse(h[1..], out _)) return true;
            if (int.TryParse(h, out _)) return true;
            if (h.StartsWith("P", StringComparison.OrdinalIgnoreCase) && int.TryParse(h[1..], out _)) return true;
            return false;
        }

        private static string Normalize(string? s)
            => (s ?? "").Trim().ToUpperInvariant();

        private static string NormalizeLitho(string? s)
            => (s ?? "").Trim().Replace(" ", "").ToUpperInvariant();

        private static (List<string> Header, List<List<string>> Rows) ParseCsv(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var headerLine = reader.ReadLine();
            if (string.IsNullOrWhiteSpace(headerLine))
                return (new List<string>(), new List<List<string>>());

            char sep = DetectSeparator(headerLine);
            var header = SplitCsvLine(headerLine, sep);
            var rows = new List<List<string>>();
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                rows.Add(SplitCsvLine(line, sep));
            }
            return (header, rows);
        }

        private static char DetectSeparator(string line)
        {
            var candidates = new[] { ',', ';', '\t', '|' };
            char best = ',';
            int bestCount = -1;
            foreach (var c in candidates)
            {
                int count = 0;
                foreach (var ch in line)
                    if (ch == c) count++;
                if (count > bestCount) { bestCount = count; best = c; }
            }
            return best;
        }

        private static List<string> SplitCsvLine(string line, char sep)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            foreach (var c in line)
            {
                if (c == '"') { inQuotes = !inQuotes; continue; }
                if (c == sep && !inQuotes) { result.Add(sb.ToString()); sb.Clear(); continue; }
                sb.Append(c);
            }
            result.Add(sb.ToString());
            return result;
        }

        private record ColumnMap { public int TemaIdx { get; init; } = -1; public int FirstQuestionIdx { get; init; } = -1; }
        private record IdentificationColumnMap { public int LithoIdx { get; init; } = -1; public int CodigoIdx { get; init; } = -1; public int TemaIdx { get; init; } = -1; }
        private record IdentInfo { public string? Codigo { get; init; } public string? Tema { get; init; } }
        private record BdColumnMap { public int CodigoIdx { get; init; } = -1; public int NombreIdx { get; init; } = -1; public int CarreraIdx { get; init; } = -1; public int ModalidadIdx { get; init; } = -1; public int TipoModalidadIdx { get; init; } = -1; public int TemaIdx { get; init; } = -1; }
    }
}
