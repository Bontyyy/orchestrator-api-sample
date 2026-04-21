using OrchestratorApiSample.Application.Exceptions;
using OrchestratorApiSample.Application.Interfaces;
using OrchestratorApiSample.Application.Services;
using OrchestratorApiSample.Domain;

namespace OrchestratorApiSample.Tests;

public sealed class WidgetServiceTests
{
    [Fact]
    public async Task CreateAsync_with_valid_data_returns_widget_with_assigned_id()
    {
        var repo = new Mock<IWidgetRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Widget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Widget w, CancellationToken _) => w);

        var service = new WidgetService(repo.Object);

        var widget = await service.CreateAsync("Gadget", "GAD-001", 5, CancellationToken.None);

        widget.Id.Should().NotBeNullOrWhiteSpace();
        widget.Name.Should().Be("Gadget");
        widget.Sku.Should().Be("GAD-001");
        widget.Quantity.Should().Be(5);
        repo.Verify(r => r.AddAsync(It.IsAny<Widget>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_trims_whitespace_from_name_and_sku()
    {
        var repo = new Mock<IWidgetRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Widget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Widget w, CancellationToken _) => w);

        var service = new WidgetService(repo.Object);

        var widget = await service.CreateAsync("  Gadget  ", "  GAD-001  ", 5, CancellationToken.None);

        widget.Name.Should().Be("Gadget");
        widget.Sku.Should().Be("GAD-001");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateAsync_with_blank_name_throws_ValidationException(string? name)
    {
        var service = new WidgetService(Mock.Of<IWidgetRepository>());

        var act = () => service.CreateAsync(name!, "GAD-001", 5, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Field.Should().Be("name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateAsync_with_blank_sku_throws_ValidationException(string? sku)
    {
        var service = new WidgetService(Mock.Of<IWidgetRepository>());

        var act = () => service.CreateAsync("Gadget", sku!, 5, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Field.Should().Be("sku");
    }

    [Fact]
    public async Task CreateAsync_with_negative_quantity_throws_ValidationException()
    {
        var service = new WidgetService(Mock.Of<IWidgetRepository>());

        var act = () => service.CreateAsync("Gadget", "GAD-001", -1, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Field.Should().Be("quantity");
    }

    [Fact]
    public async Task CreateAsync_with_zero_quantity_is_allowed()
    {
        var repo = new Mock<IWidgetRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Widget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Widget w, CancellationToken _) => w);

        var service = new WidgetService(repo.Object);

        var widget = await service.CreateAsync("Gadget", "GAD-001", 0, CancellationToken.None);

        widget.Quantity.Should().Be(0);
    }

    [Fact]
    public async Task GetByIdAsync_returns_widget_when_repository_has_it()
    {
        var stored = new Widget("abc123", "Gadget", "GAD-001", 5);
        var repo = new Mock<IWidgetRepository>();
        repo.Setup(r => r.GetByIdAsync("abc123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        var service = new WidgetService(repo.Object);

        var widget = await service.GetByIdAsync("abc123", CancellationToken.None);

        widget.Should().Be(stored);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_repository_has_nothing()
    {
        var repo = new Mock<IWidgetRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Widget?)null);

        var service = new WidgetService(repo.Object);

        var widget = await service.GetByIdAsync("nonexistent", CancellationToken.None);

        widget.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetByIdAsync_with_blank_id_throws_ValidationException(string? id)
    {
        var service = new WidgetService(Mock.Of<IWidgetRepository>());

        var act = () => service.GetByIdAsync(id!, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Field.Should().Be("id");
    }
}
