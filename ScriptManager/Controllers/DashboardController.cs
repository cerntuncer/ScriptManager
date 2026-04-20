using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

            var openConflictRecordCount = await _db.Conflicts.AsNoTracking()
                .CountAsync(c => c.ResolvedAt == null && !c.IsDeleted);

            var openConflictPairs = await _db.Conflicts.AsNoTracking()
                .Where(c => c.ResolvedAt == null && !c.IsDeleted)
                .Select(c => new { c.ScriptId, c.ConflictingScriptId })
                .ToListAsync();
            var scriptIdsInOpenConflicts = openConflictPairs
                .SelectMany(p => new[] { p.ScriptId, p.ConflictingScriptId })
                .ToHashSet();

            var readyCount = 0;
            var draftCount = 0;
            var scriptsInOpenConflict = 0;
            var otherCount = 0;

            foreach (var s in scripts)
            {
                if (scriptIdsInOpenConflicts.Contains(s.ScriptId))
                {
                    scriptsInOpenConflict++;
                    continue;
                }

                if (string.Equals(s.Status, "Ready", StringComparison.OrdinalIgnoreCase))
                    readyCount++;
                else if (string.Equals(s.Status, "Draft", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(s.Status, "PendingTesterReview", StringComparison.OrdinalIgnoreCase))
                    draftCount++;
                else
                    otherCount++;
            }

            var scriptsTotal = scripts.Count;

            var model = new DashboardViewModel
            {
                TotalReleases = releases.Count,
                TotalScripts = scriptsTotal,
                OpenConflicts = openConflictRecordCount,
                ScriptsInOpenConflict = scriptsInOpenConflict,
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
