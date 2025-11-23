using WinRegMcp.Domain.Models;

namespace WinRegMcp.Domain.Services;

/// <summary>
/// Domain service interface for Windows Registry operations.
/// All implementations must be thread-safe and handle cancellation.
/// </summary>
public interface IRegistryService
{
    /// <summary>
    /// Reads a value from the registry.
    /// </summary>
    /// <param name="path">Registry key path</param>
    /// <param name="valueName">Name of the value to read</param>
    /// <param name="context">Request context</param>
    /// <returns>Registry value</returns>
    /// <exception cref="Exceptions.RegistryKeyNotFoundException">Key does not exist</exception>
    /// <exception cref="Exceptions.RegistryValueNotFoundException">Value does not exist</exception>
    Task<RegistryValue> ReadValueAsync(
        RegistryPath path, 
        string valueName, 
        RequestContext context);

    /// <summary>
    /// Writes a value to the registry.
    /// </summary>
    /// <param name="path">Registry key path</param>
    /// <param name="valueName">Name of the value to write</param>
    /// <param name="data">Value data</param>
    /// <param name="valueType">Value type</param>
    /// <param name="context">Request context</param>
    Task WriteValueAsync(
        RegistryPath path,
        string valueName,
        object data,
        RegistryValueType valueType,
        RequestContext context);

    /// <summary>
    /// Deletes a value from the registry.
    /// </summary>
    /// <param name="path">Registry key path</param>
    /// <param name="valueName">Name of the value to delete</param>
    /// <param name="context">Request context</param>
    Task DeleteValueAsync(
        RegistryPath path,
        string valueName,
        RequestContext context);

    /// <summary>
    /// Enumerates subkeys under a registry path.
    /// </summary>
    /// <param name="path">Parent registry path</param>
    /// <param name="maxDepth">Maximum enumeration depth (default: 1)</param>
    /// <param name="context">Request context</param>
    /// <returns>List of subkey names</returns>
    Task<IReadOnlyList<string>> EnumerateKeysAsync(
        RegistryPath path,
        int maxDepth,
        RequestContext context);

    /// <summary>
    /// Enumerates all values in a registry key.
    /// </summary>
    /// <param name="path">Registry key path</param>
    /// <param name="context">Request context</param>
    /// <returns>List of registry values</returns>
    Task<IReadOnlyList<RegistryValue>> EnumerateValuesAsync(
        RegistryPath path,
        RequestContext context);

    /// <summary>
    /// Gets metadata about a registry key.
    /// </summary>
    /// <param name="path">Registry key path</param>
    /// <param name="context">Request context</param>
    /// <returns>Registry key information</returns>
    Task<RegistryKey> GetKeyInfoAsync(
        RegistryPath path,
        RequestContext context);

    /// <summary>
    /// Deletes a registry key and all its subkeys.
    /// </summary>
    /// <param name="path">Registry key path to delete</param>
    /// <param name="context">Request context</param>
    Task DeleteKeyAsync(
        RegistryPath path,
        RequestContext context);

    /// <summary>
    /// Checks if a registry key exists.
    /// </summary>
    /// <param name="path">Registry key path</param>
    /// <param name="context">Request context</param>
    /// <returns>True if key exists, false otherwise</returns>
    Task<bool> KeyExistsAsync(
        RegistryPath path,
        RequestContext context);
}

