using System.Text.Json.Serialization;
using WinRegMcp.Domain.Models;

namespace WinRegMcp.Infrastructure.Configuration;

/// <summary>
/// Represents an access rule for a registry path.
/// </summary>
public sealed class PathAccessRule
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;
    
    [JsonPropertyName("access")]
    public string Access { get; init; } = "read";
    
    [JsonPropertyName("maxDepth")]
    public int MaxDepth { get; init; } = 2;

    public AccessLevel GetAccessLevel()
    {
        return Access.ToLowerInvariant() switch
        {
            "read" => AccessLevel.ReadOnly,
            "read_write" => AccessLevel.ReadWrite,
            "admin" => AccessLevel.Admin,
            _ => AccessLevel.ReadOnly
        };
    }
}

