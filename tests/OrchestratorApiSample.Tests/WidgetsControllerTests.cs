using Microsoft.AspNetCore.Mvc;
using OrchestratorApiSample.Api.Controllers;
using OrchestratorApiSample.Api.Persistence;
using OrchestratorApiSample.Application.Services;
using OrchestratorApiSample.Domain;

namespace OrchestratorApiSample.Tests;

public sealed class WidgetsControllerTests
{
    private static WidgetsController BuildController()
    {
        var repo = new InMemoryWidgetRepository();
        var service = new WidgetService(repo);
        return new WidgetsController(service);
    }

    [Fact]
    public async Task Create_with_valid_body_returns_Created_with_widget()
    {
        var controller = BuildController();
        var request = new CreateWidgetRequest("Gadget", "GAD-001", 5);

        var result = await controller.Create(request, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Which;
        var widget = created.Value.Should().BeOfType<Widget>().Which;
        widget.Name.Should().Be("Gadget");
        created.ActionName.Should().Be(nameof(WidgetsController.GetById));
    }

    [Fact]
    public async Task Create_with_blank_name_returns_BadRequest_with_field_info()
    {
        var controller = BuildController();
        var request = new CreateWidgetRequest("", "GAD-001", 5);

        var result = await controller.Create(request, CancellationToken.None);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Which;
        badRequest.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_returns_NotFound_when_widget_does_not_exist()
    {
        var controller = BuildController();

        var result = await controller.GetById("nonexistent-id", CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_returns_Ok_with_widget_when_it_exists()
    {
        var controller = BuildController();
        var createResult = await controller.Create(
            new CreateWidgetRequest("Gadget", "GAD-001", 5),
            CancellationToken.None);
        var createdWidget = ((CreatedAtActionResult)createResult.Result!).Value as Widget;

        var result = await controller.GetById(createdWidget!.Id, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Which;
        ok.Value.Should().Be(createdWidget);
    }

    [Fact]
    public async Task Delete_with_existing_id_returns_NoContent_and_widget_is_gone()
    {
        var controller = BuildController();
        var createResult = await controller.Create(
            new CreateWidgetRequest("Gadget", "GAD-001", 5),
            CancellationToken.None);
        var createdWidget = ((CreatedAtActionResult)createResult.Result!).Value as Widget;

        var deleteResult = await controller.Delete(createdWidget!.Id, CancellationToken.None);

        deleteResult.Should().BeOfType<NoContentResult>();

        var getResult = await controller.GetById(createdWidget.Id, CancellationToken.None);
        getResult.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_with_unknown_id_returns_NoContent_idempotently()
    {
        var controller = BuildController();

        var deleteResult = await controller.Delete("nonexistent-id", CancellationToken.None);

        deleteResult.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_with_blank_id_returns_BadRequest_with_field_info()
    {
        var controller = BuildController();

        var deleteResult = await controller.Delete("   ", CancellationToken.None);

        deleteResult.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetCount_with_empty_store_returns_Ok_with_count_zero()
    {
        var controller = BuildController();

        var result = await controller.GetCount(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Which;
        ok.Value.Should().BeEquivalentTo(new { count = 0 });
    }

    [Fact]
    public async Task GetCount_after_creating_widgets_returns_Ok_with_correct_count()
    {
        var controller = BuildController();
        await controller.Create(new CreateWidgetRequest("Widget A", "SKU-A", 1), CancellationToken.None);
        await controller.Create(new CreateWidgetRequest("Widget B", "SKU-B", 2), CancellationToken.None);

        var result = await controller.GetCount(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Which;
        ok.Value.Should().BeEquivalentTo(new { count = 2 });
    }
}
