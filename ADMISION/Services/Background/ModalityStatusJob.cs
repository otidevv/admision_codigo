using ADMISION.ENTITIES.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ADMISION.Services.Background
{
    public class ModalityStatusJob
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ModalityStatusJob> _logger;
        private readonly IConfiguration _configuration;

        public ModalityStatusJob(
            AppDbContext context,
            ILogger<ModalityStatusJob> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task CheckAndDeactivateModalitiesAsync()
        {
            _logger.LogInformation("Hangfire Job: Checking modalities status.");

            var timeZoneId = _configuration["Jobs:TimeZoneId"] ?? "SA Pacific Standard Time";
            DateTimeOffset nowInTargetZone;
            try
            {
                var timezone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                nowInTargetZone = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timezone);
            }
            catch (TimeZoneNotFoundException)
            {
                _logger.LogWarning("TimeZone {TZ} no encontrada. Usando UTC-5 como fallback.", timeZoneId);
                nowInTargetZone = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5));
            }

            var todayLocal = DateOnly.FromDateTime(nowInTargetZone.DateTime);

            _logger.LogInformation("Job running for local date: {Date}", todayLocal);

            // Cierra la modalidad cuando ya pasó su hora de cierre (EndDate + EndTime).
            var nowLocal = nowInTargetZone.DateTime;

            var affectedRows = await _context.Modalities
                .Where(m => m.IsActive == true && m.EndDate.ToDateTime(m.EndTime) < nowLocal)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.IsActive, false)
                    .SetProperty(m => m.UpdatedAt, DateTimeOffset.UtcNow)
                    .SetProperty(m => m.UpdatedBy, "System (Hangfire Job)")
                );

            if (affectedRows > 0)
                _logger.LogInformation("Modalities deactivated: {Count}", affectedRows);
            else
                _logger.LogInformation("No modalities found to deactivate.");
        }
    }
}
