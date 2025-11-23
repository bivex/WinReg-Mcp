namespace WinRegMcp.Domain.Models;

/// <summary>
/// Represents the data type of a registry value.
/// Maps to Win32 Registry value types.
/// </summary>
public enum RegistryValueType
{
    None = 0,
    String = 1,           // REG_SZ
    ExpandString = 2,     // REG_EXPAND_SZ
    Binary = 3,           // REG_BINARY
    DWord = 4,            // REG_DWORD
    MultiString = 7,      // REG_MULTI_SZ
    QWord = 11,           // REG_QWORD
    Unknown = -1
}

