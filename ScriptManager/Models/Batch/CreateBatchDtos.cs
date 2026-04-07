namespace ScriptManager.Models.Batch
{
    public class CreateBatchFormRequest
    {
        public string Name { get; set; } = string.Empty;
        public long CreatedBy { get; set; }
    }

    /// <summary>API POST api/Batch gövdesi.</summary>
    public class ApiCreateBatchRequest
    {
        public string Name { get; set; } = string.Empty;
        public long CreatedBy { get; set; }
    }

    public class BatchCreateResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public long BatchId { get; set; }
    }
}
