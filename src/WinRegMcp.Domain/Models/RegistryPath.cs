namespace WinRegMcp.Domain.Models;

/// <summary>
/// Represents a validated and normalized Windows Registry path.
/// </summary>
public sealed class RegistryPath
{
    public string FullPath { get; }
    public RegistryHive Hive { get; }
    public string SubKeyPath { get; }

    private RegistryPath(string fullPath, RegistryHive hive, string subKeyPath)
    {
        FullPath = fullPath;
        Hive = hive;
        SubKeyPath = subKeyPath;
    }

    /// <summary>
    /// Creates a RegistryPath from a full path string.
    /// </summary>
    /// <param name="fullPath">Full registry path (e.g., "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft")</param>
    /// <returns>Validated RegistryPath</returns>
    /// <exception cref="ArgumentException">If path format is invalid</exception>
    public static RegistryPath Parse(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
            throw new ArgumentException("Registry path cannot be empty", nameof(fullPath));

        // Normalize path separators
        fullPath = fullPath.Replace('/', '\\').Trim();

        // Split hive and subkey
        var parts = fullPath.Split('\\', 2);
        if (parts.Length == 0)
            throw new ArgumentException($"Invalid registry path format: {fullPath}", nameof(fullPath));

        var hiveName = parts[0].ToUpperInvariant();
        var hive = ParseHive(hiveName);
        var subKeyPath = parts.Length > 1 ? parts[1] : string.Empty;

        // Validate subkey path doesn't contain invalid characters
        if (subKeyPath.Contains('\0'))
            throw new ArgumentException("Registry path cannot contain null characters", nameof(fullPath));

        return new RegistryPath(fullPath, hive, subKeyPath);
    }

    private static RegistryHive ParseHive(string hiveName)
    {
        return hiveName switch
        {
            "HKEY_LOCAL_MACHINE" or "HKLM" => RegistryHive.LocalMachine,
            "HKEY_CURRENT_USER" or "HKCU" => RegistryHive.CurrentUser,
            "HKEY_CLASSES_ROOT" or "HKCR" => RegistryHive.ClassesRoot,
            "HKEY_USERS" or "HKU" => RegistryHive.Users,
            "HKEY_CURRENT_CONFIG" or "HKCC" => RegistryHive.CurrentConfig,
            _ => throw new ArgumentException($"Unknown registry hive: {hiveName}")
        };
    }

    /// <summary>
    /// Gets the normalized full path with standard hive name.
    /// </summary>
    public string GetNormalizedPath()
    {
        var hiveName = Hive switch
        {
            RegistryHive.LocalMachine => "HKEY_LOCAL_MACHINE",
            RegistryHive.CurrentUser => "HKEY_CURRENT_USER",
            RegistryHive.ClassesRoot => "HKEY_CLASSES_ROOT",
            RegistryHive.Users => "HKEY_USERS",
            RegistryHive.CurrentConfig => "HKEY_CURRENT_CONFIG",
            _ => throw new InvalidOperationException($"Unknown hive: {Hive}")
        };

        return string.IsNullOrEmpty(SubKeyPath) 
            ? hiveName 
            : $"{hiveName}\\{SubKeyPath}";
    }

    /// <summary>
    /// Calculates the depth of this path (number of backslashes in subkey).
    /// </summary>
    public int GetDepth()
    {
        if (string.IsNullOrEmpty(SubKeyPath))
            return 0;
        
        return SubKeyPath.Split('\\', StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// Checks if this path is a parent of or equal to another path.
    /// </summary>
    public bool IsParentOfOrEqual(RegistryPath other)
    {
        if (Hive != other.Hive)
            return false;

        if (string.IsNullOrEmpty(SubKeyPath))
            return true; // Root hive is parent of everything in that hive

        var thisNormalized = GetNormalizedPath().TrimEnd('\\');
        var otherNormalized = other.GetNormalizedPath().TrimEnd('\\');

        return otherNormalized.Equals(thisNormalized, StringComparison.OrdinalIgnoreCase) ||
               otherNormalized.StartsWith(thisNormalized + "\\", StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString() => GetNormalizedPath();

    public override bool Equals(object? obj) =>
        obj is RegistryPath other && 
        GetNormalizedPath().Equals(other.GetNormalizedPath(), StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => 
        GetNormalizedPath().ToUpperInvariant().GetHashCode();
}

