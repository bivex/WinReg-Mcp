namespace WinRegMcp.Domain.Models;

/// <summary>
/// Defines the authorization level for registry operations.
/// </summary>
public enum AccessLevel
{
    /// <summary>
    /// Can only read from allowed paths.
    /// </summary>
    ReadOnly,

    /// <summary>
    /// Can read and write to allowed paths.
    /// </summary>
    ReadWrite,

    /// <summary>
    /// Full access including key deletion (requires explicit configuration).
    /// </summary>
    Admin
}

