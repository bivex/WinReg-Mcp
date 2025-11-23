namespace WinRegMcp.Domain.Exceptions;

/// <summary>
/// Exception thrown when a registry key is not found.
/// </summary>
public sealed class RegistryKeyNotFoundException : RegistryDomainException
{
    public RegistryKeyNotFoundException(string path)
        : base("KEY_NOT_FOUND", $"Registry key not found: {path}", path)
    {
    }

    public RegistryKeyNotFoundException(string path, Exception innerException)
        : base("KEY_NOT_FOUND", $"Registry key not found: {path}", path, innerException)
    {
    }
}

