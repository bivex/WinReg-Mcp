using Microsoft.Extensions.Logging;
using WinRegMcp.Domain.Exceptions;
using WinRegMcp.Domain.Models;
using WinRegMcp.Domain.Services;
using WinRegMcp.Infrastructure.Configuration;

namespace WinRegMcp.Application.Authorization;

/// <summary>
/// Authorization service that validates registry access based on allowed/denied paths.
/// </summary>
public sealed class PathAuthorizationService : IAuthorizationService
{
    private readonly ILogger<PathAuthorizationService> _logger;
    private readonly AllowedPathsConfiguration _allowedPaths;
    private readonly int _globalMaxDepth;

    public PathAuthorizationService(
        ILogger<PathAuthorizationService> logger,
        AllowedPathsConfiguration allowedPaths,
        int globalMaxDepth = 3)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _allowedPaths = allowedPaths ?? throw new ArgumentNullException(nameof(allowedPaths));
        _globalMaxDepth = globalMaxDepth;
    }

    public Task ValidateReadAccessAsync(RegistryPath path, RequestContext context)
    {
        ValidatePathAccess(path, context, AccessLevel.ReadOnly);
        return Task.CompletedTask;
    }

    public Task ValidateWriteAccessAsync(RegistryPath path, RequestContext context)
    {
        if (context.AccessLevel == AccessLevel.ReadOnly)
        {
            throw new RegistryAccessDeniedException(
                path.GetNormalizedPath(),
                "Write access denied: User has READ_ONLY authorization level");
        }

        ValidatePathAccess(path, context, AccessLevel.ReadWrite);
        return Task.CompletedTask;
    }

    public Task ValidateDeleteAccessAsync(RegistryPath path, RequestContext context)
    {
        if (context.AccessLevel != AccessLevel.Admin)
        {
            throw new RegistryAccessDeniedException(
                path.GetNormalizedPath(),
                "Delete access denied: ADMIN authorization level required");
        }

        ValidatePathAccess(path, context, AccessLevel.Admin);
        return Task.CompletedTask;
    }

    public Task<int> ValidateEnumerationAccessAsync(
        RegistryPath path,
        int requestedDepth,
        RequestContext context)
    {
        ValidatePathAccess(path, context, AccessLevel.ReadOnly);

        // Find the matching allowed root to check max depth
        var matchingRule = FindMatchingAllowedRule(path);
        var maxDepth = matchingRule?.MaxDepth ?? _globalMaxDepth;

        // Apply global maximum
        maxDepth = Math.Min(maxDepth, _globalMaxDepth);

        var allowedDepth = Math.Min(requestedDepth, maxDepth);

        if (allowedDepth < requestedDepth)
        {
            _logger.LogWarning(
                "[{CorrelationId}] Enumeration depth reduced from {Requested} to {Allowed} for path: {Path}",
                context.CorrelationId, requestedDepth, allowedDepth, path);
        }

        return Task.FromResult(allowedDepth);
    }

    private void ValidatePathAccess(
        RegistryPath path,
        RequestContext context,
        AccessLevel requiredLevel)
    {
        var normalizedPath = path.GetNormalizedPath();
        _logger.LogDebug(
            "[{CorrelationId}] Validating access to path: {Path}, Required level: {RequiredLevel}",
            context.CorrelationId, normalizedPath, requiredLevel);

        // First check if path is explicitly denied
        _logger.LogDebug(
            "[{CorrelationId}] Checking if path is in denied list (total denied paths: {Count})",
            context.CorrelationId, _allowedPaths.DeniedPaths.Count);

        if (IsPathDenied(path))
        {
            _logger.LogWarning(
                "[{CorrelationId}] Access denied to explicitly denied path: {Path}",
                context.CorrelationId, normalizedPath);
            
            throw new RegistryAccessDeniedException(
                normalizedPath,
                "Path is in the denied list");
        }

        _logger.LogDebug(
            "[{CorrelationId}] Path not in denied list, checking allowed roots (total allowed roots: {Count})",
            context.CorrelationId, _allowedPaths.AllowedRoots.Count);

        // Check if path is under an allowed root
        var matchingRule = FindMatchingAllowedRule(path);
        if (matchingRule == null)
        {
            _logger.LogWarning(
                "[{CorrelationId}] Access denied to path not in allowed list: {Path}. Checked {Count} allowed roots",
                context.CorrelationId, normalizedPath, _allowedPaths.AllowedRoots.Count);
            
            throw new RegistryAccessDeniedException(
                normalizedPath,
                "Path is not in the allowed list");
        }

        _logger.LogDebug(
            "[{CorrelationId}] Found matching allowed rule: Path={RulePath}, Access={RuleAccess}, MaxDepth={MaxDepth}",
            context.CorrelationId, matchingRule.Path, matchingRule.Access, matchingRule.MaxDepth);

        // Check if the allowed rule grants sufficient access level
        var ruleAccessLevel = matchingRule.GetAccessLevel();
        if (ruleAccessLevel < requiredLevel)
        {
            _logger.LogWarning(
                "[{CorrelationId}] Insufficient access level for path: {Path}. Required: {Required}, Rule: {Rule}",
                context.CorrelationId, normalizedPath, requiredLevel, ruleAccessLevel);
            
            throw new RegistryAccessDeniedException(
                normalizedPath,
                $"Insufficient access level. Required: {requiredLevel}, Allowed: {ruleAccessLevel}");
        }

        _logger.LogDebug(
            "[{CorrelationId}] Access granted to path: {Path} (Level: {Level}, Rule: {RulePath})",
            context.CorrelationId, normalizedPath, requiredLevel, matchingRule.Path);
    }

    private bool IsPathDenied(RegistryPath path)
    {
        foreach (var deniedPathStr in _allowedPaths.DeniedPaths)
        {
            try
            {
                var deniedPath = RegistryPath.Parse(deniedPathStr);
                if (deniedPath.IsParentOfOrEqual(path))
                {
                    return true;
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid denied path in configuration: {Path}", deniedPathStr);
            }
        }

        return false;
    }

    private PathAccessRule? FindMatchingAllowedRule(RegistryPath path)
    {
        foreach (var rule in _allowedPaths.AllowedRoots)
        {
            try
            {
                var allowedPath = RegistryPath.Parse(rule.Path);
                if (allowedPath.IsParentOfOrEqual(path))
                {
                    return rule;
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid allowed path in configuration: {Path}", rule.Path);
            }
        }

        return null;
    }
}

