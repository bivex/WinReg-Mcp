namespace WinRegMcp.Infrastructure.Registry;

/// <summary>
/// Configuration for registry operation limits.
/// </summary>
public sealed class RegistryLimits
{
    /// <summary>
    /// Maximum depth for key enumeration (default: 3).
    /// </summary>
    public int MaxEnumerationDepth { get; init; } = 3;

    /// <summary>
    /// Maximum number of values returned per query (default: 100).
    /// </summary>
    public int MaxValuesPerQuery { get; init; } = 100;

    /// <summary>
    /// Maximum size of a single registry value in bytes (default: 1MB).
    /// </summary>
    public int MaxValueSizeBytes { get; init; } = 1024 * 1024; // 1MB

    /// <summary>
    /// Operation timeout in milliseconds (default: 5000ms).
    /// </summary>
    public int OperationTimeoutMs { get; init; } = 5000;

    /// <summary>
    /// Maximum requests per minute (default: 100).
    /// </summary>
    public int RateLimitPerMinute { get; init; } = 100;
}

