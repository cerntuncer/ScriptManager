using Microsoft.AspNetCore.Mvc;
using ScriptManager.Data;
using DAL.Context;
using ScriptManager.Models.Dashboard;
using ScriptManager.Models.Release;

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
            ViewData["Title"] = "Dashboard";

            var releases = await ReleaseReadQueries.ListReleasesAsync(_db);
            var scripts = await ScriptReadQueries.ListActiveScriptsAsync(_db);

            var alerts = new List<AlertItemViewModel>();
            var readyCount = scripts.Count(s => string.Equals(s.Status, "Ready", StringComparison.OrdinalIgnoreCase));
            var testingCount = scripts.Count(s => string.Equals(s.Status, "Testing", StringComparison.OrdinalIgnoreCase));
            var conflictCount = scripts.Count(s => string.Equals(s.Status, "Conflict", StringComparison.OrdinalIgnoreCase));
            var draftCount = scripts.Count(s => string.Equals(s.Status, "Draft", StringComparison.OrdinalIgnoreCase));
            var scriptsTotal = scripts.Count;
            var accounted = readyCount + testingCount + conflictCount + draftCount;
            var otherCount = Math.Max(0, scriptsTotal - accounted);
            if (conflictCount > 0)
                alerts.Add(new AlertItemViewModel { Severity = 3, Message = $"{conflictCount} script çakışma (Conflict) durumunda." });

            if (draftCount > 0)
                alerts.Add(new AlertItemViewModel { Severity = 2, Message = $"{draftCount} script taslak — test / onay bekliyor olabilir." });

            var missingRollbackReady = scripts.Count(s =>
                string.Equals(s.Status, "Ready", StringComparison.OrdinalIgnoreCase) &&
                !s.HasRollback);
            if (missingRollbackReady > 0)
                alerts.Add(new AlertItemViewModel { Severity = 2, Message = $"{missingRollbackReady} Ready script’te rollback tanımı yok (release uyarısı tetiklenebilir)." });

            var cacheRiskScripts = scripts.Count(s => s.HasCacheBustHints);
            if (cacheRiskScripts > 0)
                alerts.Add(new AlertItemViewModel
                {
                    Severity = 2,
                    Message = $"{cacheRiskScripts} script’te önbellek / plan recompile ipucu (SQL metninde anahtar kelime). Detay veya release listesinde inceleyin."
                });

            if (alerts.Count == 0)
                alerts.Add(new AlertItemViewModel { Severity = 1, Message = "Özet: Kritik uyarı yok. Kanal aktarımı (L:\\...) dosya içe aktarma sonraki adım." });

            var model = new DashboardViewModel
            {
                TotalReleases = releases.Count,
                TotalScripts = scriptsTotal,
                OpenConflicts = conflictCount,
                ScriptsWithCacheHints = cacheRiskScripts,
                ReadyScripts = readyCount,
                TestingScripts = testingCount,
                DraftScripts = draftCount,
                OtherScripts = otherCount,
                LatestReleases = releases
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(5)
                    .ToList(),
                Alerts = alerts
            };

            return View(model);
        }
    }
}
