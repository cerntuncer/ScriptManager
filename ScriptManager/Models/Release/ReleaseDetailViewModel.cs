namespace ScriptManager.Models.Release
{
    public class ReleaseDetailViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long ReleaseId { get; set; }
        public string ReleaseName { get; set; } = string.Empty;
        public string Version { get; set; }
        public string? Description { get; set; }
        public string CombinedSql { get; set; }
        public string CombinedRollback { get; set; }
        public List<ReleaseScriptItemViewModel> Scripts { get; set; } = new();
        public List<ReleaseBatchFolderViewModel> FolderTree { get; set; } = new();

        public bool IsTreeLocked { get; set; }

        public bool IsCancelled { get; set; }

        public List<BatchPickerOptionViewModel> BatchPickerOptions { get; set; } = new();

    }

    public class BatchPickerOptionViewModel
    {
        public long BatchId { get; set; }
        public string Label { get; set; } = string.Empty;
    }
    public class ReleaseScriptItemViewModel
    {
        public long ScriptId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
        public long BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;

        public long DeveloperId { get; set; }
        public string DeveloperName { get; set; } = string.Empty;
        public string ReferencedTablesDisplay { get; set; } = "—";

        public string SqlScript { get; set; } = string.Empty;
        public string? RollbackScript { get; set; }
    }
}