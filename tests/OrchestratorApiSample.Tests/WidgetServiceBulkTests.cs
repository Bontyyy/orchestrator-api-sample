using OrchestratorApiSample.Application.Interfaces;
using OrchestratorApiSample.Application.Services;
using OrchestratorApiSample.Domain;

namespace OrchestratorApiSample.Tests;

public sealed class WidgetServiceBulkTests
{
    private static WidgetService BuildService(Mock<IWidgetRepository>? repoMock = null)
    {
        if (repoMock is null)
        {
            var mock = new Mock<IWidgetRepository>();
            mock.Setup(r => r.AddAsync(It.IsAny<Widget>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Widget w, CancellationToken _) => w);
            return new WidgetService(mock.Object);
        }

        return new WidgetService(repoMock.Object);
    }

    // AC-1 / AC-5: happy path — all valid items are persisted and returned
    [Fact]
    public async Task BulkCreateAsync_with_all_valid_items_returns_Created_outcome()
    {
        var items = new List<BulkCreateItem>
        {
            new("Widget A", "SKU-A", 1),
            new("Widget B", "SKU-B", 2),
            new("Widget C", "SKU-C", 0),
        };

        var service = BuildService();

        var result = await service.BulkCreateAsync(items, CancellationToken.None);

        result.ResultOutcome.Should().Be(BulkCreateResult.Outcome.Created);
        result.CreatedWidgets.Should().HaveCount(3);
        result.CreatedWidgets.Should().OnlyContain(w => !string.IsNullOrWhiteSpace(w.Id));
    }

    [Fact]
    public async Task BulkCreateAsync_with_single_valid_item_returns_Created_outcome()
    {
        var items = new List<BulkCreateItem> { new("Gadget", "GAD-001", 5) };
        var service = BuildService();

        var result = await service.BulkCreateAsync(items, CancellationToken.None);

        result.ResultOutcome.Should().Be(BulkCreateResult.Outcome.Created);
        result.CreatedWidgets.Should().HaveCount(1);
        result.CreatedWidgets[0].Name.Should().Be("Gadget");
        result.CreatedWidgets[0].Sku.Should().Be("GAD-001");
        result.CreatedWidgets[0].Quantity.Should().Be(5);
    }

    [Fact]
    public async Task BulkCreateAsync_with_exactly_50_valid_items_returns_Created_outcome()
    {
        var items = Enumerable.Range(1, 50)
            .Select(i => new BulkCreateItem($"Widget {i}", $"SKU-{i}", i))
            .ToList();

        var service = BuildService();

        var result = await service.BulkCreateAsync(items, CancellationToken.None);

        result.ResultOutcome.Should().Be(BulkCreateResult.Outcome.Created);
        result.CreatedWidgets.Should().HaveCount(50);
    }

    [Fact]
    public async Task BulkCreateAsync_trims_whitespace_from_name_and_sku()
    {
        var items = new List<BulkCreateItem> { new("  Gadget  ", "  GAD-001  ", 5) };
        var service = BuildService();

        var result = await service.BulkCreateAsync(items, CancellationToken.None);

        result.ResultOutcome.Should().Be(BulkCreateResult.Outcome.Created);
        result.CreatedWidgets[0].Name.Should().Be("Gadget");
        result.CreatedWidgets[0].Sku.Should().Be("GAD-001");
    }

    // AC-2 / AC-5: batch exceeding 50 items is rejected before any validation or persistence
    [Fact]
    public async Task BulkCreateAsync_with_51_items_returns_BatchSizeExceeded_outcome()
    {
        var items = Enumerable.Range(1, 51)
            .Select(i => new BulkCreateItem($"Widget {i}", $"SKU-{i}", i))
            .ToList();

        var repo = new Mock<IWidgetRepository>();
        var service = BuildService(repo);

        var result = await service.BulkCreateAsync(items, CancellationToken.None);

        result.ResultOutcome.Should().Be(BulkCreateResult.Outcome.BatchSizeExceeded);
        result.ReceivedCount.Should().Be(51);
        result.MaxAllowed.Should().Be(50);
        // No persistence must have occurred
        repo.Verify(r => r.AddAsync(It.IsAny<Widget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(51)]
    [InlineData(100)]
    public async Task BulkCreateAsync_with_batch_exceeding_max_returns_BatchSizeExceeded_outcome(int count)
    {
        var items = Enumerable.Range(1, count)
            .Select(i => new BulkCreateItem($"Widget {i}", $"SKU-{i}", i))
            .ToList();

        var repo = new Mock<IWidgetRepository>();
        var service = BuildService(repo);

        var result = await service.BulkCreateAsync(items, CancellationToken.None);

        result.ResultOutcome.Should().Be(BulkCreateResult.Outcome.BatchSizeExceeded);
        repo.Verify(r => r.AddAsync(It.IsAny<Widget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC-3 / AC-5: any item failing validation causes BatchValidationFailure and zero persistence
    [Fact]
    public async Task BulkCreateAsync_with_one_invalid_item_returns_ValidationFailure_with_correct_index()
    {
        // Item at index 1 has blank name
        var items = new List<BulkCreateItem>
        {
            new("Valid Widget", "SKU-001", 5),
            new("", "SKU-002", 5),        // blank name — index 1
            new("Another Widget", "SKU-003", 5),
        };

        var repo = new Mock<IWidgetRepository>();
        var service = BuildService(repo);

        var result = await service.BulkCreateAsync(items, CancellationToken.None);

        result.ResultOutcome.Should().Be(BulkCreateResult.Outcome.ValidationFailure);
        result.Failures.Should().HaveCount(1);
        result.Failures[0].Index.Should().Be(1);
        result.Failures[0].Reason.Should().NotBeNullOrWhiteSpace();
        // No persistence must have occurred
        repo.Verify(r => r.AddAsync(It.IsAny<Widget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkCreateAsync_with_multiple_invalid_items_returns_all_failures()
    {
        var items = new List<BulkCreateItem>
        {
            new("Valid Widget", "SKU-001", 5),
            new("", "SKU-002", 5),         // blank name — index 1
            new("Widget", "", 5),           // blank sku — index 2
            new("Widget", "SKU-004", -1),   // negative quantity — index 3
        };

        var repo = new Mock<IWidgetRepository>();
        var service = BuildService(repo);

        var result = await service.BulkCreateAsync(items, CancellationToken.None);

        result.ResultOutcome.Should().Be(BulkCreateResult.Outcome.ValidationFailure);
        result.Failures.Should().HaveCount(3);
        result.Failures.Select(f => f.Index).Should().BeEquivalentTo(new[] { 1, 2, 3 });
        repo.Verify(r => r.AddAsync(It.IsAny<Widget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BulkCreateAsync_with_blank_name_returns_ValidationFailure_at_correct_index(string name)
    {
        var items = new List<BulkCreateItem> { new(name, "SKU-001", 5) };
        var repo = new Mock<IWidgetRepository>();
        var service = BuildService(repo);

        var result = await service.BulkCreateAsync(items, CancellationToken.None);

        result.ResultOutcome.Should().Be(BulkCreateResult.Outcome.ValidationFailure);
        result.Failures.Should().HaveCount(1);
        result.Failures[0].Index.Should().Be(0);
        repo.Verify(r => r.AddAsync(It.IsAny<Widget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BulkCreateAsync_with_blank_sku_returns_ValidationFailure_at_correct_index(string sku)
    {
        var items = new List<BulkCreateItem> { new("Widget", sku, 5) };
        var repo = new Mock<IWidgetRepository>();
        var service = BuildService(repo);

        var result = await service.BulkCreateAsync(items, CancellationToken.None);

        result.ResultOutcome.Should().Be(BulkCreateResult.Outcome.ValidationFailure);
        result.Failures.Should().HaveCount(1);
        result.Failures[0].Index.Should().Be(0);
        repo.Verify(r => r.AddAsync(It.IsAny<Widget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkCreateAsync_with_negative_quantity_returns_ValidationFailure()
    {
        var items = new List<BulkCreateItem> { new("Widget", "SKU-001", -1) };
        var repo = new Mock<IWidgetRepository>();
        var service = BuildService(repo);

        var result = await service.BulkCreateAsync(items, CancellationToken.None);

        result.ResultOutcome.Should().Be(BulkCreateResult.Outcome.ValidationFailure);
        result.Failures[0].Index.Should().Be(0);
        repo.Verify(r => r.AddAsync(It.IsAny<Widget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkCreateAsync_with_quantity_above_ceiling_returns_ValidationFailure()
    {
        var items = new List<BulkCreateItem> { new("Widget", "SKU-001", 10_001) };
        var repo = new Mock<IWidgetRepository>();
        var service = BuildService(repo);

        var result = await service.BulkCreateAsync(items, CancellationToken.None);

        result.ResultOutcome.Should().Be(BulkCreateResult.Outcome.ValidationFailure);
        result.Failures[0].Index.Should().Be(0);
        repo.Verify(r => r.AddAsync(It.IsAny<Widget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkCreateAsync_calls_AddAsync_for_each_valid_item()
    {
        var items = new List<BulkCreateItem>
        {
            new("Widget A", "SKU-A", 1),
            new("Widget B", "SKU-B", 2),
        };

        var repo = new Mock<IWidgetRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<Widget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Widget w, CancellationToken _) => w);

        var service = BuildService(repo);

        await service.BulkCreateAsync(items, CancellationToken.None);

        repo.Verify(r => r.AddAsync(It.IsAny<Widget>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
