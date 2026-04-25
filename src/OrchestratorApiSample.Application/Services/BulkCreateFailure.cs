namespace OrchestratorApiSample.Application.Services;

/// <summary>
/// Describes a single validation failure within a bulk creation request.
/// </summary>
/// <param name="Index">Zero-based position of the failing item in the input array.</param>
/// <param name="Reason">Human-readable description of why validation failed.</param>
public sealed record BulkCreateFailure(int Index, string Reason);
