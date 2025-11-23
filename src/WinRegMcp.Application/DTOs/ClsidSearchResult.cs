namespace WinRegMcp.Application.DTOs;

/// <summary>
/// Result of a CLSID search operation.
/// </summary>
public class ClsidSearchResult
{
    public string Clsid { get; set; } = string.Empty;
    public string DllPath { get; set; } = string.Empty;
    public string RegistryPath { get; set; } = string.Empty;
}

