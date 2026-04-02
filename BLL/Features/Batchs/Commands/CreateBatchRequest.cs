using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Batchs.Commands
{
    public class CreateBatchRequest : IRequest<CreateBatchResponse>
    {
        public string Name { get; set; } // klasör adı
        public int CreatedBy { get; set; }
    }
}