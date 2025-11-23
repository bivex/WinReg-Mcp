namespace WinRegMcp.Application.DTOs;

/// <summary>
/// Response containing registry value data.
/// </summary>
public sealed class RegistryValueResponse
{
    public string Name { get; init; } = string.Empty;
    public string Data { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public int SizeBytes { get; init; }
}

