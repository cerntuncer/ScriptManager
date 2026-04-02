using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.User.Commands
{
    public class CreateUserRequest : IRequest<CreateUserResponse>
    {
        public string Name { get; set; }
        public string Email { get; set; }

    }
}
