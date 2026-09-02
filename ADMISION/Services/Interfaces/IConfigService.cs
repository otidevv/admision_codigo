using System.Collections.Generic;
using System.Threading.Tasks;

namespace ADMISION.Services.Interfaces
{
    public interface IConfigService
    {
        Task<Dictionary<string, string>> GetAllConfigsAsync();
        Task<string> GetConfigValueAsync(string key);
        Task UpdateConfigsAsync(Dictionary<string, string> configs, string updatedBy);
    }
}
