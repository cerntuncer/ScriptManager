using BLL.Services;
using DAL.Context;
using DAL.Entities;
using DAL.Enums;
using Microsoft.EntityFrameworkCore;
using ScriptManager.Models.Release;
using ScriptManager.Models.Script;

namespace ScriptManager.Data;

public static class ScriptReadQueries
{
    public static string ScriptStatusDisplay(ScriptStatus s) =>
        s switch
        {
            ScriptStatus.Draft => "Taslak",
            ScriptStatus.Ready => "Hazır",
            ScriptStatus.Conflict => "Çakışma",
            ScriptStatus.Deleted => "Silindi",
            _ => s.ToString()
        };

    public static async Task<List<ScriptListItemViewModel>> ListActiveScriptsAsync(MyContext db)
    {
        var scriptRows = await db.Scripts.AsNoTracking()
            .Include(s => s.Batch)
            .Include(s => s.Developer)
            .Where(s => !s.IsDeleted && s.Status != ScriptStatus.Deleted)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return scriptRows.Select(ToListItem).ToList();
    }

    /// <summary>Release ağacı / export satırları için ortak view-model kurulumu.</summary>
    internal static ReleaseScriptItemViewModel ToReleaseScriptItem(Script s, int order, string batchName)
    {
        var tables = SqlReferencedTableExtractor.ExtractTables(s.SqlScript);
        tables.UnionWith(SqlReferencedTableExtractor.ExtractTables(s.RollbackScript));
        var tablesDisplay = tables.Count > 0 ? string.Join(", ", tables.OrderBy(t => t)) : "—";
        return new ReleaseScriptItemViewModel
        {
            ScriptId = s.Id,
            Name = s.Name,
            Order = order,
            BatchId = s.BatchId!.Value,
            BatchName = batchName,
            SqlScript = s.SqlScript ?? string.Empty,
            RollbackScript = s.RollbackScript,
            DeveloperId = s.DeveloperId,
            DeveloperName = s.Developer?.Name ?? string.Empty,
            ReferencedTablesDisplay = tablesDisplay
        };
    }

    public static async Task<List<BatchPickerOptionViewModel>> ListAllBatchesWithPathAsync(MyContext db)
    {
        var batches = await db.Batches.AsNoTracking()
            .Include(b => b.Release)
            .Where(b => !b.IsDeleted && b.ReleaseId != null && b.Release != null && !b.Release.IsCancelled)
            .ToListAsync();

        var byId = batches.ToDictionary(b => b.Id);
        string Path(long id)
        {
            var parts = new List<string>();
            Batch? c = byId.GetValueOrDefault(id);
            var g = 0;
            while (c != null && g++ < 64)
            {
                parts.Insert(0, c.Name);
                if (!c.ParentBatchId.HasValue) break;
                c = byId.GetValueOrDefault(c.ParentBatchId.Value);
            }

            return string.Join(" → ", parts);
        }

        return batches
            .OrderBy(b => b.Release!.Version)
            .ThenBy(b => Path(b.Id))
            .Select(b => new BatchPickerOptionViewModel
            {
                BatchId = b.Id,
                Label = $"{b.Release!.Version} — {Path(b.Id)}"
            })
            .ToList();
    }

    public static ScriptListItemViewModel ToListItem(Script s)
    {
        var tables = SqlReferencedTableExtractor.ExtractTables(s.SqlScript);
        tables.UnionWith(SqlReferencedTableExtractor.ExtractTables(s.RollbackScript));
        var tablesDisplay = tables.Count > 0 ? string.Join(", ", tables.OrderBy(t => t)) : string.Empty;
        return new ScriptListItemViewModel
        {
            ScriptId = s.Id,
            Name = s.Name,
            SqlScript = s.SqlScript,
            RollbackScript = s.RollbackScript,
            BatchId = s.BatchId,
            BatchName = s.Batch != null ? s.Batch.Name : "Atanmamış",
            DeveloperId = s.DeveloperId,
            DeveloperName = s.Developer != null ? s.Developer.Name : string.Empty,
            Status = s.Status.ToString(),
            StatusEnum = s.Status,
            StatusDisplay = ScriptStatusDisplay(s.Status),
            CreatedAt = s.CreatedAt,
            HasRollback = !string.IsNullOrWhiteSpace(s.RollbackScript),
            ReferencedTablesDisplay = tablesDisplay
        };
    }

}

public static class ReleaseReadQueries
{
    private static bool IsScriptActive(Script s) =>
        !s.IsDeleted && s.Status != ScriptStatus.Deleted;

    private static async Task<HashSet<long>> CollectSubtreeIdsAsync(MyContext db, long rootBatchId)
    {
        var result = new HashSet<long>();
        var queue = new Queue<long>();
        queue.Enqueue(rootBatchId);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!result.Add(id))
                continue;

            var children = await db.Batches.AsNoTracking()
                .Where(b => b.ParentBatchId == id && !b.IsDeleted)
                .Select(b => b.Id)
                .ToListAsync();

            foreach (var childId in children)
                queue.Enqueue(childId);
        }

        return result;
    }

    /// <summary>Ağaç post-order ile aktif script sırası (birleşik SQL ile aynı).</summary>
    private static List<Script> OrderScriptsForRelease(Release release, List<Batch> batches)
    {
        var byParent = batches.ToLookup(b => b.ParentBatchId);

        List<Batch> Children(long? parentId) =>
            byParent[parentId].OrderBy(x => x.Name).ThenBy(x => x.Id).ToList();

        List<Batch> roots;
        if (release.RootBatchId.HasValue)
        {
            var rb = batches.FirstOrDefault(b => b.Id == release.RootBatchId.Value);
            roots = rb != null ? new List<Batch> { rb } : new List<Batch>();
        }
        else
            roots = Children(null);

        IEnumerable<Script> PostWalk(Batch b)
        {
            foreach (var c in Children(b.Id))
            {
                foreach (var s in PostWalk(c))
                    yield return s;
            }

            foreach (var s in b.Scripts.Where(IsScriptActive).OrderBy(x => x.Name).ThenBy(x => x.Id))
                yield return s;
        }

        return roots.SelectMany(PostWalk).ToList();
    }

    /// <summary>Seçili scriptler için birleşik SQL / rollback (sıra = sürüm ağacı post-order).</summary>
    public static (string sql, string rollback)? BuildExportForScriptSubset(ReleaseDetailViewModel detail,
        IReadOnlyCollection<long> scriptIds)
    {
        if (detail.Scripts.Count == 0 || scriptIds.Count == 0)
            return ("", "");

        var set = scriptIds.ToHashSet();
        var pick = detail.Scripts.Where(s => set.Contains(s.ScriptId)).OrderBy(s => s.Order).ToList();
        if (pick.Count == 0)
            return null;

        var sqlBlocks = pick
            .Select(s => (s.SqlScript ?? "").Trim())
            .Where(s => s.Length > 0)
            .ToList();
        var sql = sqlBlocks.Count > 0 ? string.Join("\r\n\r\n", sqlBlocks) : string.Empty;

        var rbBlocks = pick
            .Select(s => (s.RollbackScript ?? "").Trim())
            .Where(s => s.Length > 0)
            .ToList();
        var rb = rbBlocks.Count > 0 ? string.Join("\r\n\r\n", rbBlocks) : string.Empty;
        return (sql, rb);
    }

    public static async Task<List<ReleaseListItemViewModel>> ListReleasesAsync(MyContext db)
    {
        var rows = await db.Releases.AsNoTracking()
            .Where(r => !r.IsDeleted)
            .Include(r => r.Batches)
            .ThenInclude(b => b.Scripts)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return rows.Select(r => new ReleaseListItemViewModel
        {
            ReleaseId = r.Id,
            Name = r.Name,
            Version = r.Version,
            CreatedAt = r.CreatedAt,
            IsCancelled = r.IsCancelled,
            ScriptCount = r.IsCancelled
                ? 0
                : r.Batches.SelectMany(b => b.Scripts).Count(IsScriptActive),
            RollbackScriptCount = r.IsCancelled
                ? 0
                : r.Batches.SelectMany(b => b.Scripts).Count(s =>
                    IsScriptActive(s) && !string.IsNullOrWhiteSpace(s.RollbackScript))
        }).ToList();
    }

    public static async Task<ReleaseDetailViewModel?> GetReleaseDetailAsync(MyContext db, long id)
    {
        var release = await db.Releases.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (release == null)
            return null;

        if (release.IsCancelled)
        {
            return new ReleaseDetailViewModel
            {
                Success = true,
                Message = "Bu sürüm iptal edildi; klasörler ve scriptler klasör ağacında.",
                ReleaseId = release.Id,
                ReleaseName = release.Name,
                Version = release.Version,
                Description = release.Description,
                CombinedSql = string.Empty,
                CombinedRollback = string.Empty,
                Scripts = new List<ReleaseScriptItemViewModel>(),
                FolderTree = new List<ReleaseBatchFolderViewModel>(),
                BatchPickerOptions = new List<BatchPickerOptionViewModel>(),
                IsTreeLocked = false,
                IsCancelled = true
            };
        }

        var batches = await db.Batches.AsNoTracking()
            .Where(b => b.ReleaseId == id && !b.IsDeleted)
            .Include(b => b.Scripts)
            .ThenInclude(s => s.Developer)
            .ToListAsync();

        if (release.RootBatchId.HasValue)
        {
            var allowed = await CollectSubtreeIdsAsync(db, release.RootBatchId.Value);
            batches = batches.Where(b => allowed.Contains(b.Id)).ToList();
        }

        var byParent = batches.ToLookup(b => b.ParentBatchId);

        List<Batch> Children(long? parentId) =>
            byParent[parentId].OrderBy(x => x.Name).ThenBy(x => x.Id).ToList();

        List<Batch> roots;
        if (release.RootBatchId.HasValue)
        {
            var rb = batches.FirstOrDefault(b => b.Id == release.RootBatchId.Value);
            roots = rb != null ? new List<Batch> { rb } : new List<Batch>();
        }
        else
        {
            roots = Children(null);
        }

        var ordered = OrderScriptsForRelease(release, batches);
        var batchNameById = batches.ToDictionary(b => b.Id, b => b.Name);
        var orderByScriptId = ordered.Select((s, i) => (s.Id, i + 1)).ToDictionary(x => x.Id, x => x.Item2);

        var sqlBlocks = ordered
            .Select(s => (s.SqlScript ?? "").Trim())
            .Where(s => s.Length > 0)
            .ToList();
        var combinedSql = sqlBlocks.Count > 0 ? string.Join("\r\n\r\n", sqlBlocks) : string.Empty;

        var rbBlocks = ordered
            .Select(s => (s.RollbackScript ?? "").Trim())
            .Where(s => s.Length > 0)
            .ToList();
        var combinedRb = rbBlocks.Count > 0 ? string.Join("\r\n\r\n", rbBlocks) : string.Empty;

        ReleaseBatchFolderViewModel MapFolder(Batch b)
        {
            var subs = Children(b.Id);
            return new ReleaseBatchFolderViewModel
            {
                BatchId = b.Id,
                Name = b.Name,
                Folders = subs.Select(MapFolder).ToList(),
                Scripts = b.Scripts.Where(IsScriptActive).OrderBy(s => s.Name).ThenBy(s => s.Id)
                    .Select(s => ScriptReadQueries.ToReleaseScriptItem(s,
                        orderByScriptId.GetValueOrDefault(s.Id, 0),
                        b.Name))
                    .ToList()
            };
        }

        var folderTree = roots.Select(MapFolder).ToList();

        var batchPathsForPicker = new Dictionary<long, string>();
        foreach (var b in batches)
        {
            var parts = new List<string>();
            Batch? cur = b;
            var guard = 0;
            while (cur != null && guard++ < 64)
            {
                parts.Insert(0, cur.Name);
                if (!cur.ParentBatchId.HasValue) break;
                cur = batches.FirstOrDefault(x => x.Id == cur.ParentBatchId.Value);
            }

            batchPathsForPicker[b.Id] = string.Join(" → ", parts);
        }

        var pickerOptions = batches
            .OrderBy(b => batchPathsForPicker.GetValueOrDefault(b.Id, b.Name))
            .Select(b => new BatchPickerOptionViewModel
            {
                BatchId = b.Id,
                Label = batchPathsForPicker.GetValueOrDefault(b.Id, b.Name)
            })
            .ToList();

        var scriptRows = ordered.Select((s, i) => ScriptReadQueries.ToReleaseScriptItem(s, i + 1,
            batchNameById.GetValueOrDefault(s.BatchId!.Value, string.Empty))).ToList();

        var treeLocked = batches.Any(b => b.IsLocked);

        return new ReleaseDetailViewModel
        {
            Success = true,
            Message = string.Empty,
            ReleaseId = release.Id,
            ReleaseName = release.Name,
            Version = release.Version,
            Description = release.Description,
            CombinedSql = combinedSql,
            CombinedRollback = combinedRb,
            Scripts = scriptRows,
            FolderTree = folderTree,
            BatchPickerOptions = pickerOptions,
            IsTreeLocked = treeLocked,
            IsCancelled = false
        };
    }
}
