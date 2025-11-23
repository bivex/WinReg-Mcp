using WinRegMcp.Domain.Models;

namespace WinRegMcp.Domain.Services;

/// <summary>
/// Service for validating registry access authorization.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Validates if a read operation is authorized for the given path.
    /// </summary>
    /// <param name="path">Registry path to read</param>
    /// <param name="context">Request context with authorization info</param>
    /// <exception cref="Exceptions.RegistryAccessDeniedException">If access is denied</exception>
    Task ValidateReadAccessAsync(RegistryPath path, RequestContext context);

    /// <summary>
    /// Validates if a write operation is authorized for the given path.
    /// </summary>
    /// <param name="path">Registry path to write</param>
    /// <param name="context">Request context with authorization info</param>
    /// <exception cref="Exceptions.RegistryAccessDeniedException">If access is denied</exception>
    Task ValidateWriteAccessAsync(RegistryPath path, RequestContext context);

    /// <summary>
    /// Validates if a delete operation is authorized for the given path.
    /// </summary>
    /// <param name="path">Registry path to delete</param>
    /// <param name="context">Request context with authorization info</param>
    /// <exception cref="Exceptions.RegistryAccessDeniedException">If access is denied</exception>
    Task ValidateDeleteAccessAsync(RegistryPath path, RequestContext context);

    /// <summary>
    /// Validates if enumeration is authorized for the given path and depth.
    /// </summary>
    /// <param name="path">Registry path to enumerate</param>
    /// <param name="requestedDepth">Requested enumeration depth</param>
    /// <param name="context">Request context</param>
    /// <returns>Maximum allowed depth (may be less than requested)</returns>
    Task<int> ValidateEnumerationAccessAsync(
        RegistryPath path, 
        int requestedDepth, 
        RequestContext context);
}

