using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Users.Queries
{
    public class GetUserByEmailRequest : IRequest<GetUserByEmailResponse>
    {
        public string Email { get; set; }
    }
}