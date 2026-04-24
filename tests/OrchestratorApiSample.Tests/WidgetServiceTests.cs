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
    public async Task CreateAsync_with_quantity_above_ceiling_throws_ValidationException()
    {
        var service = new WidgetService(Mock.Of<IWidgetRepository>());

        var act = () => service.CreateAsync("Gadget", "GAD-001", 10_001, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Field.Should().Be("quantity");
        ex.Which.Reason.Should().Be("must be at most 10000");
    }

    [Fact]
    public async Task CreateAsync_with_quantity_at_ceiling_is_allowed()
    {
        var repo = new Mock<IWidgetRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Widget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Widget w, CancellationToken _) => w);

        var service = new WidgetService(repo.Object);

        var widget = await service.CreateAsync("Gadget", "GAD-001", 10_000, CancellationToken.None);

        widget.Quantity.Should().Be(10_000);
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

    [Fact]
    public async Task DeleteAsync_with_known_id_delegates_to_repository()
    {
        var repo = new Mock<IWidgetRepository>();
        repo.Setup(r => r.DeleteAsync("abc123", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new WidgetService(repo.Object);

        await service.DeleteAsync("abc123", CancellationToken.None);

        repo.Verify(r => r.DeleteAsync("abc123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_with_unknown_id_is_idempotent_and_still_delegates()
    {
        var repo = new Mock<IWidgetRepository>();
        repo.Setup(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new WidgetService(repo.Object);

        var act = () => service.DeleteAsync("nonexistent", CancellationToken.None);

        await act.Should().NotThrowAsync();
        repo.Verify(r => r.DeleteAsync("nonexistent", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task DeleteAsync_with_blank_id_throws_ValidationException_and_skips_repository(string? id)
    {
        var repo = new Mock<IWidgetRepository>();
        var service = new WidgetService(repo.Object);

        var act = () => service.DeleteAsync(id!, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Field.Should().Be("id");
        repo.Verify(r => r.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCountAsync_with_zero_widgets_returns_zero()
    {
        var repo = new Mock<IWidgetRepository>();
        repo.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var service = new WidgetService(repo.Object);

        var count = await service.GetCountAsync(CancellationToken.None);

        count.Should().Be(0);
        repo.Verify(r => r.CountAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCountAsync_with_n_widgets_returns_n()
    {
        var repo = new Mock<IWidgetRepository>();
        repo.Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var service = new WidgetService(repo.Object);

        var count = await service.GetCountAsync(CancellationToken.None);

        count.Should().Be(3);
        repo.Verify(r => r.CountAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC-1 / AC-2: valid page sizes delegate to repository
    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(500)]
    public async Task GetListAsync_with_valid_page_size_delegates_to_repository(int pageSize)
    {
        IReadOnlyList<Widget> stored = new List<Widget> { new Widget("id1", "W", "SKU", 1) };
        var repo = new Mock<IWidgetRepository>();
        repo.Setup(r => r.GetListAsync(pageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        var service = new WidgetService(repo.Object);

        var result = await service.GetListAsync(pageSize, CancellationToken.None);

        result.Should().BeSameAs(stored);
        repo.Verify(r => r.GetListAsync(pageSize, It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC-3: page_size > 500 throws ValidationException before hitting repository
    [Theory]
    [InlineData(501)]
    [InlineData(1000)]
    public async Task GetListAsync_with_page_size_over_500_throws_ValidationException(int pageSize)
    {
        var repo = new Mock<IWidgetRepository>();
        var service = new WidgetService(repo.Object);

        var act = () => service.GetListAsync(pageSize, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Field.Should().Be("pageSize");
        repo.Verify(r => r.GetListAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
