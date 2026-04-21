namespace OrchestratorApiSample.Application.Exceptions;

public sealed class ValidationException : Exception
{
    public ValidationException(string field, string reason)
        : base($"Validation failed for field '{field}': {reason}")
    {
        Field = field;
        Reason = reason;
    }

    public string Field { get; }

    public string Reason { get; }
}
