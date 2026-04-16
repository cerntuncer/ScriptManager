using Microsoft.AspNetCore.Mvc;
using ScriptManager.Data;
using DAL.Context;
using ScriptManager.Models.Dashboard;

namespace ScriptManager.Controllers
{
    public class DashboardController : Controller
    {
        private readonly MyContext _db;

        public DashboardController(MyContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Genel Bakış";

            var releases = await ReleaseReadQueries.ListReleasesAsync(_db);
            var scripts = await ScriptReadQueries.ListActiveScriptsAsync(_db);

            var readyCount = scripts.Count(s => string.Equals(s.Status, "Ready", StringComparison.OrdinalIgnoreCase));
            var conflictCount = scripts.Count(s => string.Equals(s.Status, "Conflict", StringComparison.OrdinalIgnoreCase));
            var draftCount = scripts.Count(s => string.Equals(s.Status, "Draft", StringComparison.OrdinalIgnoreCase));
            var scriptsTotal = scripts.Count;
            var accounted = readyCount + conflictCount + draftCount;
            var otherCount = Math.Max(0, scriptsTotal - accounted);

            var model = new DashboardViewModel
            {
                TotalReleases = releases.Count,
                TotalScripts = scriptsTotal,
                OpenConflicts = conflictCount,
                ReadyScripts = readyCount,
                DraftScripts = draftCount,
                OtherScripts = otherCount,
                LatestReleases = releases
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(8)
                    .ToList()
            };

            return View(model);
        }
    }
}
