using Microsoft.Extensions.Logging;
using WinRegMcp.Application.DTOs;
using WinRegMcp.Domain.Exceptions;
using WinRegMcp.Domain.Models;
using WinRegMcp.Domain.Services;
using WinRegMcp.Infrastructure.Observability;

namespace WinRegMcp.Application.Handlers;

/// <summary>
/// Handlers for registry MCP tools.
/// </summary>
public sealed class RegistryToolHandlers
{
    private readonly ILogger<RegistryToolHandlers> _logger;
    private readonly IRegistryService _registryService;
    private readonly MetricsCollector _metrics;

    public RegistryToolHandlers(
        ILogger<RegistryToolHandlers> logger,
        IRegistryService registryService,
        MetricsCollector metrics)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registryService = registryService ?? throw new ArgumentNullException(nameof(registryService));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    public async Task<RegistryValueResponse> HandleReadValueAsync(
        ReadValueRequest request,
        RequestContext context)
    {
        _logger.LogDebug(
            "[{CorrelationId}] HandleReadValueAsync called: Path={Path}, ValueName={ValueName}",
            context.CorrelationId, request.Path, request.ValueName);

        using var timer = _metrics.StartTimer(
            "registry_read_value_duration_seconds",
            new Dictionary<string, string> { ["operation"] = "read_value" });

        try
        {
            _logger.LogDebug(
                "[{CorrelationId}] Parsing registry path: {Path}",
                context.CorrelationId, request.Path);

            var path = RegistryPath.Parse(request.Path);

            _logger.LogDebug(
                "[{CorrelationId}] Path parsed successfully: {NormalizedPath}, Hive={Hive}",
                context.CorrelationId, path.GetNormalizedPath(), path.Hive);

            _logger.LogDebug(
                "[{CorrelationId}] Reading value '{ValueName}' from registry",
                context.CorrelationId, request.ValueName);

            var value = await _registryService.ReadValueAsync(path, request.ValueName, context);

            _logger.LogInformation(
                "[{CorrelationId}] Successfully read value '{ValueName}' from {Path}: Type={Type}, Size={Size} bytes",
                context.CorrelationId, value.Name, value.KeyPath.GetNormalizedPath(), value.Type, value.DataSizeBytes);

            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "read_value", ["status"] = "success" });

            var response = new RegistryValueResponse
            {
                Name = value.Name,
                Data = value.GetDataAsString(),
                Type = value.Type.ToString(),
                Path = value.KeyPath.GetNormalizedPath(),
                SizeBytes = value.DataSizeBytes
            };

            _logger.LogDebug(
                "[{CorrelationId}] Returning response: Name={Name}, Type={Type}, Path={Path}",
                context.CorrelationId, response.Name, response.Type, response.Path);

            return response;
        }
        catch (RegistryDomainException ex)
        {
            _logger.LogError(
                ex,
                "[{CorrelationId}] Failed to read value '{ValueName}' from {Path}: {ErrorCode} - {Message}",
                context.CorrelationId, request.ValueName, request.Path, ex.ErrorCode, ex.Message);

            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "read_value", ["status"] = "error" });
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[{CorrelationId}] Unexpected error reading value '{ValueName}' from {Path}: {Message}",
                context.CorrelationId, request.ValueName, request.Path, ex.Message);

            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "read_value", ["status"] = "error" });
            throw;
        }
    }

    public async Task<string> HandleWriteValueAsync(
        WriteValueRequest request,
        RequestContext context)
    {
        using var timer = _metrics.StartTimer(
            "registry_write_value_duration_seconds",
            new Dictionary<string, string> { ["operation"] = "write_value" });

        try
        {
            var path = RegistryPath.Parse(request.Path);
            var valueType = ParseValueType(request.ValueType);
            var data = ConvertValueData(request.ValueData, valueType);

            await _registryService.WriteValueAsync(
                path,
                request.ValueName,
                data,
                valueType,
                context);

            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "write_value", ["status"] = "success" });

            return $"Successfully wrote value '{request.ValueName}' to {path}";
        }
        catch (RegistryDomainException)
        {
            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "write_value", ["status"] = "error" });
            throw;
        }
    }

    public async Task<string> HandleDeleteValueAsync(
        string path,
        string valueName,
        RequestContext context)
    {
        using var timer = _metrics.StartTimer(
            "registry_delete_value_duration_seconds",
            new Dictionary<string, string> { ["operation"] = "delete_value" });

        try
        {
            var registryPath = RegistryPath.Parse(path);
            await _registryService.DeleteValueAsync(registryPath, valueName, context);

            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "delete_value", ["status"] = "success" });

            return $"✅ Successfully deleted '{valueName}' from autostart";
        }
        catch (RegistryValueNotFoundException)
        {
            // Value already doesn't exist - that's fine
            _logger.LogInformation(
                "[{CorrelationId}] Value '{ValueName}' not found in {Path} - already deleted or doesn't exist",
                context.CorrelationId, valueName, path);

            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "delete_value", ["status"] = "success" });

            return $"ℹ️  '{valueName}' was already removed or doesn't exist";
        }
        catch (RegistryKeyNotFoundException)
        {
            // Key doesn't exist - that's fine too
            _logger.LogInformation(
                "[{CorrelationId}] Registry key not found: {Path}",
                context.CorrelationId, path);

            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "delete_value", ["status"] = "success" });

            return $"ℹ️  Registry key not found: {path}";
        }
        catch (RegistryAccessDeniedException ex)
        {
            _logger.LogError(
                ex,
                "[{CorrelationId}] Access denied when deleting '{ValueName}' from {Path}",
                context.CorrelationId, valueName, path);

            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "delete_value", ["status"] = "error" });

            return $"❌ Access denied: {ex.Reason}";
        }
        catch (RegistryDomainException ex)
        {
            _logger.LogError(
                ex,
                "[{CorrelationId}] Failed to delete '{ValueName}' from {Path}: {ErrorCode} - {Message}",
                context.CorrelationId, valueName, path, ex.ErrorCode, ex.Message);

            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "delete_value", ["status"] = "error" });

            return $"❌ Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[{CorrelationId}] Unexpected error deleting '{ValueName}' from {Path}",
                context.CorrelationId, valueName, path);

            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "delete_value", ["status"] = "error" });

            return $"❌ Unexpected error: {ex.Message}";
        }
    }

    public async Task<List<string>> HandleEnumerateKeysAsync(
        EnumerateKeysRequest request,
        RequestContext context)
    {
        _logger.LogDebug(
            "[{CorrelationId}] HandleEnumerateKeysAsync called: Path={Path}, MaxDepth={MaxDepth}",
            context.CorrelationId, request.Path, request.MaxDepth);

        using var timer = _metrics.StartTimer(
            "registry_enumerate_keys_duration_seconds",
            new Dictionary<string, string> { ["operation"] = "enumerate_keys" });

        try
        {
            _logger.LogDebug(
                "[{CorrelationId}] Parsing registry path: {Path}",
                context.CorrelationId, request.Path);

            var path = RegistryPath.Parse(request.Path);

            _logger.LogDebug(
                "[{CorrelationId}] Path parsed successfully: {NormalizedPath}, Hive={Hive}",
                context.CorrelationId, path.GetNormalizedPath(), path.Hive);

            _logger.LogDebug(
                "[{CorrelationId}] Enumerating keys with max depth {MaxDepth}",
                context.CorrelationId, request.MaxDepth);

            var keys = await _registryService.EnumerateKeysAsync(
                path,
                request.MaxDepth,
                context);

            _logger.LogInformation(
                "[{CorrelationId}] Successfully enumerated {Count} keys from {Path}",
                context.CorrelationId, keys.Count(), path.GetNormalizedPath());

            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "enumerate_keys", ["status"] = "success" });

            var result = keys.ToList();

            _logger.LogDebug(
                "[{CorrelationId}] Returning {Count} keys",
                context.CorrelationId, result.Count);

            return result;
        }
        catch (RegistryDomainException ex)
        {
            _logger.LogError(
                ex,
                "[{CorrelationId}] Failed to enumerate keys from {Path}: {ErrorCode} - {Message}",
                context.CorrelationId, request.Path, ex.ErrorCode, ex.Message);

            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "enumerate_keys", ["status"] = "error" });
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[{CorrelationId}] Unexpected error enumerating keys from {Path}: {Message}",
                context.CorrelationId, request.Path, ex.Message);

            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "enumerate_keys", ["status"] = "error" });
            throw;
        }
    }

    public async Task<List<RegistryValueResponse>> HandleEnumerateValuesAsync(
        string path,
        RequestContext context)
    {
        using var timer = _metrics.StartTimer(
            "registry_enumerate_values_duration_seconds",
            new Dictionary<string, string> { ["operation"] = "enumerate_values" });

        try
        {
            var registryPath = RegistryPath.Parse(path);
            var values = await _registryService.EnumerateValuesAsync(registryPath, context);

            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "enumerate_values", ["status"] = "success" });

            return values.Select(v => new RegistryValueResponse
            {
                Name = v.Name,
                Data = v.GetDataAsString(),
                Type = v.Type.ToString(),
                Path = v.KeyPath.GetNormalizedPath(),
                SizeBytes = v.DataSizeBytes
            }).ToList();
        }
        catch (RegistryDomainException)
        {
            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "enumerate_values", ["status"] = "error" });
            throw;
        }
    }

    public async Task<RegistryKeyInfoResponse> HandleGetKeyInfoAsync(
        string path,
        RequestContext context)
    {
        using var timer = _metrics.StartTimer(
            "registry_get_key_info_duration_seconds",
            new Dictionary<string, string> { ["operation"] = "get_key_info" });

        try
        {
            var registryPath = RegistryPath.Parse(path);
            var keyInfo = await _registryService.GetKeyInfoAsync(registryPath, context);

            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "get_key_info", ["status"] = "success" });

            return new RegistryKeyInfoResponse
            {
                Path = keyInfo.Path.GetNormalizedPath(),
                Name = keyInfo.Name,
                SubKeyCount = keyInfo.SubKeyCount,
                ValueCount = keyInfo.ValueCount,
                SubKeyNames = keyInfo.SubKeyNames.ToList()
            };
        }
        catch (RegistryDomainException)
        {
            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "get_key_info", ["status"] = "error" });
            throw;
        }
    }

    public async Task<string> HandleDeleteKeyAsync(
        string path,
        RequestContext context)
    {
        using var timer = _metrics.StartTimer(
            "registry_delete_key_duration_seconds",
            new Dictionary<string, string> { ["operation"] = "delete_key" });

        try
        {
            var registryPath = RegistryPath.Parse(path);
            await _registryService.DeleteKeyAsync(registryPath, context);

            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "delete_key", ["status"] = "success" });

            return $"Successfully deleted key: {path}";
        }
        catch (RegistryDomainException)
        {
            _metrics.IncrementCounter(
                "registry_operations_total",
                new Dictionary<string, string> { ["operation"] = "delete_key", ["status"] = "error" });
            throw;
        }
    }

    private static RegistryValueType ParseValueType(string typeStr)
    {
        return typeStr.ToUpperInvariant() switch
        {
            "STRING" => RegistryValueType.String,
            "DWORD" => RegistryValueType.DWord,
            "QWORD" => RegistryValueType.QWord,
            "BINARY" => RegistryValueType.Binary,
            "MULTISTRING" => RegistryValueType.MultiString,
            "EXPANDSTRING" => RegistryValueType.ExpandString,
            _ => RegistryValueType.String
        };
    }

    private static object ConvertValueData(string dataStr, RegistryValueType type)
    {
        return type switch
        {
            RegistryValueType.String or RegistryValueType.ExpandString => dataStr,
            RegistryValueType.DWord => int.Parse(dataStr),
            RegistryValueType.QWord => long.Parse(dataStr),
            RegistryValueType.Binary => Convert.FromBase64String(dataStr),
            RegistryValueType.MultiString => dataStr.Split('\n'),
            _ => dataStr
        };
    }
}

