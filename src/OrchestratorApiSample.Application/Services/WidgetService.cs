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
