using Microsoft.AspNetCore.Mvc;
using ScriptManager.Models.Batch;
using ScriptManager.Models.Dashboard;
using ScriptManager.Models.Release;
using ScriptManager.Services;

namespace ScriptManager.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IApiService _apiService;

        public DashboardController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Dashboard";

            var releases = await _apiService.GetListAsync<ReleaseListItemViewModel>("api/Release");
            var batches = await _apiService.GetListAsync<BatchListItemViewModel>("api/Batch");

            var model = new DashboardViewModel
            {
                TotalReleases = releases.Count,
                TotalScripts = 56,
                OpenConflicts = 2,
                ReadyScripts = 12,
                TestingScripts = 5,
                LatestReleases = releases
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(5)
                    .ToList(),
                RecentBatches = batches
                    .Take(5)
                    .Select(x => new BatchSummaryViewModel
                    {
                        Name = x.Name,
                        ScriptCount = 0
                    })
                    .ToList(),
                Alerts = new List<AlertItemViewModel>
                {
                    new AlertItemViewModel { Severity = 3, Message = "Conflicts detected on Customers table" },
                    new AlertItemViewModel { Severity = 2, Message = "5 scripts waiting for approval" },
                    new AlertItemViewModel { Severity = 1, Message = "1 release missing rollback script" }
                }
            };

            return View(model);
        }
    }
}