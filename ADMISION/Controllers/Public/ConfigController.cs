using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace ADMISION.Controllers.Public
{
    [Route("public/config")]
    public class ConfigController : Controller
    {
        private readonly IConfigService _configService;

        public ConfigController(IConfigService configService)
        {
            _configService = configService;
        }

        [HttpGet("list")]
        public async Task<IActionResult> List()
        {
            var configs = await _configService.GetAllConfigsAsync();
            return Json(configs);
        }
    }
}
