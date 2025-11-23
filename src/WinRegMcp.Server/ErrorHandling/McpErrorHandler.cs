using Microsoft.Extensions.Logging;
using WinRegMcp.Domain.Exceptions;

namespace WinRegMcp.Server.ErrorHandling;

/// <summary>
/// Handles domain exceptions and converts them to user-friendly error messages.
/// The MCP SDK will automatically wrap exceptions in proper protocol error format.
/// </summary>
public static class McpErrorHandler
{
    public static Exception EnrichException(
        Exception ex,
        ILogger logger,
        string correlationId)
    {
        logger.LogError(ex, "[{CorrelationId}] Error during operation", correlationId);

        // Enrich domain exceptions with better messages
        return ex switch
        {
            RegistryKeyNotFoundException keyNotFound => new InvalidOperationException(
                $"Registry key not found: {keyNotFound.RegistryPath}. [CorrelationId: {correlationId}]",
                keyNotFound),

            RegistryValueNotFoundException valueNotFound => new InvalidOperationException(
                $"Registry value '{valueNotFound.ValueName}' not found in key: {valueNotFound.RegistryPath}. [CorrelationId: {correlationId}]",
                valueNotFound),

            RegistryAccessDeniedException accessDenied => new UnauthorizedAccessException(
                $"Access denied to registry path: {accessDenied.RegistryPath}. {accessDenied.Reason} [CorrelationId: {correlationId}]",
                accessDenied),

            RegistryLimitExceededException limitExceeded => new InvalidOperationException(
                $"{limitExceeded.LimitType} limit exceeded. Requested: {limitExceeded.RequestedValue}, Maximum: {limitExceeded.MaxAllowedValue}. [CorrelationId: {correlationId}]",
                limitExceeded),

            RegistryInvalidValueTypeException invalidType => new ArgumentException(
                $"Invalid value type for '{invalidType.ValueName}'. Expected: {invalidType.ExpectedType}, Actual: {invalidType.ActualType}. [CorrelationId: {correlationId}]",
                invalidType),

            RegistryDomainException domain => new InvalidOperationException(
                $"{domain.Message} [CorrelationId: {correlationId}, ErrorCode: {domain.ErrorCode}]",
                domain),

            OperationCanceledException => new OperationCanceledException(
                $"Operation was cancelled. [CorrelationId: {correlationId}]",
                ex),

            TimeoutException => new TimeoutException(
                $"Operation timed out. [CorrelationId: {correlationId}]",
                ex),

            ArgumentException argEx => new ArgumentException(
                $"Invalid argument: {argEx.Message} [CorrelationId: {correlationId}]",
                argEx),

            _ => new Exception(
                $"Internal server error: {ex.Message} [CorrelationId: {correlationId}]",
                ex)
        };
    }
}

