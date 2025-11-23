using System.Text.Json.Serialization;

namespace WinRegMcp.Infrastructure.Configuration;

/// <summary>
/// Configuration for allowed and denied registry paths.
/// </summary>
public sealed class AllowedPathsConfiguration
{
    [JsonPropertyName("allowedRoots")]
    public List<PathAccessRule> AllowedRoots { get; init; } = new();
    
    [JsonPropertyName("deniedPaths")]
    public List<string> DeniedPaths { get; init; } = new();

    /// <summary>
    /// Gets the default configuration with safe defaults.
    /// </summary>
    public static AllowedPathsConfiguration GetDefault()
    {
        return new AllowedPathsConfiguration
        {
            AllowedRoots = new List<PathAccessRule>
            {
                new()
                {
                    Path = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion",
                    Access = "read",
                    MaxDepth = 2
                },
                new()
                {
                    Path = @"HKEY_CURRENT_USER\Software",
                    Access = "read",
                    MaxDepth = 3
                }
            },
            DeniedPaths = new List<string>
            {
                @"HKEY_LOCAL_MACHINE\SECURITY",
                @"HKEY_LOCAL_MACHINE\SAM",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa"
            }
        };
    }
}

