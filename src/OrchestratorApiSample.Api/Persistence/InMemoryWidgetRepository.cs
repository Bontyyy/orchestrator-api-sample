using System.Collections.Concurrent;
using OrchestratorApiSample.Application.Interfaces;
using OrchestratorApiSample.Domain;

namespace OrchestratorApiSample.Api.Persistence;

public sealed class InMemoryWidgetRepository : IWidgetRepository
{
    private readonly ConcurrentDictionary<string, Widget> _store = new();

    public Task<Widget> AddAsync(Widget widget, CancellationToken cancellationToken)
    {
        _store[widget.Id] = widget;
        return Task.FromResult(widget);
    }

    public Task<Widget?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id, out var widget);
        return Task.FromResult(widget);
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_store.Count);
    }
}
