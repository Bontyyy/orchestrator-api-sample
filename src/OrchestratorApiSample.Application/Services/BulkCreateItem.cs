namespace OrchestratorApiSample.Application.Services;

/// <summary>
/// Represents a single item in a bulk widget creation request.
/// </summary>
public sealed record BulkCreateItem(string Name, string Sku, int Quantity);
