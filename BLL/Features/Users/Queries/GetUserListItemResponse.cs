namespace BLL.Features.Users.Queries
{
    public class GetUserListItemResponse
    {
        public long UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
