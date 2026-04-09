using BLL.Common;
using DAL.Enums;

namespace BLL.Features.Users.Queries
{
    public class GetUserResponse : BaseResponse
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public int ScriptCount { get; set; }
        public int ResolvedConflictCount { get; set; }
    }
}
