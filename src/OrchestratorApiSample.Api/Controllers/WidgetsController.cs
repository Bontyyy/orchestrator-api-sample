using Microsoft.AspNetCore.Mvc;
using OrchestratorApiSample.Application.Exceptions;
using OrchestratorApiSample.Application.Services;
using OrchestratorApiSample.Domain;

namespace OrchestratorApiSample.Api.Controllers;

[ApiController]
[Route("widgets")]
public sealed class WidgetsController : ControllerBase
{
    private readonly WidgetService _service;

    public WidgetsController(WidgetService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<Widget>> Create(
        [FromBody] CreateWidgetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var widget = await _service.CreateAsync(
                request.Name,
                request.Sku,
                request.Quantity,
                cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = widget.Id }, widget);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = "validation_failed", field = ex.Field, reason = ex.Reason });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Widget>> GetById(string id, CancellationToken cancellationToken)
    {
        var widget = await _service.GetByIdAsync(id, cancellationToken);

        return widget is null ? NotFound() : Ok(widget);
    }
}

public sealed record CreateWidgetRequest(string Name, string Sku, int Quantity);
