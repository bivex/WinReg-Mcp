using System.ComponentModel;

namespace WinRegMcp.Application.DTOs;

/// <summary>
/// Request to read a registry value.
/// </summary>
public sealed class ReadValueRequest
{
    [Description("Full registry path (e.g., 'HKEY_CURRENT_USER\\Software\\MyApp')")]
    public string Path { get; init; } = string.Empty;

    [Description("Name of the value to read")]
    public string ValueName { get; init; } = string.Empty;
}

