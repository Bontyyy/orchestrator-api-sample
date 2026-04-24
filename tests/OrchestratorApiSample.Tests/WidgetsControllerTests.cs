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

    // AC-1: no page_size → returns HTTP 200 with up to 50 widgets
    [Fact]
    public async Task GetList_without_page_size_returns_Ok_with_up_to_50_widgets()
    {
        var controller = BuildController();
        for (var i = 0; i < 60; i++)
        {
            await controller.Create(new CreateWidgetRequest($"Widget {i}", $"SKU-{i}", i), CancellationToken.None);
        }

        var result = await controller.GetList(pageSize: null, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Which;
        var widgets = ok.Value.Should().BeAssignableTo<IReadOnlyList<Widget>>().Which;
        widgets.Should().HaveCount(50);
    }

    [Fact]
    public async Task GetList_without_page_size_returns_Ok_when_fewer_than_50_widgets_exist()
    {
        var controller = BuildController();
        await controller.Create(new CreateWidgetRequest("Widget A", "SKU-A", 1), CancellationToken.None);
        await controller.Create(new CreateWidgetRequest("Widget B", "SKU-B", 2), CancellationToken.None);

        var result = await controller.GetList(pageSize: null, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Which;
        var widgets = ok.Value.Should().BeAssignableTo<IReadOnlyList<Widget>>().Which;
        widgets.Should().HaveCount(2);
    }

    // AC-2: page_size=N with 1 ≤ N ≤ 500 → returns HTTP 200 with up to N widgets
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(500)]
    public async Task GetList_with_valid_page_size_returns_Ok_with_up_to_N_widgets(int pageSize)
    {
        var controller = BuildController();
        for (var i = 0; i < 10; i++)
        {
            await controller.Create(new CreateWidgetRequest($"Widget {i}", $"SKU-{i}", i), CancellationToken.None);
        }

        var result = await controller.GetList(pageSize: pageSize, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Which;
        var widgets = ok.Value.Should().BeAssignableTo<IReadOnlyList<Widget>>().Which;
        widgets.Count.Should().BeLessThanOrEqualTo(pageSize);
    }

    [Fact]
    public async Task GetList_with_page_size_10_returns_exactly_10_when_more_exist()
    {
        var controller = BuildController();
        for (var i = 0; i < 20; i++)
        {
            await controller.Create(new CreateWidgetRequest($"Widget {i}", $"SKU-{i}", i), CancellationToken.None);
        }

        var result = await controller.GetList(pageSize: 10, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Which;
        var widgets = ok.Value.Should().BeAssignableTo<IReadOnlyList<Widget>>().Which;
        widgets.Should().HaveCount(10);
    }

    // AC-3: page_size > 500 → returns HTTP 400 with {"error": {"code": "page_size_over_limit", ...}}
    [Theory]
    [InlineData(501)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public async Task GetList_with_page_size_over_500_returns_BadRequest_with_page_size_over_limit_code(int pageSize)
    {
        var controller = BuildController();

        var result = await controller.GetList(pageSize: pageSize, CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Which;
        var body = badRequest.Value!;
        var errorProp = body.GetType().GetProperty("error");
        errorProp.Should().NotBeNull();
        var errorValue = errorProp!.GetValue(body)!;
        var codeProp = errorValue.GetType().GetProperty("code");
        codeProp.Should().NotBeNull();
        codeProp!.GetValue(errorValue).Should().Be("page_size_over_limit");
    }
}
