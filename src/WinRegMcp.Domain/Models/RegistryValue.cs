namespace WinRegMcp.Domain.Models;

/// <summary>
/// Represents a Windows Registry value.
/// </summary>
public sealed class RegistryValue
{
    public string Name { get; init; }
    public object? Data { get; init; }
    public RegistryValueType Type { get; init; }
    public RegistryPath KeyPath { get; init; }
    public int DataSizeBytes { get; init; }

    public RegistryValue(
        string name,
        object? data,
        RegistryValueType type,
        RegistryPath keyPath,
        int dataSizeBytes = 0)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Data = data;
        Type = type;
        KeyPath = keyPath ?? throw new ArgumentNullException(nameof(keyPath));
        DataSizeBytes = dataSizeBytes;
    }

    /// <summary>
    /// Gets the data as a string representation.
    /// </summary>
    public string GetDataAsString()
    {
        if (Data == null)
            return string.Empty;

        return Type switch
        {
            RegistryValueType.String or RegistryValueType.ExpandString => Data.ToString() ?? string.Empty,
            RegistryValueType.DWord or RegistryValueType.QWord => Data.ToString() ?? "0",
            RegistryValueType.MultiString => string.Join("\n", (string[])Data),
            RegistryValueType.Binary => Convert.ToBase64String((byte[])Data),
            _ => Data.ToString() ?? string.Empty
        };
    }
}

