namespace ScriptManager.Models.Release
{
    public class ReleaseListItemViewModel
    {
        public long ReleaseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ScriptCount { get; set; }
        public int RollbackScriptCount { get; set; }
        public bool IsCancelled { get; set; }
    }
}