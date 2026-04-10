using BLL.Features.Scripts.Commands;
using BLL.Features.Scripts.Queries;
using BLL.Features.Users.Queries;
using BLL.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/scripts")]
    public class ScriptController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ISqlScriptSyntaxValidator _sqlSyntax;

        public ScriptController(IMediator mediator, ISqlScriptSyntaxValidator sqlSyntax)
        {
            _mediator = mediator;
            _sqlSyntax = sqlSyntax;
        }

        [HttpPost("validate-sql")]
        public IActionResult ValidateSql([FromBody] SqlSyntaxValidationRequest? body)
        {
            if (body == null)
                return BadRequest(new { success = false, message = "Geçersiz istek." });

            var issues = new List<SqlScriptSyntaxIssue>();
            issues.AddRange(_sqlSyntax.Validate(body.SqlScript, "SQL").Issues);
            issues.AddRange(_sqlSyntax.Validate(body.RollbackScript, "Rollback").Issues);

            return Ok(new
            {
                success = true,
                isValid = issues.Count == 0,
                issues = issues.Select(i => new
                {
                    source = i.Source,
                    batchNumber = i.BatchNumber,
                    line = i.Line,
                    column = i.Column,
                    message = i.Message
                })
            });
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