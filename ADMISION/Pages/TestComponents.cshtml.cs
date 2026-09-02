using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace admision.Pages
{
    public class TestComponentsModel : PageModel
    {
        [BindProperty]
        public string? SelectedValue { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            TempData["Success"] = $"Formulario enviado correctamente. Valor: {SelectedValue}";
            return Page();
        }
    }
}
