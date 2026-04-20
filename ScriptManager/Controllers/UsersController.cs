using Microsoft.AspNetCore.Mvc;

namespace ScriptManager.Controllers;

public class UsersController : Controller
{
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Index() => NotFound();

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult SetActive([FromBody] object? body) => NotFound();
}
