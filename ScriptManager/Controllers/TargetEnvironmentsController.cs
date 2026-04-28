using BLL.Features.TargetEnvironments.Commands;
using BLL.Features.TargetEnvironments.Queries;
using DAL.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ScriptManager.Controllers
{
    public class TargetEnvironmentsController : Controller
    {
        private readonly IMediator _mediator;

        public TargetEnvironmentsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Hedef Ortamlar";
            var items = await _mediator.Send(new GetTargetEnvironmentsRequest());
            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? projectName)
        {
            var items = await _mediator.Send(new GetTargetEnvironmentsRequest { ProjectName = projectName });
            return Json(items.Select(e => new
            {
                id = e.Id,
                projectName = e.ProjectName,
                environmentType = (int)e.EnvironmentType,
                environmentTypeName = e.EnvironmentTypeName,
                description = e.Description,
                label = $"{e.ProjectName} — {e.EnvironmentTypeName}"
            }));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateTargetEnvironmentRequest? request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Geçersiz istek." });

            var result = await _mediator.Send(request);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            return Json(new { success = true, message = result.Message, id = result.TargetEnvironmentId });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateTargetEnvironmentRequest? request)
        {
            if (request == null || id <= 0)
                return BadRequest(new { success = false, message = "Geçersiz istek." });

            request.Id = id;
            var result = await _mediator.Send(request);
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            return Json(new { success = true, message = result.Message });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            if (id <= 0)
                return BadRequest(new { success = false, message = "Geçersiz ID." });

            var result = await _mediator.Send(new DeleteTargetEnvironmentRequest { Id = id });
            if (!result.Success)
                return BadRequest(new { success = false, message = result.Message });

            return Json(new { success = true, message = result.Message });
        }
    }
}
