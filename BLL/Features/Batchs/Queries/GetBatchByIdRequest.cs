using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Batchs.Queries
{
    public class GetBatchByIdRequest : IRequest<GetBatchByIdResponse>
    {
        public long BatchId { get; set; }
    }
}