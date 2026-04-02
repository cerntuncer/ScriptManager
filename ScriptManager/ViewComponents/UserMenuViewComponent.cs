using Microsoft.AspNetCore.Mvc;

namespace ScriptManager.ViewComponents
{
    public class UserMenuViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var model = new UserMenuViewModel
            {
                FullName = "Demo User",
                Role = "Admin"
            };

            return View(model);
        }
    }

    public class UserMenuViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}