using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using WinRegMcp.Application.Authorization;
using WinRegMcp.Application.Handlers;
using WinRegMcp.Domain.Services;
using WinRegMcp.Infrastructure.Configuration;
using WinRegMcp.Infrastructure.Observability;
using WinRegMcp.Infrastructure.Registry;
using WinRegMcp.Server.Tools;

namespace WinRegMcp.Server;

public class Program
{
    [SupportedOSPlatform("windows")]
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Configure logging to stderr (MCP requirement)
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });
        // Set log level from environment or default to Information
        var logLevel = Environment.GetEnvironmentVariable("WINREG_MCP_LOG_LEVEL");
        if (!string.IsNullOrEmpty(logLevel) && Enum.TryParse<LogLevel>(logLevel, true, out var parsedLevel))
        {
            builder.Logging.SetMinimumLevel(parsedLevel);
        }
        else
        {
            builder.Logging.SetMinimumLevel(LogLevel.Information);
        }
        
        // Enable Debug level for detailed troubleshooting
        builder.Logging.AddFilter("WinRegMcp", LogLevel.Debug);

        // Load configuration
        var serverConfig = ServerConfiguration.LoadFromEnvironment();

        // Register infrastructure services
        builder.Services.AddSingleton(serverConfig);
        builder.Services.AddSingleton<MetricsCollector>();
        builder.Services.AddSingleton<ConfigurationProvider>();

        // Load allowed paths configuration
        var configProvider = new ConfigurationProvider(
            builder.Services.BuildServiceProvider().GetRequiredService<ILogger<ConfigurationProvider>>());
        var allowedPaths = await configProvider.LoadAllowedPathsAsync(serverConfig.AllowedPathsFile);
        builder.Services.AddSingleton(allowedPaths);

        // Configure registry limits from environment
        var limits = new RegistryLimits
        {
            MaxEnumerationDepth = GetEnvInt("WINREG_MCP_MAX_ENUMERATION_DEPTH", 3),
            MaxValuesPerQuery = GetEnvInt("WINREG_MCP_MAX_VALUES_PER_QUERY", 100),
            MaxValueSizeBytes = GetEnvInt("WINREG_MCP_MAX_VALUE_SIZE_BYTES", 1024 * 1024),
            OperationTimeoutMs = GetEnvInt("WINREG_MCP_OPERATION_TIMEOUT_MS", 5000),
            RateLimitPerMinute = GetEnvInt("WINREG_MCP_RATE_LIMIT_PER_MINUTE", 100)
        };
        builder.Services.AddSingleton(limits);

        // Register domain services
        builder.Services.AddSingleton<IAuthorizationService>(sp =>
            new PathAuthorizationService(
                sp.GetRequiredService<ILogger<PathAuthorizationService>>(),
                sp.GetRequiredService<AllowedPathsConfiguration>(),
                limits.MaxEnumerationDepth));

        builder.Services.AddSingleton<IRegistryService, WindowsRegistryService>();

        // Register application handlers
        builder.Services.AddSingleton<RegistryToolHandlers>();

        // Register tools with access level
        Console.Error.WriteLine($"[PROGRAM] Registering RegistryTools with AccessLevel={serverConfig.AuthorizationLevel}");
        builder.Services.AddSingleton(sp => 
        {
            Console.Error.WriteLine("[PROGRAM] Creating RegistryTools instance...");
            var tools = new RegistryTools(
                sp.GetRequiredService<RegistryToolHandlers>(),
                serverConfig.AuthorizationLevel,
                sp.GetRequiredService<ILogger<RegistryTools>>());
            Console.Error.WriteLine("[PROGRAM] RegistryTools instance created");
            return tools;
        });

        // Configure MCP server
        Console.Error.WriteLine("[PROGRAM] Configuring MCP server...");
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();
        Console.Error.WriteLine("[PROGRAM] MCP server configuration completed");

        // Build service provider and set it for RegistryTools factory
        var tempServiceProvider = builder.Services.BuildServiceProvider();
        Console.Error.WriteLine("[PROGRAM] Setting service provider for RegistryTools...");
        RegistryTools.SetServiceProvider(tempServiceProvider);

        var logger = tempServiceProvider.GetRequiredService<ILogger<Program>>();

        Console.Error.WriteLine($"[PROGRAM] Starting Windows Registry MCP Server v{serverConfig.Version}");
        Console.Error.WriteLine($"[PROGRAM] Authorization Level: {serverConfig.AuthorizationLevel}");
        Console.Error.WriteLine($"[PROGRAM] Allowed paths: {allowedPaths.AllowedRoots.Count} roots, Denied paths: {allowedPaths.DeniedPaths.Count}");
        
        logger.LogInformation(
            "Starting Windows Registry MCP Server v{Version} (Authorization: {AuthLevel})",
            serverConfig.Version,
            serverConfig.AuthorizationLevel);

        logger.LogInformation(
            "Allowed paths: {AllowedCount} roots, Denied paths: {DeniedCount}",
            allowedPaths.AllowedRoots.Count,
            allowedPaths.DeniedPaths.Count);

        try
        {
            await builder.Build().RunAsync();
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Server terminated unexpectedly");
            throw;
        }
    }

    private static int GetEnvInt(string name, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }
}

