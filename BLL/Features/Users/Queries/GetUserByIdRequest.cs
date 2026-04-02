using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Users.Queries
{
    public class GetUserByIdRequest : IRequest<GetUserByIdResponse>
    {
        public long Id { get; set; }
    }
}