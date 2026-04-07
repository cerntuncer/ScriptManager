namespace ScriptManager.Models.Script
{
    public class CreateScriptFormRequest
    {
        /// <summary>Dolu ise script bu batch’e bağlanır; boş/0 ise havuzda (batch yok).</summary>
        public long? BatchId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SqlScript { get; set; } = string.Empty;
        public string? RollbackScript { get; set; }
        public long DeveloperId { get; set; }
    }
}
