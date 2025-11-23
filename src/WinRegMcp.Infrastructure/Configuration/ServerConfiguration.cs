using WinRegMcp.Domain.Models;

namespace WinRegMcp.Infrastructure.Configuration;

/// <summary>
/// Main server configuration.
/// </summary>
public sealed class ServerConfiguration
{
    public string ServerName { get; init; } = "winreg-mcp-server";
    public string Version { get; init; } = "1.0.0";
    public string LogLevel { get; init; } = "Information";
    public int WorkerThreads { get; init; } = 4;
    public AccessLevel AuthorizationLevel { get; init; } = AccessLevel.ReadOnly;
    public string? AllowedPathsFile { get; init; }

    public static ServerConfiguration LoadFromEnvironment()
    {
        return new ServerConfiguration
        {
            ServerName = Environment.GetEnvironmentVariable("WINREG_MCP_SERVER_NAME") ?? "winreg-mcp-server",
            LogLevel = Environment.GetEnvironmentVariable("WINREG_MCP_LOG_LEVEL") ?? "Information",
            WorkerThreads = int.TryParse(
                Environment.GetEnvironmentVariable("WINREG_MCP_WORKER_THREADS"),
                out var threads) ? threads : 4,
            AuthorizationLevel = ParseAccessLevel(
                Environment.GetEnvironmentVariable("WINREG_MCP_AUTHORIZATION_LEVEL")),
            AllowedPathsFile = Environment.GetEnvironmentVariable("WINREG_MCP_ALLOWED_PATHS_FILE")
        };
    }

    private static AccessLevel ParseAccessLevel(string? level)
    {
        return (level?.ToUpperInvariant()) switch
        {
            "READ_ONLY" => AccessLevel.ReadOnly,
            "READ_WRITE" => AccessLevel.ReadWrite,
            "ADMIN" => AccessLevel.Admin,
            _ => AccessLevel.ReadOnly
        };
    }
}

