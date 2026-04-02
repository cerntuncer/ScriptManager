using Microsoft.AspNetCore.Mvc;
using ScriptManager.Models.Script;
using ScriptManager.Services;

namespace ScriptManager.Controllers
{
    public class ScriptsController : Controller
    {
        private readonly IApiService _apiService;

        public ScriptsController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Scripts";

            var scripts = await _apiService.GetListAsync<ScriptListItemViewModel>("api/Script/list");

            return View(scripts);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(long id)
        {
            var model = await _apiService.GetAsync<ScriptListItemViewModel>($"api/Script/{id}");
            if (model == null) return NotFound();

            return View(model);
        }
    }

}