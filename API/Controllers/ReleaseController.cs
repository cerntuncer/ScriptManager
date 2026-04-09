using BLL.Features.Releases.Commands;
using BLL.Features.Releases.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/releases")]
    public class ReleaseController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReleaseController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReleaseRequest request)
        {
            var result = await _mediator.Send(request);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetById), new { id = result.ReleaseId }, result);
        }

        /// <summary>Sürüm güncelleme API'de desteklenmez.</summary>
        [HttpPut("{id:long}")]
        public IActionResult Update(long id) =>
            BadRequest(new { message = "Release güncelleme kaldırıldı; web arayüzünden yönetin." });

        /// <summary>Sürüm silinmez; iptal endpoint'ini kullanın.</summary>
        [HttpPost("{id:long}/cancel")]
        public async Task<IActionResult> Cancel(long id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz release id." });

            var result = await _mediator.Send(new CancelReleaseRequest { ReleaseId = id });
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

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz release id." });

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