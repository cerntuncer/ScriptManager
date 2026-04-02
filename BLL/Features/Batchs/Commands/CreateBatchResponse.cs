using BLL.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Batchs.Commands
{
    public class CreateBatchResponse : BaseResponse
    {
        public long BatchId { get; set; }
    }
}