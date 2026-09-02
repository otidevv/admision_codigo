using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Integrations;
using ADMISION.Models.Shared;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class ExternalApiService : IExternalApiService
    {
        private const int ResponseExcerptLimit = 8 * 1024; // 8KB
        private const string PenalizadosApiName = "CONSULTA_PENALIZADOS";
        private static readonly Regex PlaceholderRegex = new(@"\{(?<key>[A-Za-z0-9_]+)\}", RegexOptions.Compiled);

        // Campos dentro de la respuesta JSON cuyo valor es un estado del estudiante.
        private static readonly HashSet<string> StatusCandidateKeys = new(StringComparer.Ordinal)
        {
            "studentstatus", "estadoestudiante", "estadodelestudiante", "condicionestudiante",
            "estado", "estatus", "situacionestudiante", "situacion"
        };

        // Campos dentro de la respuesta JSON cuyo valor es un nombre de carrera.
        private static readonly HashSet<string> CareerCandidateKeys = new(StringComparer.Ordinal)
        {
            "carrername", "careername", "carrer", "career", "carrera", "programa",
            "programaestudios", "nombrecarrera", "escuelaprofesional"
        };

        private readonly HttpClient _http;
        private readonly AppDbContext _context;
        private readonly ILogger<ExternalApiService> _logger;

        public ExternalApiService(HttpClient http, AppDbContext context, ILogger<ExternalApiService> logger)
        {
            _http = http;
            _context = context;
            _logger = logger;
        }

        public async Task<ApiInvocationResult> InvokeAsync(
            Guid apiId,
            IDictionary<string, string?> parameters,
            ClaimsPrincipal user,
            string? remoteIp,
            CancellationToken ct = default)
        {
            var api = await _context.ExternalApis.AsNoTracking().FirstOrDefaultAsync(a => a.Id == apiId, ct);
            if (api == null)
            {
                throw new InvalidOperationException($"API '{apiId}' no encontrada.");
            }
            if (!api.IsActive)
            {
                throw new InvalidOperationException("La API solicitada está inactiva.");
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = user.Identity?.Name ?? "Anonymous";

            var stopwatch = Stopwatch.StartNew();
            var result = new ApiInvocationResult();

            string? rawResponse = null;
            int statusCode = 0;
            string? errorMessage = null;
            bool success = false;

            try
            {
                using var request = BuildRequest(api, parameters);
                using var response = await _http.SendAsync(request, ct);
                statusCode = (int)response.StatusCode;
                rawResponse = await response.Content.ReadAsStringAsync(ct);
                success = response.IsSuccessStatusCode;

                if (success)
                {
                    result.Rows = ExtractRows(rawResponse, api.ResponseFieldsJson);
                }
                else
                {
                    errorMessage = $"HTTP {statusCode}";
                }
            }
            catch (TaskCanceledException ex)
            {
                errorMessage = "Tiempo de espera agotado";
                _logger.LogWarning(ex, "Timeout invocando API {ApiId} ({Name})", api.Id, api.Name);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                _logger.LogWarning(ex, "Error invocando API {ApiId} ({Name})", api.Id, api.Name);
            }

            stopwatch.Stop();
            result.Success = success;
            result.StatusCode = statusCode;
            result.RawResponse = rawResponse;
            result.Error = errorMessage;
            result.DurationMs = (int)stopwatch.ElapsedMilliseconds;

            // Auditoría — el log se crea siempre, exitoso o no.
            var log = new ApiQueryLog
            {
                Id = Guid.NewGuid(),
                ApiId = api.Id,
                UserId = userId,
                UserName = userName,
                IpAddress = remoteIp,
                RequestParametersJson = SafeSerializeParameters(parameters),
                ResponseStatus = statusCode,
                ResponseSuccess = success,
                ResponseExcerpt = TrimExcerpt(rawResponse),
                ErrorMessage = errorMessage,
                DurationMs = result.DurationMs,
                QueriedAt = DateTimeOffset.UtcNow
            };
            _context.ApiQueryLogs.Add(log);
            await _context.SaveChangesAsync(ct);

            result.LogId = log.Id;
            return result;
        }

        private HttpRequestMessage BuildRequest(ExternalApi api, IDictionary<string, string?> parameters)
        {
            var resolvedUrl = ReplacePlaceholders(api.Url, parameters);
            var method = new HttpMethod(string.IsNullOrWhiteSpace(api.HttpMethod) ? "GET" : api.HttpMethod.ToUpperInvariant());

            var request = new HttpRequestMessage(method, resolvedUrl);

            // Headers definidos por el admin
            if (!string.IsNullOrWhiteSpace(api.HeadersJson))
            {
                try
                {
                    var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(api.HeadersJson);
                    if (headers != null)
                    {
                        foreach (var (k, v) in headers)
                        {
                            // Algunos headers (Content-Type) deben ir en el contenido; los demás en request.Headers.
                            if (string.Equals(k, "Content-Type", StringComparison.OrdinalIgnoreCase))
                            {
                                continue; // se aplica al construir el body
                            }
                            request.Headers.TryAddWithoutValidation(k, v);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "HeadersJson inválido para API {ApiId}", api.Id);
                }
            }

            // Autenticación
            ApplyAuth(request, api);

            // Body para POST/PUT/PATCH
            if (HttpMethodHasBody(method))
            {
                var body = BuildBody(api, parameters);
                var contentType = GetContentTypeFromHeaders(api.HeadersJson) ?? "application/json";
                request.Content = new StringContent(body, Encoding.UTF8, contentType);
            }

            return request;
        }

        private static void ApplyAuth(HttpRequestMessage request, ExternalApi api)
        {
            if (string.IsNullOrWhiteSpace(api.AuthValue)) return;

            switch ((api.AuthType ?? "None").Trim())
            {
                case "Bearer":
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", api.AuthValue);
                    break;
                case "Basic":
                    var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(api.AuthValue));
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encoded);
                    break;
                case "ApiKey":
                    var headerName = string.IsNullOrWhiteSpace(api.AuthHeaderName) ? "X-Api-Key" : api.AuthHeaderName!;
                    request.Headers.TryAddWithoutValidation(headerName, api.AuthValue);
                    break;
            }
        }

        private static string BuildBody(ExternalApi api, IDictionary<string, string?> parameters)
        {
            if (!string.IsNullOrWhiteSpace(api.RequestBodyTemplate))
            {
                return ReplacePlaceholders(api.RequestBodyTemplate!, parameters);
            }
            // Sin plantilla → enviar todos los parámetros como JSON simple.
            var dict = parameters.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
            return JsonSerializer.Serialize(dict);
        }

        private static bool HttpMethodHasBody(HttpMethod method) =>
            method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch;

        private static string? GetContentTypeFromHeaders(string? headersJson)
        {
            if (string.IsNullOrWhiteSpace(headersJson)) return null;
            try
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
                if (headers == null) return null;
                foreach (var (k, v) in headers)
                {
                    if (string.Equals(k, "Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        // El charset se añade después en StringContent; aquí devolvemos solo el media type.
                        var semicolon = v.IndexOf(';');
                        return semicolon > 0 ? v[..semicolon].Trim() : v.Trim();
                    }
                }
            }
            catch (JsonException) { /* ignorar — los headers se loggean al construir el request */ }
            return null;
        }

        private static string ReplacePlaceholders(string template, IDictionary<string, string?> parameters)
        {
            return PlaceholderRegex.Replace(template, m =>
            {
                var key = m.Groups["key"].Value;
                return parameters.TryGetValue(key, out var value) ? Uri.EscapeDataString(value ?? string.Empty) : m.Value;
            });
        }

        private static IList<ApiResultRow> ExtractRows(string? rawResponse, string? responseFieldsJson)
        {
            if (string.IsNullOrWhiteSpace(rawResponse)) return new List<ApiResultRow>();

            JsonDocument? doc = null;
            try
            {
                doc = JsonDocument.Parse(rawResponse);
            }
            catch (JsonException)
            {
                // No es JSON válido — devolver el texto crudo como fila.
                return new List<ApiResultRow>
                {
                    new() { Label = "Respuesta", Value = rawResponse }
                };
            }

            try
            {
                var rows = new List<ApiResultRow>();

                if (!string.IsNullOrWhiteSpace(responseFieldsJson))
                {
                    // Mapeo manual por campo configurado.
                    var fields = JsonSerializer.Deserialize<List<ResponseField>>(responseFieldsJson);
                    if (fields != null)
                    {
                        foreach (var field in fields)
                        {
                            var value = ResolvePath(doc.RootElement, field.Path);
                            rows.Add(new ApiResultRow
                            {
                                Label = string.IsNullOrWhiteSpace(field.Label) ? field.Path : field.Label,
                                Value = value ?? string.Empty
                            });
                        }
                        return rows;
                    }
                }

                // Auto-flatten del primer nivel del JSON.
                FlattenInto(doc.RootElement, prefix: string.Empty, rows, depth: 0, maxDepth: 2);
                return rows;
            }
            finally
            {
                doc.Dispose();
            }
        }

        private static void FlattenInto(JsonElement element, string prefix, List<ApiResultRow> rows, int depth, int maxDepth)
        {
            if (depth > maxDepth)
            {
                rows.Add(new ApiResultRow { Label = prefix, Value = element.ToString() });
                return;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        var label = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                        if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            FlattenInto(prop.Value, label, rows, depth + 1, maxDepth);
                        }
                        else
                        {
                            rows.Add(new ApiResultRow { Label = label, Value = JsonValueToString(prop.Value) });
                        }
                    }
                    break;
                case JsonValueKind.Array:
                    int i = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        var label = $"{prefix}[{i}]";
                        if (item.ValueKind == JsonValueKind.Object || item.ValueKind == JsonValueKind.Array)
                        {
                            FlattenInto(item, label, rows, depth + 1, maxDepth);
                        }
                        else
                        {
                            rows.Add(new ApiResultRow { Label = label, Value = JsonValueToString(item) });
                        }
                        i++;
                    }
                    break;
                default:
                    rows.Add(new ApiResultRow
                    {
                        Label = string.IsNullOrEmpty(prefix) ? "valor" : prefix,
                        Value = JsonValueToString(element)
                    });
                    break;
            }
        }

        private static string? ResolvePath(JsonElement root, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            JsonElement current = root;
            foreach (var rawSeg in segments)
            {
                var seg = rawSeg;
                int? arrayIndex = null;
                var bracket = seg.IndexOf('[');
                if (bracket > 0 && seg.EndsWith(']'))
                {
                    var idxStr = seg.Substring(bracket + 1, seg.Length - bracket - 2);
                    if (int.TryParse(idxStr, out var idx)) arrayIndex = idx;
                    seg = seg[..bracket];
                }

                if (current.ValueKind != JsonValueKind.Object) return null;
                if (!current.TryGetProperty(seg, out var next)) return null;
                current = next;

                if (arrayIndex.HasValue)
                {
                    if (current.ValueKind != JsonValueKind.Array) return null;
                    if (arrayIndex.Value < 0 || arrayIndex.Value >= current.GetArrayLength()) return null;
                    current = current[arrayIndex.Value];
                }
            }
            return JsonValueToString(current);
        }

        private static string JsonValueToString(JsonElement v) => v.ValueKind switch
        {
            JsonValueKind.String => v.GetString() ?? string.Empty,
            JsonValueKind.Number => v.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Undefined => string.Empty,
            _ => v.GetRawText()
        };

        private static string SafeSerializeParameters(IDictionary<string, string?> parameters)
        {
            try { return JsonSerializer.Serialize(parameters); }
            catch { return "{}"; }
        }

        private static string? TrimExcerpt(string? raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            return raw.Length <= ResponseExcerptLimit ? raw : raw[..ResponseExcerptLimit] + "…";
        }

        private sealed class ResponseField
        {
            public string Path { get; set; } = string.Empty;
            public string? Label { get; set; }
        }

        // ════════════════════════════════════════════════════════════════
        //  Admin CRUD + Logs (movido del controller).
        // ════════════════════════════════════════════════════════════════

        public async Task<IReadOnlyList<ExternalApi>> GetAllAsync(CancellationToken ct = default)
        {
            return await _context.ExternalApis
                .AsNoTracking()
                .OrderBy(a => a.Name)
                .ToListAsync(ct);
        }

        public Task<ExternalApi?> GetByIdAsync(Guid id, bool tracking = false, CancellationToken ct = default)
        {
            var q = tracking ? _context.ExternalApis.AsQueryable() : _context.ExternalApis.AsNoTracking();
            return q.FirstOrDefaultAsync(a => a.Id == id, ct);
        }

        public async Task<SaveResult> CreateAsync(ExternalApi model, string actor, CancellationToken ct = default)
        {
            var errors = ValidateModel(model);
            if (errors.Any()) return SaveResult.Invalid(errors);

            model.Id = Guid.NewGuid();
            model.CreatedAt = DateTimeOffset.UtcNow;
            model.CreatedBy = actor;
            _context.ExternalApis.Add(model);
            await _context.SaveChangesAsync(ct);
            return SaveResult.Ok();
        }

        public async Task<SaveResult> UpdateAsync(ExternalApi model, string actor, CancellationToken ct = default)
        {
            var existing = await _context.ExternalApis.FirstOrDefaultAsync(a => a.Id == model.Id, ct);
            if (existing == null) return SaveResult.NotFoundResult();

            var errors = ValidateModel(model, includeRequiredChecks: false);
            if (errors.Any()) return SaveResult.Invalid(errors);

            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.Category = model.Category;
            existing.HttpMethod = model.HttpMethod;
            existing.Url = model.Url;
            existing.AuthType = model.AuthType;
            existing.AuthHeaderName = model.AuthHeaderName;
            // El secreto solo se actualiza si se escribió uno nuevo (placeholder vacío conserva el anterior).
            if (!string.IsNullOrWhiteSpace(model.AuthValue))
            {
                existing.AuthValue = model.AuthValue;
            }
            existing.RequestParametersJson = model.RequestParametersJson;
            existing.HeadersJson = model.HeadersJson;
            existing.RequestBodyTemplate = model.RequestBodyTemplate;
            existing.ResponseFieldsJson = model.ResponseFieldsJson;
            existing.IsActive = model.IsActive;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            existing.UpdatedBy = actor;

            await _context.SaveChangesAsync(ct);
            return SaveResult.Ok();
        }

        public async Task<ExternalApiDeleteOutcome> DeleteAsync(Guid id, string actor, CancellationToken ct = default)
        {
            var api = await _context.ExternalApis.FirstOrDefaultAsync(a => a.Id == id, ct);
            if (api == null) return ExternalApiDeleteOutcome.NotFound;

            // Soft-delete cuando hay logs registrados — preserva la auditoría.
            var hasLogs = await _context.ApiQueryLogs.AnyAsync(l => l.ApiId == id, ct);
            if (hasLogs)
            {
                api.IsActive = false;
                api.UpdatedAt = DateTimeOffset.UtcNow;
                api.UpdatedBy = actor;
                await _context.SaveChangesAsync(ct);
                return ExternalApiDeleteOutcome.SoftDeleted;
            }

            _context.ExternalApis.Remove(api);
            await _context.SaveChangesAsync(ct);
            return ExternalApiDeleteOutcome.Deleted;
        }

        public async Task<PagedResult<ApiQueryLog>> GetLogsAsync(Guid? apiId, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _context.ApiQueryLogs
                .AsNoTracking()
                .Include(l => l.Api)
                .AsQueryable();

            if (apiId.HasValue)
                query = query.Where(l => l.ApiId == apiId.Value);

            query = query.OrderByDescending(l => l.QueriedAt);
            return await PagedResult<ApiQueryLog>.CreateAsync(query, page, pageSize, ct);
        }

        public Task<ApiQueryLog?> GetLogByIdAsync(Guid logId, CancellationToken ct = default)
            => _context.ApiQueryLogs.AsNoTracking().FirstOrDefaultAsync(l => l.Id == logId, ct);

        public async Task SaveAcademicInfoAsync(IEnumerable<ExternalAcademicInfo> records, CancellationToken ct = default)
        {
            _context.ExternalAcademicInfos.AddRange(records);
            await _context.SaveChangesAsync(ct);
        }

        public async Task SavePaymentVouchersAsync(IEnumerable<ExternalPaymentVoucher> vouchers, CancellationToken ct = default)
        {
            _context.ExternalPaymentVouchers.AddRange(vouchers);
            await _context.SaveChangesAsync(ct);
        }

        // ════════════════════════════════════════════════════════════════
        //  Lectura de datos persistidos
        // ════════════════════════════════════════════════════════════════

        public async Task<IReadOnlyList<ExternalAcademicInfo>> GetAcademicInfoByDniAsync(string dni, CancellationToken ct = default)
        {
            return await _context.ExternalAcademicInfos
                .AsNoTracking()
                .Where(e => e.Dni == dni)
                .OrderBy(e => e.CareerName)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<ExternalPaymentVoucher>> GetPaymentVouchersByDniAsync(string dni, CancellationToken ct = default)
        {
            return await _context.ExternalPaymentVouchers
                .AsNoTracking()
                .Include(v => v.Payments)
                .Where(v => v.UserName == dni)
                .OrderByDescending(v => v.QueriedAt)
                .ToListAsync(ct);
        }

        // ════════════════════════════════════════════════════════════════
        //  Búsqueda de API por categoría
        // ════════════════════════════════════════════════════════════════

        public Task<ExternalApi?> FindApiByCategoryAsync(string category, CancellationToken ct = default)
        {
            return _context.ExternalApis
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.IsActive && a.Category == category, ct);
        }

        // ════════════════════════════════════════════════════════════════
        //  Verificación de penalizados (pre-inscripción)
        // ════════════════════════════════════════════════════════════════

        public async Task<SanctionCheckResult> CheckSanctionsAsync(
            string dni,
            string inscribingCareerName,
            string? remoteIp,
            CancellationToken ct = default)
        {
            var result = new SanctionCheckResult();

            var api = await _context.ExternalApis
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.IsActive && a.Name == PenalizadosApiName, ct);

            if (api == null)
            {
                result.Error = $"API '{PenalizadosApiName}' no configurada o inactiva.";
                return result;
            }

            var parameters = new Dictionary<string, string?> { ["dni"] = dni };
            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());

            ApiInvocationResult invocation;
            try
            {
                invocation = await InvokeAsync(api.Id, parameters, anonymousUser, remoteIp, ct);
            }
            catch (Exception ex)
            {
                result.Error = $"Error al invocar la API de penalizados: {ex.Message}";
                _logger.LogWarning(ex, "Error invocando API de penalizados para DNI {Dni}", dni);
                return result;
            }

            result.RawResponse = invocation.RawResponse;

            if (!invocation.Success || string.IsNullOrWhiteSpace(invocation.RawResponse))
            {
                result.Error = invocation.Error ?? "La API de penalizados devolvió una respuesta vacía.";
                return result;
            }

            JsonDocument? doc = null;
            try
            {
                doc = JsonDocument.Parse(invocation.RawResponse);
            }
            catch (JsonException)
            {
                result.Error = "La API de penalizados devolvió una respuesta no parseable.";
                return result;
            }

            using (doc)
            {
                var records = CollectStudentRecords(doc.RootElement);
                if (records.Count == 0)
                {
                    result.Error = "No se detectaron registros de estudiante (estado/carrera) en la respuesta de la API.";
                    return result;
                }
                result.Records.AddRange(records);

                // 1) Estado bloqueante (Sancionado / Expulsado) → Dirección de Admisión.
                //    Se evalúa cada registro por separado: si uno de los registros del DNI
                //    (por ej. una de sus dos carreras) figura con estado bloqueante, se bloquea.
                var blocking = records.FirstOrDefault(r => IsBlockingStatus(r.StudentStatus));
                if (blocking != null)
                {
                    result.StudentStatus = blocking.StudentStatus!.Trim();
                    result.StudentCareer = string.IsNullOrWhiteSpace(blocking.CareerName) ? null : blocking.CareerName.Trim();
                    result.Blocked = true;
                    result.Message =
                        "No es posible registrar tu inscripción porque actualmente figuras como " +
                        $"{result.StudentStatus.ToUpperInvariant()} en los registros académicos. " +
                        "Comunícate con la Dirección de Admisión para más información.";
                    return result;
                }

                // 2) Coincidencia con la carrera a la que postula → administración.
                if (!string.IsNullOrWhiteSpace(inscribingCareerName))
                {
                    var normCareer = NormalizeName(inscribingCareerName);
                    var match = records.FirstOrDefault(r =>
                        !string.IsNullOrWhiteSpace(r.CareerName) && NormalizeName(r.CareerName) == normCareer);

                    if (match != null)
                    {
                        result.CareerMatch = true;
                        result.CareerName = match.CareerName!.Trim();
                        result.Blocked = true;
                        result.Message =
                            "No es posible registrar tu inscripción porque ya figuras registrado(a) en " +
                            $"la carrera «{result.CareerName}» a la que estás postulando. " +
                            "Consulta con la administración para más información.";
                        return result;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Considera bloqueante un estado si, normalizado (sin tildes, minúsculas), empieza
        /// por "sancion" o "expuls": cubre "Sancionado", "Sanción", "Expulsado", "Expulsión", etc.
        /// No dispara con valores tipo "Invicto" o "No registra sanciones".
        /// </summary>
        private static bool IsBlockingStatus(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var norm = NormalizeName(value);
            return norm.StartsWith("sancion", StringComparison.Ordinal)
                || norm.StartsWith("expuls", StringComparison.Ordinal);
        }

        /// <summary>
        /// Recorre la respuesta (objetos y arreglos, cualquier profundidad) y produce un registro
        /// por cada objeto JSON que exponga un estado (StudentStatus) o un nombre de carrera
        /// (CarrerName/CareerName/carrera/...). Así soporta tanto un único "infoStudent" como
        /// varios registros del mismo DNI (dos carreras) o formatos anidados.
        /// </summary>
        private static List<StudentSanctionRecord> CollectStudentRecords(JsonElement element)
        {
            var records = new List<StudentSanctionRecord>();
            CollectStudentRecordsCore(element, records);
            return records;
        }

        private static void CollectStudentRecordsCore(JsonElement element, ICollection<StudentSanctionRecord> records)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                string? status = null;
                string? career = null;
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.Value.ValueKind != JsonValueKind.String) continue;

                    var key = NormalizeKey(prop.Name);
                    var val = prop.Value.GetString()?.Trim();
                    if (string.IsNullOrWhiteSpace(val)) continue;

                    if (status == null && StatusCandidateKeys.Contains(key)) status = val;
                    else if (career == null && CareerCandidateKeys.Contains(key)) career = val;
                }

                if (status != null || career != null)
                {
                    records.Add(new StudentSanctionRecord { StudentStatus = status, CareerName = career });
                }

                foreach (var prop in element.EnumerateObject())
                    CollectStudentRecordsCore(prop.Value, records);
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                    CollectStudentRecordsCore(item, records);
            }
        }

        /// <summary>Reduce una clave JSON a minúsculas y sin guiones/espacios para compararla.</summary>
        private static string NormalizeKey(string key)
        {
            var sb = new StringBuilder(key.Length);
            foreach (var ch in key)
            {
                if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Normaliza un nombre para comparación: sin tildes (FormD + quitar marcas),
        /// espacios colapsados y minúsculas.
        /// </summary>
        private static string NormalizeName(string value)
        {
            var decomposed = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            foreach (var ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsWhiteSpace(ch))
                {
                    if (sb.Length > 0 && sb[^1] != ' ') sb.Append(' ');
                }
                else
                {
                    sb.Append(char.ToLowerInvariant(ch));
                }
            }
            return sb.ToString().Trim();
        }

        // ════════════════════════════════════════════════════════════════
        //  Fetch + upsert académico
        // ════════════════════════════════════════════════════════════════

        public async Task<AcademicFetchResult> FetchAndSaveAcademicAsync(
            Guid apiId, string dni, ClaimsPrincipal user, string? remoteIp, CancellationToken ct = default)
        {
            var result = new AcademicFetchResult();
            var parameters = new Dictionary<string, string?> { ["dni"] = dni };

            try
            {
                var invocation = await InvokeAsync(apiId, parameters, user, remoteIp, ct);
                result.LogId = invocation.LogId;

                if (!invocation.Success || string.IsNullOrWhiteSpace(invocation.RawResponse))
                {
                    result.Error = invocation.Error ?? "Respuesta vacía de la API.";
                    return result;
                }

                var parsed = ParseAcademicResponse(invocation.RawResponse);
                if (parsed == null)
                {
                    result.Error = "No se pudo interpretar la respuesta académica.";
                    return result;
                }

                using var doc = JsonDocument.Parse(invocation.RawResponse);
                if (!doc.RootElement.TryGetProperty("data", out var dataArr) || dataArr.ValueKind != JsonValueKind.Array)
                {
                    result.Error = "Estructura 'data' no encontrada en la respuesta.";
                    return result;
                }

                var newRecords = new List<ExternalAcademicInfo>();
                foreach (var item in dataArr.EnumerateArray())
                {
                    if (!item.TryGetProperty("info", out var info)) continue;

                    var careerName = GetJsonString(info, "carrerName");
                    if (string.IsNullOrWhiteSpace(careerName)) continue;

                    var record = new ExternalAcademicInfo
                    {
                        Id = Guid.NewGuid(),
                        ExternalApiId = apiId,
                        QueryLogId = invocation.LogId,
                        Dni = dni,
                        UserName = GetJsonString(info, "username"),
                        Name = GetJsonString(info, "name"),
                        PaternalSurname = GetJsonString(info, "paternalSurname"),
                        MaternalSurname = GetJsonString(info, "maternalSurname"),
                        Email = GetJsonStringOrNull(info, "email"),
                        PersonalEmail = GetJsonStringOrNull(info, "personalEmail"),
                        CareerName = careerName,
                        FacultyName = GetJsonString(info, "facultyName"),
                        TotalCreditsApproved = GetJsonDecimal(item, "totalCreditsApproved"),
                        QueriedAt = DateTimeOffset.UtcNow
                    };
                    newRecords.Add(record);
                }

                if (newRecords.Count == 0)
                {
                    result.Error = "No se encontraron registros académicos en la respuesta.";
                    return result;
                }

                // Upsert: update existing by DNI+CareerName, insert new
                var existingDnis = await _context.ExternalAcademicInfos
                    .Where(e => e.Dni == dni)
                    .ToListAsync(ct);

                var saved = 0;
                foreach (var record in newRecords)
                {
                    var existing = existingDnis.FirstOrDefault(e =>
                        e.Dni == record.Dni && e.CareerName == record.CareerName);

                    if (existing != null)
                    {
                        existing.TotalCreditsApproved = record.TotalCreditsApproved;
                        existing.FacultyName = record.FacultyName;
                        existing.Name = record.Name;
                        existing.PaternalSurname = record.PaternalSurname;
                        existing.MaternalSurname = record.MaternalSurname;
                        existing.Email = record.Email;
                        existing.PersonalEmail = record.PersonalEmail;
                        existing.QueryLogId = record.QueryLogId;
                        existing.QueriedAt = record.QueriedAt;
                        saved++;
                    }
                    else
                    {
                        _context.ExternalAcademicInfos.Add(record);
                        saved++;
                    }
                }

                await _context.SaveChangesAsync(ct);

                result.Success = true;
                result.Count = saved;
                result.Records = newRecords.ToList();
                return result;
            }
            catch (InvalidOperationException ex)
            {
                result.Error = ex.Message;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching academic data for DNI {Dni}", dni);
                result.Error = "Error inesperado al consultar datos académicos.";
                return result;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  Fetch + insert-only de pagos
        // ════════════════════════════════════════════════════════════════

        public async Task<PaymentFetchResult> FetchAndSavePaymentsAsync(
            Guid apiId, string dni, ClaimsPrincipal user, string? remoteIp, CancellationToken ct = default)
        {
            var result = new PaymentFetchResult();
            var parameters = new Dictionary<string, string?> { ["dni"] = dni };

            try
            {
                var invocation = await InvokeAsync(apiId, parameters, user, remoteIp, ct);
                result.LogId = invocation.LogId;

                if (!invocation.Success || string.IsNullOrWhiteSpace(invocation.RawResponse))
                {
                    result.Error = invocation.Error ?? "Respuesta vacía de la API.";
                    return result;
                }

                using var doc = JsonDocument.Parse(invocation.RawResponse);
                var root = doc.RootElement;

                JsonElement.ArrayEnumerator voucherEnumerator;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    voucherEnumerator = root.EnumerateArray();
                }
                else if (root.TryGetProperty("data", out var innerArr) && innerArr.ValueKind == JsonValueKind.Array)
                {
                    voucherEnumerator = innerArr.EnumerateArray();
                }
                else
                {
                    result.Error = "Estructura de respuesta no reconocida (se esperaba un array).";
                    return result;
                }

                var existingSerialVouchers = await _context.ExternalPaymentVouchers
                    .AsNoTracking()
                    .Where(v => v.UserName == dni)
                    .Select(v => v.SerialVoucher)
                    .ToListAsync(ct);

                var existingSet = new HashSet<string>(existingSerialVouchers);

                var voucherEntities = new List<ExternalPaymentVoucher>();
                int totalPayments = 0;

                foreach (var voucher in voucherEnumerator)
                {
                    var serial = GetJsonString(voucher, "serial_voucher");
                    if (string.IsNullOrWhiteSpace(serial)) continue;
                    if (existingSet.Contains(serial)) continue;

                    var ve = new ExternalPaymentVoucher
                    {
                        Id = Guid.NewGuid(),
                        ExternalApiId = apiId,
                        QueryLogId = invocation.LogId,
                        SerialVoucher = serial,
                        UserName = GetJsonString(voucher, "userName"),
                        FullName = GetJsonString(voucher, "fullName"),
                        QueriedAt = DateTimeOffset.UtcNow,
                    };

                    // Parse payments array inside each voucher
                    if (voucher.TryGetProperty("payments", out var pArr) && pArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var p in pArr.EnumerateArray())
                        {
                            ve.Payments.Add(new ExternalPaymentDetail
                            {
                                Id = Guid.NewGuid(),
                                VoucherId = ve.Id,
                                Description = GetJsonString(p, "description"),
                                SubTotal = GetJsonDecimal(p, "subTotal"),
                                Discount = GetJsonDecimal(p, "discount"),
                                Total = GetJsonDecimal(p, "total"),
                                TypeUser = GetJsonStringOrNull(p, "type_user"),
                                Quantity = GetJsonDecimal(p, "quantity"),
                                Status = GetJsonInt(p, "status"),
                                PaymentDate = TryParseDateTime(GetJsonStringOrNull(p, "paymentDate")),
                                CreatedBy = GetJsonStringOrNull(p, "createdBy"),
                                IsBankPayment = GetJsonBool(p, "isBankPayment"),
                                Name = GetJsonStringOrNull(p, "name"),
                                ActiveDependency = GetJsonBool(p, "activeDependency"),
                                Acronym = GetJsonStringOrNull(p, "acronym"),
                                Cashier = GetJsonStringOrNull(p, "cashier"),
                                TermName = GetJsonStringOrNull(p, "termName"),
                                AmountInWords = GetJsonStringOrNull(p, "amountInWords")
                            });
                            totalPayments++;
                        }
                    }

                    voucherEntities.Add(ve);
                    existingSet.Add(serial);
                }

                if (voucherEntities.Count > 0)
                {
                    _context.ExternalPaymentVouchers.AddRange(voucherEntities);
                    await _context.SaveChangesAsync(ct);
                }

                result.Success = true;
                result.VouchersCount = voucherEntities.Count;
                result.PaymentsCount = totalPayments;
                result.Records = voucherEntities.ToList();
                return result;
            }
            catch (InvalidOperationException ex)
            {
                result.Error = ex.Message;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching payment data for DNI {Dni}", dni);
                result.Error = "Error inesperado al consultar datos de pagos.";
                return result;
            }
        }

        // ───────── Parseo JSON helpers ─────────
        private static string GetJsonString(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? string.Empty : string.Empty;

        private static string? GetJsonStringOrNull(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        private static decimal GetJsonDecimal(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : 0;

        private static int GetJsonInt(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) ? v.GetInt32() : 0;

        private static bool GetJsonBool(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.True;

        private static DateTime? TryParseDateTime(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            if (DateTime.TryParse(value, out var dt))
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return null;
        }

        private static object? ParseAcademicResponse(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                if (!root.TryGetProperty("data", out var dataArr) || dataArr.ValueKind != JsonValueKind.Array)
                    return null;

                var items = new List<object>();
                foreach (var item in dataArr.EnumerateArray())
                {
                    if (!item.TryGetProperty("info", out var info)) continue;

                    items.Add(new
                    {
                        userName = GetJsonString(info, "username"),
                        dni = GetJsonString(info, "dni"),
                        name = GetJsonString(info, "name"),
                        paternalSurname = GetJsonString(info, "paternalSurname"),
                        maternalSurname = GetJsonString(info, "maternalSurname"),
                        email = GetJsonString(info, "email"),
                        personalEmail = GetJsonString(info, "personalEmail"),
                        careerName = GetJsonString(info, "carrerName"),
                        facultyName = GetJsonString(info, "facultyName"),
                        totalCreditsApproved = GetJsonDecimal(item, "totalCreditsApproved")
                    });
                }

                return new
                {
                    message = GetJsonString(root, "message"),
                    items
                };
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // ───────── Validación reutilizable ─────────
        private static List<ValidationError> ValidateModel(ExternalApi model, bool includeRequiredChecks = true)
        {
            var errors = new List<ValidationError>();

            if (includeRequiredChecks)
            {
                if (string.IsNullOrWhiteSpace(model.Name))
                    errors.Add(new ValidationError(nameof(ExternalApi.Name), "El nombre es obligatorio."));
                if (string.IsNullOrWhiteSpace(model.Url))
                    errors.Add(new ValidationError(nameof(ExternalApi.Url), "La URL es obligatoria."));
            }

            return errors;
        }
    }
}
