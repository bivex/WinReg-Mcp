namespace WinRegMcp.Domain.Exceptions;

/// <summary>
/// Exception thrown when a registry value type is invalid or mismatched.
/// </summary>
public sealed class RegistryInvalidValueTypeException : RegistryDomainException
{
    public string ValueName { get; }
    public string ExpectedType { get; }
    public string ActualType { get; }

    public RegistryInvalidValueTypeException(
        string path,
        string valueName,
        string expectedType,
        string actualType)
        : base(
            "INVALID_VALUE_TYPE",
            $"Invalid value type for '{valueName}' in {path}. Expected: {expectedType}, Actual: {actualType}",
            path)
    {
        ValueName = valueName;
        ExpectedType = expectedType;
        ActualType = actualType;
    }
}

