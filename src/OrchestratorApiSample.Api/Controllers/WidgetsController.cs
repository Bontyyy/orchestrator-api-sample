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

    private const int _defaultPageSize = 50;

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery(Name = "page_size")] int? pageSize,
        CancellationToken cancellationToken)
    {
        var effectivePageSize = pageSize ?? _defaultPageSize;

        try
        {
            var widgets = await _service.GetListAsync(effectivePageSize, cancellationToken);
            return Ok(widgets);
        }
        catch (ValidationException)
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "page_size_over_limit",
                    message = $"page_size must be between 1 and 500; received {effectivePageSize}.",
                },
            });
        }
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

    [HttpGet("count")]
    public async Task<IActionResult> GetCount(CancellationToken cancellationToken)
    {
        var count = await _service.GetCountAsync(cancellationToken);
        return Ok(new { count });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeleteAsync(id, cancellationToken);

            return NoContent();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = "validation_failed", field = ex.Field, reason = ex.Reason });
        }
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkCreate(
        [FromBody] IReadOnlyList<CreateWidgetRequest> requests,
        CancellationToken cancellationToken)
    {
        var items = requests
            .Select(r => new BulkCreateItem(r.Name, r.Sku, r.Quantity))
            .ToList();

        var result = await _service.BulkCreateAsync(items, cancellationToken);

        return result.ResultOutcome switch
        {
            BulkCreateResult.Outcome.Created =>
                StatusCode(201, result.CreatedWidgets),

            BulkCreateResult.Outcome.BatchSizeExceeded =>
                BadRequest(new
                {
                    error = new
                    {
                        code = "batch_size_exceeded",
                        message = $"Batch size of {result.ReceivedCount} exceeds the maximum allowed limit of {result.MaxAllowed} items.",
                    },
                }),

            BulkCreateResult.Outcome.ValidationFailure =>
                UnprocessableEntity(new
                {
                    error = new
                    {
                        code = "batch_validation_failure",
                        failures = result.Failures.Select(f => new { index = f.Index, reason = f.Reason }).ToArray(),
                    },
                }),

            _ => StatusCode(500),
        };
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateWidgetRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var widget = await _service.UpdateAsync(
                id,
                request.Name,
                request.Sku,
                request.Quantity,
                cancellationToken);

            if (widget is null)
            {
                return NotFound(new
                {
                    error = new
                    {
                        code = "widget_not_found",
                        message = $"Widget with id '{id}' was not found.",
                    },
                });
            }

            return Ok(widget);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { error = "validation_failed", field = ex.Field, reason = ex.Reason });
        }
    }
}

public sealed record CreateWidgetRequest(string Name, string Sku, int Quantity);

public sealed record UpdateWidgetRequest(string? Name, string? Sku, int? Quantity);
