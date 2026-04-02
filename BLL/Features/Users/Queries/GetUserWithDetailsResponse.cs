using BLL.Common;
using DAL.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Features.Users.Queries
{
    public class GetUserWithDetailsResponse : BaseResponse
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }

        public int ScriptCount { get; set; }
        public int CommitCount { get; set; }
        public int ResolvedConflictCount { get; set; }
    }
}