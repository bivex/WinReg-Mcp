namespace WinRegMcp.Domain.Models;

/// <summary>
/// Represents the context of an incoming request with correlation and authorization info.
/// </summary>
public sealed class RequestContext
{
    public string CorrelationId { get; init; }
    public AccessLevel AccessLevel { get; init; }
    public string? UserId { get; init; }
    public string? WorkspaceId { get; init; }
    public DateTime RequestTime { get; init; }
    public CancellationToken CancellationToken { get; init; }

    public RequestContext(
        string correlationId,
        AccessLevel accessLevel,
        CancellationToken cancellationToken = default,
        string? userId = null,
        string? workspaceId = null)
    {
        CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
        AccessLevel = accessLevel;
        CancellationToken = cancellationToken;
        UserId = userId;
        WorkspaceId = workspaceId;
        RequestTime = DateTime.UtcNow;
    }
}

