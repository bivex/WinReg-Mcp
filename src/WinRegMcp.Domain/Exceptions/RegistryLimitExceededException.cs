namespace WinRegMcp.Domain.Exceptions;

/// <summary>
/// Exception thrown when an operation exceeds configured limits.
/// </summary>
public sealed class RegistryLimitExceededException : RegistryDomainException
{
    public string LimitType { get; }
    public int RequestedValue { get; }
    public int MaxAllowedValue { get; }

    public RegistryLimitExceededException(
        string limitType,
        int requestedValue,
        int maxAllowedValue,
        string? path = null)
        : base(
            "LIMIT_EXCEEDED",
            $"{limitType} limit exceeded. Requested: {requestedValue}, Maximum allowed: {maxAllowedValue}",
            path)
    {
        LimitType = limitType;
        RequestedValue = requestedValue;
        MaxAllowedValue = maxAllowedValue;
    }
}

