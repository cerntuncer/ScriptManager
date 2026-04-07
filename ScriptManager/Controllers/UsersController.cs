using DAL.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScriptManager.Data;
using ScriptManager.Models.User;
using ScriptManager.Security;

namespace ScriptManager.Controllers
{
    public class UsersController : Controller
    {
        private readonly MyContext _db;

        public UsersController(MyContext db)
        {
            _db = db;
        }

        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Index()
        {
            if (!AuthHelper.CanManageUsers(User))
                return Forbid();

            ViewData["Title"] = "Kullanıcılar";
            ViewBag.ActorUserId = await AuthHelper.GetActorUserIdAsync(User, _db);
            var users = await UserReadQueries.ListForAdminPanelAsync(_db);
            return View(new UsersIndexViewModel { Users = users });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SetActive([FromBody] SetUserActiveFormRequest? body)
        {
            if (!AuthHelper.CanManageUsers(User))
                return Forbid();

            if (body == null || body.UserId <= 0)
                return BadRequest(new { success = false, message = "Geçersiz istek." });

            var actorId = await AuthHelper.GetActorUserIdAsync(User, _db);
            if (body.UserId == actorId && !body.IsActive)
                return BadRequest(new { success = false, message = "Kendi hesabınızı pasifleştiremezsiniz." });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == body.UserId && !u.IsDeleted);
            if (user == null)
                return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });

            user.IsActive = body.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            _db.Users.Update(user);
            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = body.IsActive ? "Kullanıcı aktifleştirildi." : "Kullanıcı pasifleştirildi."
            });
        }
    }
}
