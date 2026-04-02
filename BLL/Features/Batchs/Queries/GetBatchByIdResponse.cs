using BLL.Common;
using DAL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Batchs.Queries
{
    public class GetBatchByIdResponse : BaseResponse
    {
        public long BatchId { get; set; }
        public string Name { get; set; }

        public List<BatchScriptDto> Scripts { get; set; }
    }

    public class BatchScriptDto
    {
        public long ScriptId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
    }
}