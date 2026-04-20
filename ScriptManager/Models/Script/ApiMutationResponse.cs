namespace ScriptManager.Models.Script
{
    public class ApiMutationResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public long ScriptId { get; set; }
        public long? BatchId { get; set; }

        public string? ScriptName { get; set; }
        public string? Status { get; set; }
        public string? StatusKey { get; set; }
        public string? BatchName { get; set; }
        public string? DeveloperName { get; set; }
        public bool HasRollback { get; set; }
        public string? CreatedAtDisplay { get; set; }

        public bool CanDelete { get; set; }
    }
}
