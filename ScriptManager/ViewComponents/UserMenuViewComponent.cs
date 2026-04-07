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
                return View(new UserMenuViewModel { FullName = "Geliştirme", Role = "Giriş kapalı" });
            }

            var name = u.FindFirstValue(ClaimTypes.Name)
                       ?? u.Identity?.Name
                       ?? "Kullanıcı";
            var role = u.FindFirstValue(ClaimTypes.Role) ?? "";
            return View(new UserMenuViewModel { FullName = name, Role = role });
        }
    }

    public class UserMenuViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
