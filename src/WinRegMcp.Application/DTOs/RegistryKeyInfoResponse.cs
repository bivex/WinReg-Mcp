namespace WinRegMcp.Application.DTOs;

/// <summary>
/// Response containing registry key information.
/// </summary>
public sealed class RegistryKeyInfoResponse
{
    public string Path { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int SubKeyCount { get; init; }
    public int ValueCount { get; init; }
    public List<string> SubKeyNames { get; init; } = new();
    public bool Exists { get; init; } = true;
    public string? ErrorMessage { get; init; }
}

