using DAL.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ScriptManager.ViewComponents
{
    public class ConflictCountViewComponent(MyContext db) : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var count = await db.Conflicts
                .Where(c => c.ResolvedAt == null && !c.IsDeleted)
                .CountAsync();

            return View(count);
        }
    }
}
