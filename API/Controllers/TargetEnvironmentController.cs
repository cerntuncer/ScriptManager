using BLL.Features.TargetEnvironments.Commands;
using BLL.Features.TargetEnvironments.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/target-environments")]
    public class TargetEnvironmentController : ControllerBase
    {
        private readonly IMediator _mediator;

        public TargetEnvironmentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? projectName)
        {
            var result = await _mediator.Send(new GetTargetEnvironmentsRequest { ProjectName = projectName });
            return Ok(result);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _mediator.Send(new GetTargetEnvironmentByIdRequest { Id = id });
            if (result == null)
                return NotFound(new { message = "Hedef ortam bulunamadı." });
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTargetEnvironmentRequest request)
        {
            var result = await _mediator.Send(request);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateTargetEnvironmentRequest request)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz ID." });
            request.Id = id;
            var result = await _mediator.Send(request);
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var result = await _mediator.Send(new DeleteTargetEnvironmentRequest { Id = id });
            if (!result.Success)
                return BadRequest(result);
            return Ok(result);
        }
    }
}
