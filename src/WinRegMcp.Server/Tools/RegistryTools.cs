using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using WinRegMcp.Application.DTOs;
using WinRegMcp.Application.Handlers;
using WinRegMcp.Domain.Exceptions;
using WinRegMcp.Domain.Models;
using WinRegMcp.Infrastructure.Configuration;
using WinRegMcp.Infrastructure.Observability;

namespace WinRegMcp.Server.Tools;

/// <summary>
/// MCP tools for registry operations.
/// </summary>
[McpServerToolType]
public sealed class RegistryTools
{
    private readonly ILogger<RegistryTools> _logger;
    private readonly RegistryToolHandlers _handlers;
    private readonly AccessLevel _accessLevel;
    
    // Static service provider for dependency resolution (used by MCP SDK reflection)
    private static IServiceProvider? _serviceProvider;
    
    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        Console.Error.WriteLine("[REGISTRY_TOOLS] Service provider set for factory pattern");
    }

    // Parameterless constructor for MCP SDK reflection
    public RegistryTools()
    {
        Console.Error.WriteLine("[REGISTRY_TOOLS] Parameterless constructor called by MCP SDK");
        
        if (_serviceProvider == null)
        {
            Console.Error.WriteLine("[REGISTRY_TOOLS] ERROR: Service provider not set!");
            throw new InvalidOperationException("Service provider must be set before creating RegistryTools");
        }
        
        Console.Error.WriteLine("[REGISTRY_TOOLS] Resolving dependencies from service provider...");
        _handlers = _serviceProvider.GetRequiredService<RegistryToolHandlers>();
        var serverConfig = _serviceProvider.GetRequiredService<ServerConfiguration>();
        _accessLevel = serverConfig.AuthorizationLevel;
        _logger = _serviceProvider.GetRequiredService<ILogger<RegistryTools>>();
        
        Console.Error.WriteLine($"[REGISTRY_TOOLS] RegistryTools instance created successfully with AccessLevel={_accessLevel}");
        _logger.LogCritical("RegistryTools instance created with AccessLevel={AccessLevel}", _accessLevel);
    }

    // DI constructor (kept for testing and manual instantiation)
    public RegistryTools(
        RegistryToolHandlers handlers,
        AccessLevel accessLevel,
        ILogger<RegistryTools> logger)
    {
        Console.Error.WriteLine($"[REGISTRY_TOOLS] DI Constructor called: AccessLevel={accessLevel}");
        _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        _accessLevel = accessLevel;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Console.Error.WriteLine($"[REGISTRY_TOOLS] RegistryTools instance created successfully via DI");
        _logger.LogCritical("RegistryTools instance created with AccessLevel={AccessLevel}", accessLevel);
    }

    [McpServerTool(Name = "read_value")]
    [Description("Read a specific registry value from the Windows Registry")]
    public async Task<RegistryValueResponse> ReadValueAsync(
        [Description("Full registry path (e.g., 'HKEY_CURRENT_USER\\Software\\MyApp')")] string path,
        [Description("Name of the value to read")] string value_name,
        CancellationToken cancellationToken)
    {
        // Log immediately with highest priority - even before any processing
        try
        {
            Console.Error.WriteLine($"[REGISTRY_TOOLS] ====== read_value CALLED ======");
            Console.Error.WriteLine($"[REGISTRY_TOOLS] Path parameter: '{path ?? "NULL"}'");
            Console.Error.WriteLine($"[REGISTRY_TOOLS] ValueName parameter: '{value_name ?? "NULL"}'");
            Console.Error.WriteLine($"[REGISTRY_TOOLS] CancellationToken.IsCancellationRequested: {cancellationToken.IsCancellationRequested}");
        }
        catch (Exception logEx)
        {
            Console.Error.WriteLine($"[REGISTRY_TOOLS] ERROR logging parameters: {logEx.Message}");
        }
        
        _logger.LogCritical(
            "MCP Tool 'read_value' called: Path={Path}, ValueName={ValueName}",
            path, value_name);

        try
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Registry path cannot be null or empty", nameof(path));
            if (string.IsNullOrWhiteSpace(value_name))
                throw new ArgumentException("Value name cannot be null or empty", nameof(value_name));

            var context = CreateRequestContext(cancellationToken);
            Console.Error.WriteLine($"[REGISTRY_TOOLS] Created context: CorrelationId={context.CorrelationId}");
            _logger.LogCritical(
                "Created request context: CorrelationId={CorrelationId}, AccessLevel={AccessLevel}",
                context.CorrelationId, context.AccessLevel);

            var request = new ReadValueRequest { Path = path, ValueName = value_name };
            Console.Error.WriteLine($"[REGISTRY_TOOLS] Calling handler with Path={request.Path}, ValueName={request.ValueName}");
            
            var result = await _handlers.HandleReadValueAsync(request, context);

            Console.Error.WriteLine($"[REGISTRY_TOOLS] Handler returned: Name={result.Name}, Type={result.Type}");
            _logger.LogCritical(
                "MCP Tool 'read_value' completed successfully: Path={Path}, ValueName={ValueName}, Type={Type}",
                result.Path, result.Name, result.Type);

            return result;
        }
        catch (RegistryValueNotFoundException ex)
        {
            // Value not found - return a response indicating this instead of throwing
            Console.Error.WriteLine($"[REGISTRY_TOOLS] Value not found: {ex.ValueName} in {path}");
            _logger.LogInformation(
                "Registry value '{ValueName}' not found in {Path}",
                value_name, path);

            return new RegistryValueResponse
            {
                Name = value_name!,
                Data = null,
                Type = "NotFound",
                Path = path!,
                SizeBytes = 0,
                Exists = false,
                ErrorMessage = $"Registry value '{value_name}' not found in key: {path}"
            };
        }
        catch (RegistryKeyNotFoundException)
        {
            // Key not found - return a response indicating this
            Console.Error.WriteLine($"[REGISTRY_TOOLS] Registry key not found: {path}");
            _logger.LogInformation(
                "Registry key not found: {Path}",
                path);

            return new RegistryValueResponse
            {
                Name = value_name!,
                Data = null,
                Type = "KeyNotFound",
                Path = path!,
                SizeBytes = 0,
                Exists = false,
                ErrorMessage = $"Registry key not found: {path}"
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[REGISTRY_TOOLS] ERROR: {ex.GetType().Name}: {ex.Message}");
            Console.Error.WriteLine($"[REGISTRY_TOOLS] StackTrace: {ex.StackTrace}");
            _logger.LogCritical(
                ex,
                "MCP Tool 'read_value' failed: Path={Path}, ValueName={ValueName}, Error={Message}, Type={ExceptionType}",
                path, value_name, ex.Message, ex.GetType().Name);
            throw;
        }
    }

    [McpServerTool(Name = "write_value")]
    [Description("Write or update a registry value in the Windows Registry")]
    public async Task<string> WriteValueAsync(
        [Description("Full registry path (e.g., 'HKEY_CURRENT_USER\\Software\\MyApp')")] string path,
        [Description("Name of the value to write")] string value_name,
        [Description("Value data to write")] string value_data,
        [Description("Registry value type (String, DWord, QWord, Binary, MultiString, ExpandString)")] string value_type,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Registry path cannot be null or empty", nameof(path));
        if (string.IsNullOrWhiteSpace(value_name))
            throw new ArgumentException("Value name cannot be null or empty", nameof(value_name));
        if (value_data == null)
            throw new ArgumentNullException(nameof(value_data));
        if (string.IsNullOrWhiteSpace(value_type))
            throw new ArgumentException("Value type cannot be null or empty", nameof(value_type));

        var context = CreateRequestContext(cancellationToken);
        var request = new WriteValueRequest
        {
            Path = path,
            ValueName = value_name,
            ValueData = value_data,
            ValueType = value_type
        };
        return await _handlers.HandleWriteValueAsync(request, context);
    }

    [McpServerTool(Name = "delete_value")]
    [Description("Delete a registry value from the Windows Registry")]
    public async Task<string> DeleteValueAsync(
        [Description("Full registry path (e.g., 'HKEY_CURRENT_USER\\Software\\MyApp')")] string path,
        [Description("Name of the value to delete")] string value_name,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Registry path cannot be null or empty", nameof(path));
        if (string.IsNullOrWhiteSpace(value_name))
            throw new ArgumentException("Value name cannot be null or empty", nameof(value_name));

        var context = CreateRequestContext(cancellationToken);
        return await _handlers.HandleDeleteValueAsync(path, value_name, context);
    }

    [McpServerTool(Name = "enumerate_keys")]
    [Description("List subkeys under a registry path")]
    public async Task<List<string>> EnumerateKeysAsync(
        [Description("Full registry path to enumerate (e.g., 'HKEY_CURRENT_USER\\Software')")] string path,
        [Description("Maximum enumeration depth (default: 1, max: 3)")] int max_depth,
        CancellationToken cancellationToken)
    {
        // Log immediately with highest priority - even before any processing
        try
        {
            Console.Error.WriteLine($"[REGISTRY_TOOLS] ====== enumerate_keys CALLED ======");
            Console.Error.WriteLine($"[REGISTRY_TOOLS] Path parameter: '{path ?? "NULL"}'");
            Console.Error.WriteLine($"[REGISTRY_TOOLS] MaxDepth parameter: {max_depth}");
            Console.Error.WriteLine($"[REGISTRY_TOOLS] CancellationToken.IsCancellationRequested: {cancellationToken.IsCancellationRequested}");
        }
        catch (Exception logEx)
        {
            Console.Error.WriteLine($"[REGISTRY_TOOLS] ERROR logging parameters: {logEx.Message}");
        }
        
        _logger.LogCritical(
            "MCP Tool 'enumerate_keys' called: Path={Path}, MaxDepth={MaxDepth}",
            path, max_depth);

        try
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Registry path cannot be null or empty", nameof(path));

            var context = CreateRequestContext(cancellationToken);
            Console.Error.WriteLine($"[REGISTRY_TOOLS] Created context: CorrelationId={context.CorrelationId}");
            _logger.LogCritical(
                "Created request context: CorrelationId={CorrelationId}, AccessLevel={AccessLevel}",
                context.CorrelationId, context.AccessLevel);

            var request = new EnumerateKeysRequest { Path = path, MaxDepth = max_depth };
            Console.Error.WriteLine($"[REGISTRY_TOOLS] Calling handler with Path={request.Path}, MaxDepth={request.MaxDepth}");
            
            var result = await _handlers.HandleEnumerateKeysAsync(request, context);

            Console.Error.WriteLine($"[REGISTRY_TOOLS] Handler returned {result.Count} keys");
            _logger.LogCritical(
                "MCP Tool 'enumerate_keys' completed successfully: Path={Path}, KeysCount={Count}",
                path, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[REGISTRY_TOOLS] ERROR: {ex.GetType().Name}: {ex.Message}");
            Console.Error.WriteLine($"[REGISTRY_TOOLS] StackTrace: {ex.StackTrace}");
            _logger.LogCritical(
                ex,
                "MCP Tool 'enumerate_keys' failed: Path={Path}, MaxDepth={MaxDepth}, Error={Message}, Type={ExceptionType}",
                path, max_depth, ex.Message, ex.GetType().Name);
            throw;
        }
    }

    [McpServerTool(Name = "enumerate_values")]
    [Description("List all values in a registry key")]
    public async Task<List<RegistryValueResponse>> EnumerateValuesAsync(
        [Description("Full registry path (e.g., 'HKEY_CURRENT_USER\\Software\\MyApp')")] string path,
        CancellationToken cancellationToken)
    {
        _logger.LogCritical(
            "MCP Tool 'enumerate_values' called: Path={Path}",
            path);

        try
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Registry path cannot be null or empty", nameof(path));

            var context = CreateRequestContext(cancellationToken);
            Console.Error.WriteLine($"[REGISTRY_TOOLS] Created context: CorrelationId={context.CorrelationId}");
            
            var result = await _handlers.HandleEnumerateValuesAsync(path, context);

            Console.Error.WriteLine($"[REGISTRY_TOOLS] Handler returned {result.Count} values");
            _logger.LogCritical(
                "MCP Tool 'enumerate_values' completed successfully: Path={Path}, ValuesCount={Count}",
                path, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[REGISTRY_TOOLS] Exception in EnumerateValuesAsync: {ex.GetType().Name}: {ex.Message}");
            _logger.LogError(
                ex,
                "MCP Tool 'enumerate_values' failed: Path={Path}, Error={Error}",
                path, ex.Message);
            throw;
        }
    }

    [McpServerTool(Name = "get_key_info")]
    [Description("Get metadata about a registry key (subkey count, value count, etc.)")]
    public async Task<RegistryKeyInfoResponse> GetKeyInfoAsync(
        [Description("Full registry path (e.g., 'HKEY_CURRENT_USER\\Software\\MyApp')")] string path,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Registry path cannot be null or empty", nameof(path));

        var context = CreateRequestContext(cancellationToken);
        return await _handlers.HandleGetKeyInfoAsync(path, context);
    }

    [McpServerTool(Name = "delete_key")]
    [Description("Delete a registry key and all its subkeys (requires ADMIN authorization)")]
    public async Task<string> DeleteKeyAsync(
        [Description("Full registry path to delete (e.g., 'HKEY_CURRENT_USER\\Software\\MyApp')")] string path,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Registry path cannot be null or empty", nameof(path));

        var context = CreateRequestContext(cancellationToken);
        return await _handlers.HandleDeleteKeyAsync(path, context);
    }

    [McpServerTool(Name = "search_clsid")]
    [Description("Search for COM CLSID objects with InprocServer32 DLLs (useful for finding loaded DLLs)")]
    public async Task<List<ClsidSearchResult>> SearchClsidAsync(
        [Description("Optional: Filter by DLL name (e.g., 'shell32.dll'). Leave empty to get all.")] string dll_filter,
        [Description("Maximum number of results to return (default: 50, max: 200)")] int max_results,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dll_filter))
            dll_filter = string.Empty;
        
        var limit = Math.Min(max_results > 0 ? max_results : 50, 200);
        var context = CreateRequestContext(cancellationToken);
        
        _logger.LogInformation(
            "MCP Tool 'search_clsid' called: DllFilter={DllFilter}, MaxResults={MaxResults}",
            string.IsNullOrEmpty(dll_filter) ? "none" : dll_filter, limit);

        try
        {
            var results = await _handlers.HandleSearchClsidAsync(
                string.IsNullOrEmpty(dll_filter) ? null : dll_filter, 
                limit, 
                context);

            _logger.LogInformation(
                "MCP Tool 'search_clsid' completed: Found {Count} results",
                results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "MCP Tool 'search_clsid' failed: {Error}",
                ex.Message);
            throw;
        }
    }

    private RequestContext CreateRequestContext(CancellationToken cancellationToken)
    {
        return new RequestContext(
            CorrelationIdGenerator.Generate(),
            _accessLevel,
            cancellationToken);
    }
}


