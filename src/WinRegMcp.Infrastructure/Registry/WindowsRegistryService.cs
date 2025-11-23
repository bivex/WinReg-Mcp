using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WinRegMcp.Domain.Exceptions;
using WinRegMcp.Domain.Models;
using WinRegMcp.Domain.Services;
using System.Runtime.Versioning;

namespace WinRegMcp.Infrastructure.Registry;

/// <summary>
/// Windows Registry implementation using Win32 Registry API.
/// Thread-safe and supports cancellation.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsRegistryService : IRegistryService
{
    private readonly ILogger<WindowsRegistryService> _logger;
    private readonly IAuthorizationService _authorizationService;
    private readonly RegistryLimits _limits;

    public WindowsRegistryService(
        ILogger<WindowsRegistryService> logger,
        IAuthorizationService authorizationService,
        RegistryLimits limits)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _limits = limits ?? throw new ArgumentNullException(nameof(limits));
    }

    public async Task<RegistryValue> ReadValueAsync(
        RegistryPath path,
        string valueName,
        RequestContext context)
    {
        await _authorizationService.ValidateReadAccessAsync(path, context);

        _logger.LogInformation(
            "[{CorrelationId}] Reading registry value: {Path}\\{ValueName}",
            context.CorrelationId, path, valueName);

        return await Task.Run(() =>
        {
            using var key = OpenRegistryKey(path, writable: false);
            
            var data = key.GetValue(valueName);
            if (data == null)
            {
                throw new RegistryValueNotFoundException(path.GetNormalizedPath(), valueName);
            }

            var valueKind = key.GetValueKind(valueName);
            var valueType = MapValueKind(valueKind);

            // Calculate data size
            int dataSize = data switch
            {
                string s => System.Text.Encoding.Unicode.GetByteCount(s),
                byte[] b => b.Length,
                int => 4,
                long => 8,
                string[] arr => arr.Sum(s => System.Text.Encoding.Unicode.GetByteCount(s)),
                _ => 0
            };

            // Enforce value size limit
            if (dataSize > _limits.MaxValueSizeBytes)
            {
                throw new RegistryLimitExceededException(
                    "Value size",
                    dataSize,
                    _limits.MaxValueSizeBytes,
                    path.GetNormalizedPath());
            }

            return new RegistryValue(valueName, data, valueType, path, dataSize);
        }, context.CancellationToken);
    }

    public async Task WriteValueAsync(
        RegistryPath path,
        string valueName,
        object data,
        RegistryValueType valueType,
        RequestContext context)
    {
        await _authorizationService.ValidateWriteAccessAsync(path, context);

        _logger.LogInformation(
            "[{CorrelationId}] Writing registry value: {Path}\\{ValueName} (Type: {Type})",
            context.CorrelationId, path, valueName, valueType);

        await Task.Run(() =>
        {
            using var key = OpenRegistryKey(path, writable: true, createIfMissing: true);
            
            var valueKind = MapValueType(valueType);
            key.SetValue(valueName, data, valueKind);
        }, context.CancellationToken);
    }

    public async Task DeleteValueAsync(
        RegistryPath path,
        string valueName,
        RequestContext context)
    {
        await _authorizationService.ValidateDeleteAccessAsync(path, context);

        _logger.LogInformation(
            "[{CorrelationId}] Deleting registry value: {Path}\\{ValueName}",
            context.CorrelationId, path, valueName);

        await Task.Run(() =>
        {
            using var key = OpenRegistryKey(path, writable: true);
            
            try
            {
                key.DeleteValue(valueName, throwOnMissingValue: true);
            }
            catch (ArgumentException)
            {
                throw new RegistryValueNotFoundException(path.GetNormalizedPath(), valueName);
            }
        }, context.CancellationToken);
    }

    public async Task<IReadOnlyList<string>> EnumerateKeysAsync(
        RegistryPath path,
        int maxDepth,
        RequestContext context)
    {
        var allowedDepth = await _authorizationService.ValidateEnumerationAccessAsync(
            path, maxDepth, context);

        _logger.LogInformation(
            "[{CorrelationId}] Enumerating keys under: {Path} (Depth: {Depth})",
            context.CorrelationId, path, allowedDepth);

        return await Task.Run(() =>
        {
            var result = new List<string>();
            EnumerateKeysRecursive(path, allowedDepth, result, context.CancellationToken);
            
            if (result.Count > _limits.MaxValuesPerQuery)
            {
                throw new RegistryLimitExceededException(
                    "Key enumeration",
                    result.Count,
                    _limits.MaxValuesPerQuery,
                    path.GetNormalizedPath());
            }

            return (IReadOnlyList<string>)result;
        }, context.CancellationToken);
    }

    private void EnumerateKeysRecursive(
        RegistryPath path,
        int remainingDepth,
        List<string> result,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var key = OpenRegistryKey(path, writable: false);
        var subKeyNames = key.GetSubKeyNames();
        
        foreach (var subKeyName in subKeyNames)
        {
            result.Add(subKeyName);

            if (remainingDepth > 1)
            {
                var subKeyPath = RegistryPath.Parse($"{path.GetNormalizedPath()}\\{subKeyName}");
                EnumerateKeysRecursive(subKeyPath, remainingDepth - 1, result, cancellationToken);
            }
        }
    }

    public async Task<IReadOnlyList<RegistryValue>> EnumerateValuesAsync(
        RegistryPath path,
        RequestContext context)
    {
        await _authorizationService.ValidateReadAccessAsync(path, context);

        _logger.LogInformation(
            "[{CorrelationId}] Enumerating values in: {Path}",
            context.CorrelationId, path);

        return await Task.Run(() =>
        {
            using var key = OpenRegistryKey(path, writable: false);
            var valueNames = key.GetValueNames();

            if (valueNames.Length > _limits.MaxValuesPerQuery)
            {
                throw new RegistryLimitExceededException(
                    "Value enumeration",
                    valueNames.Length,
                    _limits.MaxValuesPerQuery,
                    path.GetNormalizedPath());
            }

            var result = new List<RegistryValue>();
            foreach (var valueName in valueNames)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var data = key.GetValue(valueName);
                var valueKind = key.GetValueKind(valueName);
                var valueType = MapValueKind(valueKind);

                result.Add(new RegistryValue(
                    valueName ?? "(Default)",
                    data,
                    valueType,
                    path));
            }

            return (IReadOnlyList<RegistryValue>)result;
        }, context.CancellationToken);
    }

    public async Task<Domain.Models.RegistryKey> GetKeyInfoAsync(
        RegistryPath path,
        RequestContext context)
    {
        await _authorizationService.ValidateReadAccessAsync(path, context);

        _logger.LogInformation(
            "[{CorrelationId}] Getting key info: {Path}",
            context.CorrelationId, path);

        return await Task.Run(() =>
        {
            using var key = OpenRegistryKey(path, writable: false);
            
            var subKeyNames = key.GetSubKeyNames();
            var valueNames = key.GetValueNames();
            var keyName = path.SubKeyPath.Split('\\').LastOrDefault() ?? path.Hive.ToString();

            return new Domain.Models.RegistryKey(
                path,
                keyName,
                subKeyNames.Length,
                valueNames.Length,
                null, // LastWriteTime not easily available via managed API
                subKeyNames);
        }, context.CancellationToken);
    }

    public async Task DeleteKeyAsync(
        RegistryPath path,
        RequestContext context)
    {
        await _authorizationService.ValidateDeleteAccessAsync(path, context);

        _logger.LogWarning(
            "[{CorrelationId}] Deleting registry key: {Path}",
            context.CorrelationId, path);

        await Task.Run(() =>
        {
            var baseKey = GetBaseKey(path.Hive);
            baseKey.DeleteSubKeyTree(path.SubKeyPath, throwOnMissingSubKey: true);
        }, context.CancellationToken);
    }

    public async Task<bool> KeyExistsAsync(
        RegistryPath path,
        RequestContext context)
    {
        await _authorizationService.ValidateReadAccessAsync(path, context);

        return await Task.Run(() =>
        {
            try
            {
                using var key = OpenRegistryKey(path, writable: false);
                return true;
            }
            catch (RegistryKeyNotFoundException)
            {
                return false;
            }
        }, context.CancellationToken);
    }

    private Microsoft.Win32.RegistryKey OpenRegistryKey(
        RegistryPath path,
        bool writable,
        bool createIfMissing = false)
    {
        var baseKey = GetBaseKey(path.Hive);

        if (string.IsNullOrEmpty(path.SubKeyPath))
        {
            return baseKey;
        }

        Microsoft.Win32.RegistryKey? key;
        if (createIfMissing && writable)
        {
            key = baseKey.CreateSubKey(path.SubKeyPath, writable);
        }
        else
        {
            key = baseKey.OpenSubKey(path.SubKeyPath, writable);
        }

        if (key == null)
        {
            throw new RegistryKeyNotFoundException(path.GetNormalizedPath());
        }

        return key;
    }

    private static Microsoft.Win32.RegistryKey GetBaseKey(Domain.Models.RegistryHive hive)
    {
        return hive switch
        {
            Domain.Models.RegistryHive.LocalMachine => Microsoft.Win32.Registry.LocalMachine,
            Domain.Models.RegistryHive.CurrentUser => Microsoft.Win32.Registry.CurrentUser,
            Domain.Models.RegistryHive.ClassesRoot => Microsoft.Win32.Registry.ClassesRoot,
            Domain.Models.RegistryHive.Users => Microsoft.Win32.Registry.Users,
            Domain.Models.RegistryHive.CurrentConfig => Microsoft.Win32.Registry.CurrentConfig,
            _ => throw new ArgumentException($"Unknown registry hive: {hive}")
        };
    }

    private static RegistryValueType MapValueKind(RegistryValueKind kind)
    {
        return kind switch
        {
            RegistryValueKind.String => RegistryValueType.String,
            RegistryValueKind.ExpandString => RegistryValueType.ExpandString,
            RegistryValueKind.Binary => RegistryValueType.Binary,
            RegistryValueKind.DWord => RegistryValueType.DWord,
            RegistryValueKind.MultiString => RegistryValueType.MultiString,
            RegistryValueKind.QWord => RegistryValueType.QWord,
            RegistryValueKind.None => RegistryValueType.None,
            _ => RegistryValueType.Unknown
        };
    }

    private static RegistryValueKind MapValueType(RegistryValueType type)
    {
        return type switch
        {
            RegistryValueType.String => RegistryValueKind.String,
            RegistryValueType.ExpandString => RegistryValueKind.ExpandString,
            RegistryValueType.Binary => RegistryValueKind.Binary,
            RegistryValueType.DWord => RegistryValueKind.DWord,
            RegistryValueType.MultiString => RegistryValueKind.MultiString,
            RegistryValueType.QWord => RegistryValueKind.QWord,
            RegistryValueType.None => RegistryValueKind.None,
            _ => throw new ArgumentException($"Unsupported value type: {type}")
        };
    }
}

