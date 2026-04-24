using OrchestratorApiSample.Domain;

namespace OrchestratorApiSample.Application.Interfaces;

public interface IWidgetRepository
{
    Task<Widget> AddAsync(Widget widget, CancellationToken cancellationToken);

    Task<Widget?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task DeleteAsync(string id, CancellationToken cancellationToken);

    Task<int> CountAsync(CancellationToken cancellationToken);
}
