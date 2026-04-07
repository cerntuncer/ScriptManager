using BLL.Features.Releases.Commands;
using BLL.Features.Releases.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReleaseController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReleaseController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateReleaseApiRequest request)
        {
            var result = await _mediator.Send(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update(UpdateReleaseRequest request)
        {
            var result = await _mediator.Send(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(DeleteReleaseApiRequest request)
        {
            var result = await _mediator.Send(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetReleasesRequest());
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Detail(long id)
        {
            var result = await _mediator.Send(new GetReleaseByIdRequest
            {
                ReleaseId = id
            });

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
    }
}