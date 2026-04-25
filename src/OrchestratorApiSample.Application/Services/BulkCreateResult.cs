using OrchestratorApiSample.Domain;

namespace OrchestratorApiSample.Application.Services;

/// <summary>
/// Discriminated union result returned by <see cref="WidgetService.BulkCreateAsync"/>.
/// </summary>
public sealed class BulkCreateResult
{
    private BulkCreateResult() { }

    public enum Outcome
    {
        Created,
        BatchSizeExceeded,
        ValidationFailure,
    }

    public Outcome ResultOutcome { get; private init; }

    /// <summary>Set when <see cref="ResultOutcome"/> is <see cref="Outcome.Created"/>.</summary>
    public IReadOnlyList<Widget> CreatedWidgets { get; private init; } = [];

    /// <summary>Set when <see cref="ResultOutcome"/> is <see cref="Outcome.ValidationFailure"/>.</summary>
    public IReadOnlyList<BulkCreateFailure> Failures { get; private init; } = [];

    /// <summary>Set when <see cref="ResultOutcome"/> is <see cref="Outcome.BatchSizeExceeded"/>.</summary>
    public int ReceivedCount { get; private init; }

    /// <summary>Set when <see cref="ResultOutcome"/> is <see cref="Outcome.BatchSizeExceeded"/>.</summary>
    public int MaxAllowed { get; private init; }

    public static BulkCreateResult Success(IReadOnlyList<Widget> widgets) =>
        new() { ResultOutcome = Outcome.Created, CreatedWidgets = widgets };

    public static BulkCreateResult BatchSizeExceeded(int received, int max) =>
        new() { ResultOutcome = Outcome.BatchSizeExceeded, ReceivedCount = received, MaxAllowed = max };

    public static BulkCreateResult ValidationFailure(IReadOnlyList<BulkCreateFailure> failures) =>
        new() { ResultOutcome = Outcome.ValidationFailure, Failures = failures };
}
