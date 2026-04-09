using BLL.Features.Batchs.Commands;
using BLL.Features.Batchs.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/batches")]
    public class BatchController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BatchController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBatchRequest request)
        {
            var result = await _mediator.Send(request);

            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetById), new { id = result.BatchId }, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _mediator.Send(new GetBatchListRequest());

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Geçersiz batch id." });

            var result = await _mediator.Send(new GetBatchByIdRequest { BatchId = id });

            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
    }
}