namespace ScriptManager.Models.Script
{
    public class ValidateSchemaTextRequest
    {
        public string? SqlScript { get; set; }
        public string? RollbackScript { get; set; }
        public long TargetEnvironmentId { get; set; }
    }
}
