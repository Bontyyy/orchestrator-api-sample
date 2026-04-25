using OrchestratorApiSample.Application.Exceptions;
using OrchestratorApiSample.Application.Interfaces;
using OrchestratorApiSample.Domain;

namespace OrchestratorApiSample.Application.Services;

public sealed class WidgetService
{
    private readonly IWidgetRepository _repository;

    public WidgetService(IWidgetRepository repository)
    {
        _repository = repository;
    }

    public async Task<Widget> CreateAsync(
        string name,
        string sku,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException(nameof(name), "must not be empty");
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ValidationException(nameof(sku), "must not be empty");
        }

        if (quantity < 0)
        {
            throw new ValidationException(nameof(quantity), "must be non-negative");
        }

        if (quantity > 10_000)
        {
            throw new ValidationException(nameof(quantity), "must be at most 10000");
        }

        var widget = new Widget(
            Id: Guid.NewGuid().ToString("N"),
            Name: name.Trim(),
            Sku: sku.Trim(),
            Quantity: quantity);

        return await _repository.AddAsync(widget, cancellationToken);
    }

    public Task<Widget?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ValidationException(nameof(id), "must not be empty");
        }

        return _repository.GetByIdAsync(id, cancellationToken);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ValidationException(nameof(id), "must not be empty");
        }

        return _repository.DeleteAsync(id, cancellationToken);
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken)
    {
        return _repository.CountAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Widget>> GetListAsync(int pageSize, CancellationToken cancellationToken)
    {
        if (pageSize > 500)
        {
            throw new ValidationException(nameof(pageSize), "must be at most 500");
        }

        return _repository.GetListAsync(pageSize, cancellationToken);
    }

    public const int BulkCreateMaxBatchSize = 50;

    /// <summary>
    /// Validates all items first (atomically) and, if all pass, persists them all.
    /// Returns a <see cref="BulkCreateResult"/> that distinguishes three outcomes:
    /// batch-size overflow, validation failure, and success.
    /// </summary>
    public async Task<BulkCreateResult> BulkCreateAsync(
        IReadOnlyList<BulkCreateItem> items,
        CancellationToken cancellationToken)
    {
        if (items.Count > BulkCreateMaxBatchSize)
        {
            return BulkCreateResult.BatchSizeExceeded(items.Count, BulkCreateMaxBatchSize);
        }

        // Pass 1: validate ALL items before persisting any.
        var failures = new List<BulkCreateFailure>();
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                failures.Add(new BulkCreateFailure(i, "name must not be empty"));
            }
            else if (string.IsNullOrWhiteSpace(item.Sku))
            {
                failures.Add(new BulkCreateFailure(i, "sku must not be empty"));
            }
            else if (item.Quantity < 0)
            {
                failures.Add(new BulkCreateFailure(i, "quantity must be non-negative"));
            }
            else if (item.Quantity > 10_000)
            {
                failures.Add(new BulkCreateFailure(i, "quantity must be at most 10000"));
            }
        }

        if (failures.Count > 0)
        {
            return BulkCreateResult.ValidationFailure(failures);
        }

        // Pass 2: all items are valid — persist them all.
        var created = new List<Widget>(items.Count);
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var widget = new Widget(
                Id: Guid.NewGuid().ToString("N"),
                Name: item.Name.Trim(),
                Sku: item.Sku.Trim(),
                Quantity: item.Quantity);
            var stored = await _repository.AddAsync(widget, cancellationToken);
            created.Add(stored);
        }

        return BulkCreateResult.Success(created);
    }

    public Task<Widget?> UpdateAsync(
        string id,
        string? name,
        string? sku,
        int? quantity,
        CancellationToken cancellationToken)
    {
        if (name is not null && string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException(nameof(name), "must not be empty");
        }

        if (sku is not null && string.IsNullOrWhiteSpace(sku))
        {
            throw new ValidationException(nameof(sku), "must not be empty");
        }

        if (quantity is not null && quantity < 0)
        {
            throw new ValidationException(nameof(quantity), "must be non-negative");
        }

        if (quantity is not null && quantity > 10_000)
        {
            throw new ValidationException(nameof(quantity), "must be at most 10000");
        }

        var resolvedName = name?.Trim();
        var resolvedSku = sku?.Trim();

        return _repository.UpdateAsync(id, resolvedName, resolvedSku, quantity, cancellationToken);
    }
}
