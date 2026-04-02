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


        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var request = new GetUserByIdRequest { Id = id };
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


        [HttpGet("with-details/{id}")]
        public async Task<IActionResult> GetUserWithDetails(int id)
        {
            var request = new GetUserWithDetailsRequest { Id = id };
            var response = await _mediator.Send(request);
            return Ok(response);
        }
    }
}