namespace WinRegMcp.Domain.Exceptions;

/// <summary>
/// Base class for all registry domain exceptions.
/// </summary>
public abstract class RegistryDomainException : Exception
{
    public string ErrorCode { get; }
    public string? RegistryPath { get; }

    protected RegistryDomainException(
        string errorCode, 
        string message, 
        string? registryPath = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        RegistryPath = registryPath;
    }
}

