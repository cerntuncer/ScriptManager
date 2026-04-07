namespace ScriptManager.Models.Script
{
    public class ApiMutationResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public long ScriptId { get; set; }
        public long? BatchId { get; set; }

        /// <summary>Liste satırı için (AJAX oluşturma sonrası).</summary>
        public string? ScriptName { get; set; }
        public string? Status { get; set; }
        public string? BatchName { get; set; }
        public string? DeveloperName { get; set; }
        public bool HasRollback { get; set; }
        public string? CreatedAtDisplay { get; set; }

        /// <summary>Listede sil butonu (oluşturandan hemen sonra satır eklerken).</summary>
        public bool CanDelete { get; set; }
    }
}
