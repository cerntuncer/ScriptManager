using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Batchs.Queries
{
    public class GetBatchListRequest : IRequest<List<GetBatchListResponse>>
    {
    }
}