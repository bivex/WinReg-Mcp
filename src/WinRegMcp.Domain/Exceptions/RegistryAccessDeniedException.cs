namespace WinRegMcp.Domain.Exceptions;

/// <summary>
/// Exception thrown when access to a registry path is denied by authorization rules.
/// </summary>
public sealed class RegistryAccessDeniedException : RegistryDomainException
{
    public string Reason { get; }

    public RegistryAccessDeniedException(string path, string reason)
        : base("PATH_NOT_ALLOWED", $"Access to registry path is not permitted: {path}. Reason: {reason}", path)
    {
        Reason = reason;
    }
}

