namespace ScriptManager.Models.Script
{
    public class ApiCreateScriptRequest
    {
        public string Name { get; set; } = string.Empty;
        public string SqlScript { get; set; } = string.Empty;
        public string? RollbackScript { get; set; }
        public long? BatchId { get; set; }
        public ApiNewBatchPayload? Batch { get; set; }
        public long DeveloperId { get; set; }
    }

    public class ApiNewBatchPayload
    {
        public string Name { get; set; } = string.Empty;
        public long CreatedBy { get; set; }
    }
}
