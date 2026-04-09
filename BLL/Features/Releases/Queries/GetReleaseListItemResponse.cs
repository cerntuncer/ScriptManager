namespace BLL.Features.Releases.Queries
{
    public class GetReleaseListItemResponse
    {
        public long ReleaseId { get; set; }
        public string Version { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ScriptCount { get; set; }
        public int RollbackScriptCount { get; set; }
    }
}
