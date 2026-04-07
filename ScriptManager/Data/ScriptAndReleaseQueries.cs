using BLL.Data;
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
            ScriptStatus.Testing => "İncelemede (test)",
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
        var cacheSummary = SqlCacheBustAnalyzer.SummaryLabel(s.SqlScript, s.RollbackScript);
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
            ReferencedTablesDisplay = tablesDisplay,
            HasCacheBustHints = cacheSummary != null,
            CacheBustSummary = cacheSummary
        };
    }

    public static async Task<List<BatchPickerOptionViewModel>> ListAllBatchesWithPathAsync(MyContext db)
    {
        var batches = await db.Batches.AsNoTracking()
            .Where(b => !b.IsDeleted && b.ReleaseId != null)
            .Include(b => b.Release)
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
        var cacheSummary = SqlCacheBustAnalyzer.SummaryLabel(s.SqlScript, s.RollbackScript);
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
            HasCacheBustHints = cacheSummary != null,
            CacheBustSummary = cacheSummary
        };
    }

}

public static class ReleaseReadQueries
{
    private static bool IsScriptActive(Script s) =>
        !s.IsDeleted && s.Status != ScriptStatus.Deleted;

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
            ScriptCount = r.Batches.SelectMany(b => b.Scripts).Count(IsScriptActive),
            RollbackScriptCount = r.Batches.SelectMany(b => b.Scripts).Count(s =>
                IsScriptActive(s) && !string.IsNullOrWhiteSpace(s.RollbackScript))
        }).ToList();
    }

    public static async Task<ReleaseDetailViewModel?> GetReleaseDetailAsync(MyContext db, long id)
    {
        var release = await db.Releases.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (release == null)
            return null;

        var batches = await db.Batches.AsNoTracking()
            .Where(b => b.ReleaseId == id && !b.IsDeleted)
            .Include(b => b.Scripts)
            .ThenInclude(s => s.Developer)
            .ToListAsync();

        if (release.RootBatchId.HasValue)
        {
            var allowed = await BatchTreeHelper.CollectSubtreeIdsAsync(db, release.RootBatchId.Value);
            var set = allowed.ToHashSet();
            batches = batches.Where(b => set.Contains(b.Id)).ToList();
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

        var releaseBatchIdSet = batches.Select(b => b.Id).ToHashSet();

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

        var assignableScriptRows = await db.Scripts.AsNoTracking()
            .Include(s => s.Batch)
            .Where(s => !s.IsDeleted && s.Status != ScriptStatus.Deleted && s.Status == ScriptStatus.Ready &&
                (s.BatchId == null || !releaseBatchIdSet.Contains(s.BatchId.Value)))
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Id)
            .ToListAsync();

        var batchByIdAll = new Dictionary<long, Batch>();
        var toFetch = new Queue<long>(assignableScriptRows.Where(s => s.BatchId.HasValue).Select(s => s.BatchId!.Value).Distinct());
        while (toFetch.Count > 0)
        {
            var chunk = new List<long>();
            while (toFetch.Count > 0 && chunk.Count < 80)
            {
                var next = toFetch.Dequeue();
                if (!batchByIdAll.ContainsKey(next))
                    chunk.Add(next);
            }

            if (chunk.Count == 0) continue;
            var rows = await db.Batches.AsNoTracking()
                .Include(b => b.Release)
                .Where(b => chunk.Contains(b.Id))
                .ToListAsync();
            foreach (var row in rows)
            {
                if (batchByIdAll.ContainsKey(row.Id)) continue;
                batchByIdAll[row.Id] = row;
                if (row.ParentBatchId.HasValue && !batchByIdAll.ContainsKey(row.ParentBatchId.Value))
                    toFetch.Enqueue(row.ParentBatchId.Value);
            }
        }

        string PathFromDict(long batchId)
        {
            var parts = new List<string>();
            Batch? cur = batchByIdAll.GetValueOrDefault(batchId);
            var g = 0;
            while (cur != null && g++ < 64)
            {
                parts.Insert(0, cur.Name);
                if (!cur.ParentBatchId.HasValue) break;
                cur = batchByIdAll.GetValueOrDefault(cur.ParentBatchId.Value);
            }

            return string.Join(" → ", parts);
        }

        var assignableItems = assignableScriptRows.Select(s =>
        {
            var loc = !s.BatchId.HasValue
                ? "Atanmamış (havuz)"
                : batchByIdAll.GetValueOrDefault(s.BatchId.Value) is { } b
                    ? b.Release == null
                        ? $"Yetim · {PathFromDict(s.BatchId.Value)}"
                        : $"{b.Release.Version} · {PathFromDict(s.BatchId.Value)}"
                    : "—";
            return new AssignableScriptItemViewModel
            {
                ScriptId = s.Id,
                Name = s.Name,
                CurrentLocation = loc
            };
        }).ToList();

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

        return new ReleaseDetailViewModel
        {
            Success = true,
            Message = string.Empty,
            ReleaseId = release.Id,
            ReleaseName = release.Name,
            Version = release.Version,
            CombinedSql = combinedSql,
            CombinedRollback = combinedRb,
            Scripts = scriptRows,
            FolderTree = folderTree,
            AssignableScripts = assignableItems,
            BatchPickerOptions = pickerOptions
        };
    }
}
