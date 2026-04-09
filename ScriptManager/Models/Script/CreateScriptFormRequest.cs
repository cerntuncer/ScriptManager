namespace ScriptManager.Models.Script
{
    public class CreateScriptFormRequest
    {
        public string Name { get; set; } = string.Empty;
        public string SqlScript { get; set; } = string.Empty;
        public string? RollbackScript { get; set; }
        public long DeveloperId { get; set; }

        /// <summary>Havuzdaki yaprak batch veya boş.</summary>
        public long? BatchId { get; set; }
    }
}
