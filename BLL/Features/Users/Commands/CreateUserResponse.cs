using BLL.Common;
using DAL.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.User.Commands
{
    public class CreateUserResponse : BaseResponse
    {
        public long UserId { get; set; }
    }
}