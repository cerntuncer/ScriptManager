using BLL.Common;
using System.Collections.Generic;

namespace BLL.Features.Batchs.Queries
{
    public class GetBatchByIdResponse : BaseResponse
    {
        public long BatchId { get; set; }
        public string Name { get; set; } = string.Empty;

        public List<BatchScriptDto> Scripts { get; set; } = new();
    }

    public class BatchScriptDto
    {
        public long ScriptId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
