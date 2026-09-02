using ADMISION.ENTITIES.Models.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Text.Json;

namespace ADMISION.Services.Interceptors
{
    /// <summary>
    /// Interceptor de auditoría que registra todas las operaciones de base de datos.
    /// Incluye INSERT, UPDATE, DELETE (vía SaveChanges) y opcionalmente consultas SELECT.
    /// </summary>
    public class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            "Password", "PasswordHash", "Secret", "SecretKey", "Token", "AccessToken",
            "RefreshToken", "ApiKey", "Template", "BiometricTemplate",
            "AuthValue", "AuthToken"
        };

        private const string RedactedPlaceholder = "***REDACTED***";

        public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private static object? Redact(string propertyName, object? value)
        {
            if (value == null) return null;
            return SensitiveProperties.Contains(propertyName) ? RedactedPlaceholder : value;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var dbContext = eventData.Context;

            if (dbContext == null)
            {
                return await base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            var entries = dbContext.ChangeTracker.Entries()
                .Where(e => e.Entity is not Audit &&
                           e.Entity is not AccessLog &&
                           (e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted))
                .ToList();

            if (!entries.Any())
            {
                return await base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
            var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();
            var userId = httpContext?.User?.Identity?.Name;

            foreach (var entry in entries)
            {
                var auditEntry = new Audit
                {
                    Id = Guid.NewGuid(),
                    TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                    Action = entry.State.ToString(),
                    UserId = userId,
                    Timestamp = DateTimeOffset.UtcNow,
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                };

                // Serialización mejorada de valores usando JSON
                if (entry.State == EntityState.Modified)
                {
                    var oldValues = new Dictionary<string, object?>();
                    var newValues = new Dictionary<string, object?>();

                    foreach (var prop in entry.OriginalValues.Properties)
                    {
                        var originalValue = entry.OriginalValues[prop];
                        var currentValue = entry.CurrentValues[prop];

                        // Solo registrar si el valor cambió
                        if (!Equals(originalValue, currentValue))
                        {
                            oldValues[prop.Name] = Redact(prop.Name, originalValue);
                            newValues[prop.Name] = Redact(prop.Name, currentValue);
                        }
                    }

                    auditEntry.OldValues = oldValues.Any()
                        ? JsonSerializer.Serialize(oldValues)
                        : null;
                    auditEntry.NewValues = newValues.Any()
                        ? JsonSerializer.Serialize(newValues)
                        : null;
                }
                else if (entry.State == EntityState.Added)
                {
                    var newValues = new Dictionary<string, object?>();
                    foreach (var prop in entry.CurrentValues.Properties)
                    {
                        newValues[prop.Name] = Redact(prop.Name, entry.CurrentValues[prop]);
                    }
                    auditEntry.NewValues = JsonSerializer.Serialize(newValues);
                }
                else if (entry.State == EntityState.Deleted)
                {
                    var oldValues = new Dictionary<string, object?>();
                    foreach (var prop in entry.OriginalValues.Properties)
                    {
                        oldValues[prop.Name] = Redact(prop.Name, entry.OriginalValues[prop]);
                    }
                    auditEntry.OldValues = JsonSerializer.Serialize(oldValues);
                }

                dbContext.Set<Audit>().Add(auditEntry);
            }

            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            // También puedes agregar lógica post-guardado aquí si es necesario
            return base.SavedChanges(eventData, result);
        }
    }

    /// <summary>
    /// Interceptor completo que registra TODAS las operaciones SQL, incluyendo consultas SELECT.
    /// ADVERTENCIA: Puede generar mucho volumen de logs. Usar con precaución en producción.
    /// </summary>
    public class FullDatabaseAuditInterceptor : DbCommandInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<FullDatabaseAuditInterceptor>? _logger;

        public FullDatabaseAuditInterceptor(
            IHttpContextAccessor httpContextAccessor,
            ILogger<FullDatabaseAuditInterceptor>? logger = null)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            LogCommand(command, "SELECT");
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            LogCommand(command, "SELECT");
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            LogCommand(command, "NON_QUERY");
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            LogCommand(command, "NON_QUERY");
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result)
        {
            LogCommand(command, "SCALAR");
            return base.ScalarExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            LogCommand(command, "SCALAR");
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void LogCommand(DbCommand command, string commandType)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString();
            var userId = httpContext?.User?.Identity?.Name;
            var userAgent = httpContext?.Request?.Headers["User-Agent"].ToString();

            var logEntry = new
            {
                Timestamp = DateTimeOffset.UtcNow,
                CommandType = commandType,
                SqlCommand = command.CommandText,
                Parameters = command.Parameters.Count > 0
                    ? command.Parameters.Cast<DbParameter>()
                        .Select(p => new { p.ParameterName, Value = p.Value?.ToString() })
                        .ToList()
                    : null,
                UserId = userId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Duration = command.CommandTimeout
            };

            // Log a archivo, consola o base de datos
            _logger?.LogInformation(
                "SQL Executed: {CommandType} | User: {UserId} | IP: {IpAddress} | Query: {SqlCommand}",
                commandType,
                userId ?? "Anonymous",
                ipAddress ?? "Unknown",
                command.CommandText
            );

            // Opcionalmente, guardar en base de datos (en un contexto separado para evitar recursión)
            // Task.Run(() => SaveToDatabase(logEntry));
        }

        // Método opcional para guardar logs SQL en base de datos
        // private async Task SaveToDatabase(object logEntry)
        // {
        //     try
        //     {
        //         using var scope = _serviceProvider.CreateScope();
        //         var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        //         
        //         var sqlLog = new SqlCommandLog
        //         {
        //             Id = Guid.NewGuid(),
        //             LogData = JsonSerializer.Serialize(logEntry),
        //             CreatedAt = DateTime.UtcNow
        //         };
        //         
        //         context.SqlCommandLogs.Add(sqlLog);
        //         await context.SaveChangesAsync();
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger?.LogError(ex, "Error al guardar log SQL en base de datos");
        //     }
        // }
    }

    /// <summary>
    /// Interceptor híbrido que combina auditoría de cambios y logging selectivo de consultas.
    /// Permite configurar qué tablas o consultas se deben auditar.
    /// </summary>
    public class SelectiveAuditInterceptor : DbCommandInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SelectiveAuditInterceptor>? _logger;
        private readonly HashSet<string> _tablesToAudit;
        private readonly bool _logAllQueries;

        public SelectiveAuditInterceptor(
            IHttpContextAccessor httpContextAccessor,
            ILogger<SelectiveAuditInterceptor>? logger = null,
            IEnumerable<string>? tablesToAudit = null,
            bool logAllQueries = false)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _tablesToAudit = tablesToAudit?.ToHashSet(StringComparer.OrdinalIgnoreCase)
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _logAllQueries = logAllQueries;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            if (_logAllQueries || ShouldAuditCommand(command.CommandText))
            {
                LogQueryCommand(command);
            }

            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            // Siempre auditar comandos de modificación (INSERT, UPDATE, DELETE)
            LogModificationCommand(command);
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        private bool ShouldAuditCommand(string commandText)
        {
            if (!_tablesToAudit.Any())
                return false;

            var upperCommand = commandText.ToUpperInvariant();
            return _tablesToAudit.Any(table =>
                upperCommand.Contains($"FROM {table}", StringComparison.OrdinalIgnoreCase) ||
                upperCommand.Contains($"JOIN {table}", StringComparison.OrdinalIgnoreCase));
        }

        private void LogQueryCommand(DbCommand command)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userId = httpContext?.User?.Identity?.Name ?? "Anonymous";
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";

            _logger?.LogInformation(
                "Query Executed | User: {UserId} | IP: {IpAddress} | SQL: {CommandText}",
                userId,
                ipAddress,
                TruncateSql(command.CommandText)
            );
        }

        private void LogModificationCommand(DbCommand command)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var userId = httpContext?.User?.Identity?.Name ?? "Anonymous";
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";

            _logger?.LogWarning(
                "Data Modified | User: {UserId} | IP: {IpAddress} | SQL: {CommandText}",
                userId,
                ipAddress,
                command.CommandText
            );
        }

        private string TruncateSql(string sql, int maxLength = 200)
        {
            return sql.Length > maxLength
                ? sql.Substring(0, maxLength) + "..."
                : sql;
        }
    }
}