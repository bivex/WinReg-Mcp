namespace WinRegMcp.Domain.Models;

/// <summary>
/// Represents a Windows Registry key with its metadata.
/// </summary>
public sealed class RegistryKey
{
    public RegistryPath Path { get; init; }
    public string Name { get; init; }
    public int SubKeyCount { get; init; }
    public int ValueCount { get; init; }
    public DateTime? LastWriteTime { get; init; }
    public IReadOnlyList<string> SubKeyNames { get; init; }

    public RegistryKey(
        RegistryPath path, 
        string name,
        int subKeyCount = 0,
        int valueCount = 0,
        DateTime? lastWriteTime = null,
        IReadOnlyList<string>? subKeyNames = null)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        SubKeyCount = subKeyCount;
        ValueCount = valueCount;
        LastWriteTime = lastWriteTime;
        SubKeyNames = subKeyNames ?? Array.Empty<string>();
    }
}

