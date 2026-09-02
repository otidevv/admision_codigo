using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.System;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ADMISION.Services.Implementations
{
    public class ConfigService : IConfigService
    {
        private readonly AppDbContext _context;

        public ConfigService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Dictionary<string, string>> GetAllConfigsAsync()
        {
            var dbConfigs = await _context.Configs.ToDictionaryAsync(c => c.Key, c => c.Value);
            var defaults = ConfigGeneral.DefaultValues;

            // Merge: DB values override defaults
            foreach (var defaultValue in defaults)
            {
                if (!dbConfigs.ContainsKey(defaultValue.Key))
                {
                    dbConfigs[defaultValue.Key] = defaultValue.Value;
                }
            }

            return dbConfigs;
        }

        public async Task<string> GetConfigValueAsync(string key)
        {
            var config = await _context.Configs.FirstOrDefaultAsync(c => c.Key == key);
            if (config != null)
            {
                return config.Value;
            }

            // Fallback to defaults
            if (ConfigGeneral.DefaultValues.TryGetValue(key, out var defaultValue))
            {
                return defaultValue;
            }

            return string.Empty;
        }

        public async Task UpdateConfigsAsync(Dictionary<string, string> configs, string updatedBy)
        {
            var existingConfigs = await _context.Configs.ToListAsync();

            foreach (var kvp in configs)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key)) continue;

                // El binder MVC puede entregar null para campos vacíos (color/url
                // sin valor). La columna Value es NOT NULL → coalescer siempre.
                var value = kvp.Value ?? string.Empty;

                // No sobrescribir la clave SMTP cuando el formulario la envía vacía.
                if (kvp.Key == ConfigGeneral.SmtpPassword
                    && string.IsNullOrWhiteSpace(value)
                    && existingConfigs.Any(c => c.Key == kvp.Key))
                {
                    continue;
                }

                var existing = existingConfigs.FirstOrDefault(c => c.Key == kvp.Key);
                if (existing != null)
                {
                    if (existing.Value != value)
                    {
                        existing.Value = value;
                        existing.UpdatedAt = DateTimeOffset.UtcNow;
                        existing.UpdatedBy = updatedBy;
                    }
                }
                else
                {
                    _context.Configs.Add(new Config
                    {
                        Id = Guid.NewGuid(),
                        Key = kvp.Key,
                        Value = value,
                        CreatedAt = DateTimeOffset.UtcNow,
                        CreatedBy = updatedBy
                    });
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
