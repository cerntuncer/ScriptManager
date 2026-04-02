using Microsoft.AspNetCore.Mvc;
using ScriptManager.Models.Release;
using ScriptManager.Services;

namespace ScriptManager.Controllers
{
    public class ReleasesController : Controller
    {
        private readonly IApiService _apiService;

        public ReleasesController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Releases";
            var releases = await _apiService.GetListAsync<ReleaseListItemViewModel>("api/Release");
            return View(releases);
        }

        public async Task<IActionResult> Detail(long id)
        {
            var detail = await _apiService.GetAsync<ReleaseDetailViewModel>($"api/Release/{id}");
            if (detail == null) return NotFound();

            return View(detail);
        }
    }
}