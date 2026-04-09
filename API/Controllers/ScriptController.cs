using BLL.Features.Scripts.Commands;
using BLL.Features.Scripts.Queries;
using BLL.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/scripts")]
    public class ScriptController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ScriptController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateScriptRequest request)
        {
            var result = await _mediator.Send(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateScriptRequest request)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz script id." });
            request.ScriptId = id;

            var result = await _mediator.Send(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(DeleteScriptRequest request)
        {
            var result = await _mediator.Send(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var result = await _mediator.Send(new GetScriptByIdRequest { ScriptId = id });

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetList()
        {
            var result = await _mediator.Send(new GetScriptListRequest());
            return Ok(result);
        }

    }
}