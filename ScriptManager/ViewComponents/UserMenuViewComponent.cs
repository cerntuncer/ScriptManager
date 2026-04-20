using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace ScriptManager.ViewComponents
{
    public class UserMenuViewComponent(IHttpContextAccessor http) : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var u = http.HttpContext?.User;
            if (u?.Identity?.IsAuthenticated != true)
            {
                return View(new UserMenuViewModel
                {
                    FullName = "Misafir",
                    Role = "",
                    RoleDisplay = "",
                    IsAuthenticated = false
                });
            }

            var name = u.FindFirstValue(ClaimTypes.Name)
                       ?? u.Identity?.Name
                       ?? "Kullanıcı";
            var role = u.FindFirstValue(ClaimTypes.Role) ?? "";
            var roleDisplay = role switch
            {
                "Developer" => "Geliştirici",
                "Tester" => "Testçi",
                _ => role
            };
            return View(new UserMenuViewModel
            {
                FullName = name,
                Role = role,
                RoleDisplay = roleDisplay,
                IsAuthenticated = true
            });
        }
    }

    public class UserMenuViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string RoleDisplay { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; }
    }
}
