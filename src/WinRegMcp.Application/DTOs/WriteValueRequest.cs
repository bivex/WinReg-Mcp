using System.ComponentModel;

namespace WinRegMcp.Application.DTOs;

/// <summary>
/// Request to write a registry value.
/// </summary>
public sealed class WriteValueRequest
{
    [Description("Full registry path (e.g., 'HKEY_CURRENT_USER\\Software\\MyApp')")]
    public string Path { get; init; } = string.Empty;

    [Description("Name of the value to write")]
    public string ValueName { get; init; } = string.Empty;

    [Description("Value data to write")]
    public string ValueData { get; init; } = string.Empty;

    [Description("Registry value type (String, DWord, QWord, Binary, MultiString, ExpandString)")]
    public string ValueType { get; init; } = "String";
}

