using DAL.Enums;

namespace ScriptManager.Models.Script
{
    public class ScriptListItemViewModel
    {
        public long ScriptId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SqlScript { get; set; } = string.Empty;
        public string? RollbackScript { get; set; }
        public long? BatchId { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public long DeveloperId { get; set; }
        public string DeveloperName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        /// <summary>Durum akışı (taslak / test / hazır / çakışma) için.</summary>
        public ScriptStatus StatusEnum { get; set; }

        public string StatusDisplay { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool HasRollback { get; set; }

        public bool HasCacheBustHints { get; set; }
        public string? CacheBustSummary { get; set; }

        public string ReferencedTablesDisplay { get; set; } = string.Empty;
    }
}