using Microsoft.AspNetCore.Mvc;
using OrchestratorApiSample.Api.Controllers;
using OrchestratorApiSample.Api.Persistence;
using OrchestratorApiSample.Application.Services;
using OrchestratorApiSample.Domain;

namespace OrchestratorApiSample.Tests;

public sealed class WidgetsControllerBulkTests
{
    private static WidgetsController BuildController()
    {
        var repo = new InMemoryWidgetRepository();
        var service = new WidgetService(repo);
        return new WidgetsController(service);
    }

    // AC-1 / AC-6: valid batch returns 201 with array body containing all created widgets
    [Fact]
    public async Task BulkCreate_with_valid_batch_returns_201_with_created_widgets()
    {
        var controller = BuildController();
        var requests = new List<CreateWidgetRequest>
        {
            new("Widget A", "SKU-A", 1),
            new("Widget B", "SKU-B", 2),
            new("Widget C", "SKU-C", 0),
        };

        var result = await controller.BulkCreate(requests, CancellationToken.None);

        var statusResult = result.Should().BeOfType<ObjectResult>().Which;
        statusResult.StatusCode.Should().Be(201);
        var widgets = statusResult.Value.Should().BeAssignableTo<IReadOnlyList<Widget>>().Which;
        widgets.Should().HaveCount(3);
        widgets.Should().OnlyContain(w => !string.IsNullOrWhiteSpace(w.Id));
        widgets.Select(w => w.Name).Should().BeEquivalentTo("Widget A", "Widget B", "Widget C");
    }

    [Fact]
    public async Task BulkCreate_with_single_valid_item_returns_201_with_single_element_array()
    {
        var controller = BuildController();
        var requests = new List<CreateWidgetRequest> { new("Gadget", "GAD-001", 5) };

        var result = await controller.BulkCreate(requests, CancellationToken.None);

        var statusResult = result.Should().BeOfType<ObjectResult>().Which;
        statusResult.StatusCode.Should().Be(201);
        var widgets = statusResult.Value.Should().BeAssignableTo<IReadOnlyList<Widget>>().Which;
        widgets.Should().HaveCount(1);
        widgets[0].Name.Should().Be("Gadget");
        widgets[0].Sku.Should().Be("GAD-001");
        widgets[0].Quantity.Should().Be(5);
        widgets[0].Id.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task BulkCreate_with_exactly_50_items_returns_201()
    {
        var controller = BuildController();
        var requests = Enumerable.Range(1, 50)
            .Select(i => new CreateWidgetRequest($"Widget {i}", $"SKU-{i}", i))
            .ToList();

        var result = await controller.BulkCreate(requests, CancellationToken.None);

        var statusResult = result.Should().BeOfType<ObjectResult>().Which;
        statusResult.StatusCode.Should().Be(201);
        var widgets = statusResult.Value.Should().BeAssignableTo<IReadOnlyList<Widget>>().Which;
        widgets.Should().HaveCount(50);
    }

    // AC-2 / AC-6: oversized batch returns 400 with error.code == "batch_size_exceeded"
    [Fact]
    public async Task BulkCreate_with_51_items_returns_400_with_batch_size_exceeded_code()
    {
        var controller = BuildController();
        var requests = Enumerable.Range(1, 51)
            .Select(i => new CreateWidgetRequest($"Widget {i}", $"SKU-{i}", i))
            .ToList();

        var result = await controller.BulkCreate(requests, CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Which;
        var body = badRequest.Value!;
        var errorProp = body.GetType().GetProperty("error");
        errorProp.Should().NotBeNull();
        var errorValue = errorProp!.GetValue(body)!;
        var codeProp = errorValue.GetType().GetProperty("code");
        codeProp.Should().NotBeNull();
        codeProp!.GetValue(errorValue).Should().Be("batch_size_exceeded");
        var messageProp = errorValue.GetType().GetProperty("message");
        messageProp.Should().NotBeNull();
        ((string)messageProp!.GetValue(errorValue)!).Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(51)]
    [InlineData(100)]
    public async Task BulkCreate_with_batch_exceeding_max_returns_400_with_batch_size_exceeded_code(int count)
    {
        var controller = BuildController();
        var requests = Enumerable.Range(1, count)
            .Select(i => new CreateWidgetRequest($"Widget {i}", $"SKU-{i}", i))
            .ToList();

        var result = await controller.BulkCreate(requests, CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Which;
        var body = badRequest.Value!;
        var errorProp = body.GetType().GetProperty("error");
        var errorValue = errorProp!.GetValue(body)!;
        var codeProp = errorValue.GetType().GetProperty("code");
        codeProp!.GetValue(errorValue).Should().Be("batch_size_exceeded");
    }

    // AC-3 / AC-6: invalid batch returns 422 with error.code == "batch_validation_failure" and non-empty error.failures
    [Fact]
    public async Task BulkCreate_with_one_invalid_item_returns_422_with_batch_validation_failure_code()
    {
        var controller = BuildController();
        var requests = new List<CreateWidgetRequest>
        {
            new("Valid Widget", "SKU-001", 5),
            new("", "SKU-002", 5),           // blank name — index 1
        };

        var result = await controller.BulkCreate(requests, CancellationToken.None);

        var unprocessable = result.Should().BeOfType<UnprocessableEntityObjectResult>().Which;
        var body = unprocessable.Value!;
        var errorProp = body.GetType().GetProperty("error");
        errorProp.Should().NotBeNull();
        var errorValue = errorProp!.GetValue(body)!;
        var codeProp = errorValue.GetType().GetProperty("code");
        codeProp!.GetValue(errorValue).Should().Be("batch_validation_failure");
        var failuresProp = errorValue.GetType().GetProperty("failures");
        failuresProp.Should().NotBeNull();
        var failures = failuresProp!.GetValue(errorValue) as System.Array;
        failures.Should().NotBeNull();
        failures!.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task BulkCreate_with_invalid_item_returns_422_with_correct_index_in_failures()
    {
        var controller = BuildController();
        var requests = new List<CreateWidgetRequest>
        {
            new("Valid Widget", "SKU-001", 5),
            new("Another Valid Widget", "SKU-002", 10),
            new("Bad Widget", "SKU-003", -1),  // negative quantity — index 2
        };

        var result = await controller.BulkCreate(requests, CancellationToken.None);

        var unprocessable = result.Should().BeOfType<UnprocessableEntityObjectResult>().Which;
        var body = unprocessable.Value!;
        var errorProp = body.GetType().GetProperty("error");
        var errorValue = errorProp!.GetValue(body)!;
        var failuresProp = errorValue.GetType().GetProperty("failures");
        var failures = failuresProp!.GetValue(errorValue) as System.Array;
        failures.Should().NotBeNull();
        failures!.Length.Should().Be(1);

        var firstFailure = failures.GetValue(0)!;
        var indexProp = firstFailure.GetType().GetProperty("index");
        indexProp.Should().NotBeNull();
        indexProp!.GetValue(firstFailure).Should().Be(2);

        var reasonProp = firstFailure.GetType().GetProperty("reason");
        reasonProp.Should().NotBeNull();
        ((string)reasonProp!.GetValue(firstFailure)!).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task BulkCreate_with_invalid_items_does_not_persist_any_widgets()
    {
        // Use the real controller (InMemoryWidgetRepository) — verify GetCount stays zero
        var controller = BuildController();
        var requests = new List<CreateWidgetRequest>
        {
            new("Valid Widget", "SKU-001", 5),
            new("", "SKU-002", 5),  // blank name
        };

        await controller.BulkCreate(requests, CancellationToken.None);

        var countResult = await controller.GetCount(CancellationToken.None);
        var ok = countResult.Should().BeOfType<OkObjectResult>().Which;
        ok.Value.Should().BeEquivalentTo(new { count = 0 });
    }

    [Fact]
    public async Task BulkCreate_with_all_valid_items_persists_all_widgets()
    {
        var controller = BuildController();
        var requests = new List<CreateWidgetRequest>
        {
            new("Widget A", "SKU-A", 1),
            new("Widget B", "SKU-B", 2),
        };

        await controller.BulkCreate(requests, CancellationToken.None);

        var countResult = await controller.GetCount(CancellationToken.None);
        var ok = countResult.Should().BeOfType<OkObjectResult>().Which;
        ok.Value.Should().BeEquivalentTo(new { count = 2 });
    }
}
