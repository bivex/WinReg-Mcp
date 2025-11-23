namespace WinRegMcp.Application.DTOs;

/// <summary>
/// Response containing registry value data.
/// </summary>
public sealed class RegistryValueResponse
{
    public string Name { get; init; } = string.Empty;
    public string? Data { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public int SizeBytes { get; init; }
    public bool Exists { get; init; } = true;
    public string? ErrorMessage { get; init; }
}

