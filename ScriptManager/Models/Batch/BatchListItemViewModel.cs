namespace ScriptManager.Models.Batch
{
    public class BatchListItemViewModel
    {
        public long BatchId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    public class BatchDetailViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long BatchId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<BatchScriptItemViewModel> Scripts { get; set; } = new();
        public class BatchScriptItemViewModel
        {
            public long ScriptId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }
    }
}