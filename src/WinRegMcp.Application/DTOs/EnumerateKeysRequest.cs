using System.ComponentModel;

namespace WinRegMcp.Application.DTOs;

/// <summary>
/// Request to enumerate registry subkeys.
/// </summary>
public sealed class EnumerateKeysRequest
{
    [Description("Full registry path to enumerate (e.g., 'HKEY_CURRENT_USER\\Software')")]
    public string Path { get; init; } = string.Empty;

    [Description("Maximum enumeration depth (default: 1, max: 3)")]
    public int MaxDepth { get; init; } = 1;
}

