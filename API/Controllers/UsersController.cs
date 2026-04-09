using BLL.Features.User.Commands;
using BLL.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }


        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserRequest request)
        {
            var response = await _mediator.Send(request);
            return Ok(response);
        }

        /// <summary>"list" segmentinin {id} olarak yanlış eşleşmesini engeller.</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var request = new GetUserRequest { Id = id };
            var response = await _mediator.Send(request);
            return Ok(response);
        }

        [HttpGet("by-email")]
        public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
        {
            var request = new GetUserByEmailRequest { Email = email };
            var response = await _mediator.Send(request);
            return Ok(response);
        }

    }
}