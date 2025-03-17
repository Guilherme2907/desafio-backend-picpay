using MediatR;
using Microsoft.AspNetCore.Mvc;
using PipcPaySimplified.Application.UseCases.CreateAccount;
using PipcPaySimplified.Application.UseCases.MakeTransfer;

namespace PipcPaySimplified.Api.Controllers;

[ApiController]
[Route("accounts")]
public class AccountController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountInput input, CancellationToken cancellationToken)
    {
        await _mediator.Send(input, cancellationToken);

        return Created();
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] MakeTransferInput input, CancellationToken cancellationToken)
    {
        await _mediator.Send(input, cancellationToken);

        return Ok($"Thread ID: {Environment.CurrentManagedThreadId}");
    }
}
