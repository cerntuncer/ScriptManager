namespace BLL.Features.Batchs.Queries
{
    public class GetBatchListResponse
    {
        public long BatchId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ScriptCount { get; set; }
    }
}