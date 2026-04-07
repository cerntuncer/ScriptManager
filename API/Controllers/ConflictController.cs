using BLL.Features.Conflicts.Commands;
using BLL.Features.Conflicts.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConflictController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConflictController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("unresolved")]
    public async Task<IActionResult> GetUnresolved(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUnresolvedConflictsRequest(), cancellationToken);
        return Ok(result);
    }

    /// <summary>Çakışma yok onayı: kaydı çözümler ve ilgili script durumlarını günceller.</summary>
    [HttpPost("resolve")]
    public async Task<IActionResult> Resolve([FromBody] ResolveConflictRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}
