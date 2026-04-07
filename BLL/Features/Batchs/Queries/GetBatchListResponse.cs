using BLL.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Batchs.Queries
{
    public class GetBatchListResponse
    {
        public long BatchId { get; set; }
        public string Name { get; set; }
        public int ScriptCount { get; set; }
    }
}