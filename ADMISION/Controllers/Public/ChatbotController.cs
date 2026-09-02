using ADMISION.ENTITIES.Models.Info;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ADMISION.Controllers.Public
{
    /// <summary>
    /// Endpoints públicos del chatbot de preguntas frecuentes.
    /// La página dedicada vive en /preguntas-frecuentes y la burbuja flotante
    /// (en _PublicLayout) consume <see cref="Ask"/> vía fetch.
    /// </summary>
    [Route("")]
    public class ChatbotController : Controller
    {
        private readonly IFaqService _faq;

        public ChatbotController(IFaqService faq) { _faq = faq; }

        [HttpGet("~/preguntas-frecuentes")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var faqs = await _faq.GetPublicAsync(ct);
            var categories = await _faq.GetCategoriesAsync(ct);
            ViewBag.Categories = categories;
            return View("~/Pages/Public/Faq.cshtml", faqs);
        }

        [HttpGet("~/chatbot/ask")]
        [EnableRateLimiting("public-lookup")]
        public async Task<IActionResult> Ask(string q, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new { matched = false, suggestions = Array.Empty<object>() });
            }

            var result = await _faq.AskAsync(q.Trim(), suggestionsTop: 3, ct);

            return Json(new
            {
                matched = result.Matched,
                score = Math.Round(result.Score, 3),
                answer = result.Best == null ? null : new
                {
                    id = result.Best.Id,
                    question = result.Best.Question,
                    answer = result.Best.Answer,
                    category = result.Best.Category
                },
                suggestions = result.Suggestions.Select(s => new
                {
                    id = s.Id,
                    question = s.Question,
                    category = s.Category,
                    score = Math.Round(s.Score, 3)
                })
            });
        }

        [HttpGet("~/chatbot/faq/{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var item = await _faq.GetByIdAsync(id, ct);
            if (item == null || !item.IsActive) return NotFound();
            return Json(new
            {
                id = item.Id,
                question = item.Question,
                answer = item.Answer,
                category = item.Category
            });
        }

        /// <summary>
        /// Opciones raíz del menú del chatbot.
        /// </summary>
        [HttpGet("~/chatbot/options")]
        [EnableRateLimiting("public-lookup")]
        public async Task<IActionResult> Options(CancellationToken ct)
        {
            var roots = await _faq.GetRootOptionsAsync(ct);
            return Json(new
            {
                options = roots.Select(o => new
                {
                    id = o.Id,
                    question = o.Question,
                    category = o.Category
                })
            });
        }

        /// <summary>
        /// Respuesta de una opción + sus sub-opciones hijas.
        /// </summary>
        [HttpGet("~/chatbot/options/{id:guid}")]
        [EnableRateLimiting("public-lookup")]
        public async Task<IActionResult> OptionDetail(Guid id, CancellationToken ct)
        {
            var item = await _faq.GetOptionWithChildrenAsync(id, ct);
            if (item == null) return NotFound();

            var children = item.Children.ToList();

            return Json(new
            {
                answer = new
                {
                    id = item.Id,
                    question = item.Question,
                    answer = item.Answer,
                    category = item.Category
                },
                parentId = item.ParentId,
                hasChildren = children.Count > 0,
                options = children.Select(c => new
                {
                    id = c.Id,
                    question = c.Question,
                    category = c.Category
                })
            });
        }
    }
}
