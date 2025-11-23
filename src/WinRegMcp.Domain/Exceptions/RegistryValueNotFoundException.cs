namespace WinRegMcp.Domain.Exceptions;

/// <summary>
/// Exception thrown when a registry value is not found.
/// </summary>
public sealed class RegistryValueNotFoundException : RegistryDomainException
{
    public string ValueName { get; }

    public RegistryValueNotFoundException(string path, string valueName)
        : base("VALUE_NOT_FOUND", $"Registry value '{valueName}' not found in key: {path}", path)
    {
        ValueName = valueName;
    }
}

