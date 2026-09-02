using System.Security.Claims;
using System.Text.Json;
using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Models.Integrations;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace admision.Controllers.Admin.ConfigController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin)]
    [Route("admin/consultas")]
    public class PersonQueriesController : Controller
    {
        private readonly IExternalApiService _apis;
        private readonly ILogger<PersonQueriesController> _logger;

        public PersonQueriesController(IExternalApiService apis, ILogger<PersonQueriesController> logger)
        {
            _apis = apis;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var all = await _apis.GetAllAsync(ct);
            var personApis = all.Where(a => a.IsActive && (a.Category == "Academic" || a.Category == "Payment")).ToList();
            return View("~/Pages/Admin/Config/PersonQueries/Index.cshtml", personApis);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Query(Guid id, CancellationToken ct)
        {
            var api = await _apis.GetByIdAsync(id, tracking: false, ct);
            if (api == null || !api.IsActive) return NotFound();
            if (api.Category != "Academic" && api.Category != "Payment")
                return BadRequest("Esta API no es de tipo consulta por DNI.");
            return View("~/Pages/Admin/Config/PersonQueries/Query.cshtml", api);
        }

        [HttpPost("{id:guid}/ejecutar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Execute(Guid id, [FromForm] string dni, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dni))
                return BadRequest(new { success = false, message = "El DNI es obligatorio." });

            try
            {
                var api = await _apis.GetByIdAsync(id, tracking: false, ct);
                if (api == null || !api.IsActive)
                    return BadRequest(new { success = false, message = "API no encontrada o inactiva." });

                var parameters = new Dictionary<string, string?> { ["dni"] = dni };
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _apis.InvokeAsync(id, parameters, User, ip, ct);

                if (!result.Success || string.IsNullOrWhiteSpace(result.RawResponse))
                {
                    return Ok(new
                    {
                        success = false,
                        statusCode = result.StatusCode,
                        error = result.Error,
                        logId = result.LogId,
                        durationMs = result.DurationMs
                    });
                }

                object? structuredData = null;
                if (api.Category == "Academic")
                    structuredData = ParseAcademicResponse(result.RawResponse);
                else if (api.Category == "Payment")
                    structuredData = ParsePaymentResponse(result.RawResponse);

                return Ok(new
                {
                    success = true,
                    statusCode = result.StatusCode,
                    durationMs = result.DurationMs,
                    logId = result.LogId,
                    category = api.Category,
                    data = structuredData,
                    raw = result.RawResponse
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo ejecutando consulta DNI para API {ApiId}", id);
                return StatusCode(500, new { success = false, message = "Error inesperado al consultar." });
            }
        }

        // ───────── Parseo de respuesta académica ─────────
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
                        userName = GetString(info, "username"),
                        dni = GetString(info, "dni"),
                        name = GetString(info, "name"),
                        paternalSurname = GetString(info, "paternalSurname"),
                        maternalSurname = GetString(info, "maternalSurname"),
                        email = GetString(info, "email"),
                        personalEmail = GetString(info, "personalEmail"),
                        careerName = GetString(info, "carrerName"),
                        facultyName = GetString(info, "facultyName"),
                        totalCreditsApproved = GetDecimal(item, "totalCreditsApproved")
                    });
                }

                return new
                {
                    message = GetString(root, "message"),
                    items
                };
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // ───────── Parseo de respuesta de pagos ─────────
        private static object? ParsePaymentResponse(string raw)
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Array)
                {
                    // maybe wrapped in { data: [...] }
                    if (root.TryGetProperty("data", out var innerArr) && innerArr.ValueKind == JsonValueKind.Array)
                        root = innerArr;
                    else
                        return null;
                }

                var vouchers = new List<object>();
                foreach (var voucher in root.EnumerateArray())
                {
                    var paymentsArr = voucher.TryGetProperty("payments", out var pArr) && pArr.ValueKind == JsonValueKind.Array
                        ? pArr.EnumerateArray().Select(p => (object)new
                        {
                            description = GetString(p, "description"),
                            subTotal = GetDecimal(p, "subTotal"),
                            discount = GetDecimal(p, "discount"),
                            total = GetDecimal(p, "total"),
                            typeUser = GetString(p, "type_user"),
                            quantity = GetDecimal(p, "quantity"),
                            status = GetInt(p, "status"),
                            paymentDate = GetString(p, "paymentDate"),
                            createdBy = GetString(p, "createdBy"),
                            isBankPayment = GetBool(p, "isBankPayment"),
                            name = GetString(p, "name"),
                            activeDependency = GetBool(p, "activeDependency"),
                            acronym = GetString(p, "acronym"),
                            cashier = GetString(p, "cashier"),
                            termName = GetString(p, "termName"),
                            amountInWords = GetString(p, "amountInWords")
                        }).ToList()
                        : new List<object>();

                    vouchers.Add(new
                    {
                        serialVoucher = GetString(voucher, "serial_voucher"),
                        userName = GetString(voucher, "userName"),
                        fullName = GetString(voucher, "fullName"),
                        payments = paymentsArr
                    });
                }

                return vouchers;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // ───────── Helpers JSON ─────────
        private static string GetString(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? string.Empty : string.Empty;

        private static decimal GetDecimal(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : 0;

        private static int GetInt(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) ? v.GetInt32() : 0;

        private static bool GetBool(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.True;

        // ════════════════════════════════════════════════════════════════
        //  Persistir resultados a las tablas estructuradas
        // ════════════════════════════════════════════════════════════════

        [HttpPost("salvar-academico")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAcademic([FromForm] Guid apiId, [FromForm] string dni, [FromForm] Guid logId, CancellationToken ct)
        {
            try
            {
                var api = await _apis.GetByIdAsync(apiId, tracking: false, ct);
                if (api == null) return NotFound(new { success = false, message = "API no encontrada." });

                var log = await _apis.GetLogByIdAsync(logId, ct);
                if (log == null) return NotFound(new { success = false, message = "Log no encontrado." });

                if (string.IsNullOrWhiteSpace(log.ResponseExcerpt))
                    return BadRequest(new { success = false, message = "No hay datos en el log." });

                var parsed = ParseAcademicResponse(log.ResponseExcerpt);
                if (parsed == null)
                    return BadRequest(new { success = false, message = "No se pudieron parsear los datos académicos." });

                // Usar reflection para extraer items
                var itemsProp = parsed.GetType().GetProperty("items");
                if (itemsProp?.GetValue(parsed) is not IEnumerable<object> items) 
                    return BadRequest(new { success = false, message = "No hay items académicos." });

                var records = new List<ExternalAcademicInfo>();
                foreach (var item in items)
                {
                    var dict = ItemToDictionary(item);
                    records.Add(new ExternalAcademicInfo
                    {
                        Id = Guid.NewGuid(),
                        ExternalApiId = apiId,
                        QueryLogId = logId,
                        Dni = dict.GetValueOrDefault("dni") ?? dni,
                        UserName = dict.GetValueOrDefault("userName") ?? string.Empty,
                        Name = dict.GetValueOrDefault("name") ?? string.Empty,
                        PaternalSurname = dict.GetValueOrDefault("paternalSurname") ?? string.Empty,
                        MaternalSurname = dict.GetValueOrDefault("maternalSurname") ?? string.Empty,
                        Email = dict.GetValueOrDefault("email"),
                        PersonalEmail = dict.GetValueOrDefault("personalEmail"),
                        CareerName = dict.GetValueOrDefault("careerName") ?? string.Empty,
                        FacultyName = dict.GetValueOrDefault("facultyName") ?? string.Empty,
                        TotalCreditsApproved = decimal.TryParse(dict.GetValueOrDefault("totalCreditsApproved"), out var cr) ? cr : 0,
                        QueriedAt = log.QueriedAt
                    });
                }

                await _apis.SaveAcademicInfoAsync(records, ct);
                _logger.LogInformation("Guardados {Count} registros académicos para DNI {Dni} desde log {LogId}", records.Count, dni, logId);
                return Ok(new { success = true, count = records.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando info académica");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("salvar-pagos")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePayments([FromForm] Guid apiId, [FromForm] string dni, [FromForm] Guid logId, CancellationToken ct)
        {
            try
            {
                var api = await _apis.GetByIdAsync(apiId, tracking: false, ct);
                if (api == null) return NotFound(new { success = false, message = "API no encontrada." });

                var log = await _apis.GetLogByIdAsync(logId, ct);
                if (log == null) return NotFound(new { success = false, message = "Log no encontrado." });

                if (string.IsNullOrWhiteSpace(log.ResponseExcerpt))
                    return BadRequest(new { success = false, message = "No hay datos en el log." });

                var parsed = ParsePaymentResponse(log.ResponseExcerpt);
                if (parsed is not IEnumerable<object> vouchers)
                    return BadRequest(new { success = false, message = "No se pudieron parsear los pagos." });

                var voucherEntities = new List<ExternalPaymentVoucher>();
                foreach (var v in vouchers)
                {
                    var vDict = ItemToDictionary(v);
                    var voucher = new ExternalPaymentVoucher
                    {
                        Id = Guid.NewGuid(),
                        ExternalApiId = apiId,
                        QueryLogId = logId,
                        SerialVoucher = vDict.GetValueOrDefault("serialVoucher") ?? string.Empty,
                        UserName = vDict.GetValueOrDefault("userName") ?? string.Empty,
                        FullName = vDict.GetValueOrDefault("fullName") ?? string.Empty,
                        QueriedAt = log.QueriedAt,
                    };

                    // Extraer payments del objeto anónimo
                    var paymentsProp = v.GetType().GetProperty("payments");
                    if (paymentsProp?.GetValue(v) is IEnumerable<object> paymentItems)
                    {
                        foreach (var pi in paymentItems)
                        {
                            var pDict = ItemToDictionary(pi);
                            voucher.Payments.Add(new ExternalPaymentDetail
                            {
                                Id = Guid.NewGuid(),
                                VoucherId = voucher.Id,
                                Description = pDict.GetValueOrDefault("description") ?? string.Empty,
                                SubTotal = decimal.TryParse(pDict.GetValueOrDefault("subTotal"), out var st) ? st : 0,
                                Discount = decimal.TryParse(pDict.GetValueOrDefault("discount"), out var dc) ? dc : 0,
                                Total = decimal.TryParse(pDict.GetValueOrDefault("total"), out var tt) ? tt : 0,
                                TypeUser = pDict.GetValueOrDefault("typeUser"),
                                Quantity = decimal.TryParse(pDict.GetValueOrDefault("quantity"), out var qty) ? qty : 0,
                                Status = int.TryParse(pDict.GetValueOrDefault("status"), out var sts) ? sts : 0,
                                PaymentDate = DateTime.TryParse(pDict.GetValueOrDefault("paymentDate"), out var pd) ? pd : null,
                                CreatedBy = pDict.GetValueOrDefault("createdBy"),
                                IsBankPayment = bool.TryParse(pDict.GetValueOrDefault("isBankPayment"), out var ibp) && ibp,
                                Name = pDict.GetValueOrDefault("name"),
                                ActiveDependency = bool.TryParse(pDict.GetValueOrDefault("activeDependency"), out var ad) && ad,
                                Acronym = pDict.GetValueOrDefault("acronym"),
                                Cashier = pDict.GetValueOrDefault("cashier"),
                                TermName = pDict.GetValueOrDefault("termName"),
                                AmountInWords = pDict.GetValueOrDefault("amountInWords")
                            });
                        }
                    }

                    voucherEntities.Add(voucher);
                }

                await _apis.SavePaymentVouchersAsync(voucherEntities, ct);
                var totalPayments = voucherEntities.Sum(v => v.Payments.Count);
                _logger.LogInformation("Guardados {Vouchers} comprobantes y {Payments} pagos para DNI {Dni} desde log {LogId}",
                    voucherEntities.Count, totalPayments, dni, logId);
                return Ok(new { success = true, vouchers = voucherEntities.Count, payments = totalPayments });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando pagos");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private static Dictionary<string, string> ItemToDictionary(object item)
        {
            var dict = new Dictionary<string, string>();
            if (item == null) return dict;
            foreach (var prop in item.GetType().GetProperties())
            {
                var val = prop.GetValue(item);
                dict[prop.Name] = val?.ToString() ?? string.Empty;
            }
            return dict;
        }
    }
}
