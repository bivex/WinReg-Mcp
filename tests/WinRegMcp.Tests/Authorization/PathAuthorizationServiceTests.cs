using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WinRegMcp.Application.Authorization;
using WinRegMcp.Domain.Exceptions;
using WinRegMcp.Domain.Models;
using WinRegMcp.Infrastructure.Configuration;
using Xunit;

namespace WinRegMcp.Tests.Authorization;

public class PathAuthorizationServiceTests
{
    private readonly AllowedPathsConfiguration _testConfig;
    private readonly PathAuthorizationService _authService;

    public PathAuthorizationServiceTests()
    {
        _testConfig = new AllowedPathsConfiguration
        {
            AllowedRoots = new List<PathAccessRule>
            {
                new()
                {
                    Path = "HKEY_CURRENT_USER\\Software\\TestApp",
                    Access = "read_write",
                    MaxDepth = 3
                },
                new()
                {
                    Path = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft",
                    Access = "read",
                    MaxDepth = 2
                }
            },
            DeniedPaths = new List<string>
            {
                "HKEY_LOCAL_MACHINE\\SECURITY",
                "HKEY_LOCAL_MACHINE\\SAM"
            }
        };

        _authService = new PathAuthorizationService(
            NullLogger<PathAuthorizationService>.Instance,
            _testConfig,
            globalMaxDepth: 5);
    }

    [Fact]
    public async Task ValidateReadAccess_AllowedPath_DoesNotThrow()
    {
        // Arrange
        var path = RegistryPath.Parse("HKEY_CURRENT_USER\\Software\\TestApp\\Settings");
        var context = CreateContext(AccessLevel.ReadOnly);

        // Act & Assert
        await _authService.ValidateReadAccessAsync(path, context);
    }

    [Fact]
    public async Task ValidateReadAccess_DeniedPath_ThrowsAccessDenied()
    {
        // Arrange
        var path = RegistryPath.Parse("HKEY_LOCAL_MACHINE\\SECURITY\\SAM");
        var context = CreateContext(AccessLevel.Admin);

        // Act & Assert
        await Assert.ThrowsAsync<RegistryAccessDeniedException>(() =>
            _authService.ValidateReadAccessAsync(path, context));
    }

    [Fact]
    public async Task ValidateReadAccess_PathNotInAllowedList_ThrowsAccessDenied()
    {
        // Arrange
        var path = RegistryPath.Parse("HKEY_LOCAL_MACHINE\\SYSTEM\\Random");
        var context = CreateContext(AccessLevel.ReadOnly);

        // Act & Assert
        await Assert.ThrowsAsync<RegistryAccessDeniedException>(() =>
            _authService.ValidateReadAccessAsync(path, context));
    }

    [Fact]
    public async Task ValidateWriteAccess_ReadOnlyContext_ThrowsAccessDenied()
    {
        // Arrange
        var path = RegistryPath.Parse("HKEY_CURRENT_USER\\Software\\TestApp");
        var context = CreateContext(AccessLevel.ReadOnly);

        // Act & Assert
        await Assert.ThrowsAsync<RegistryAccessDeniedException>(() =>
            _authService.ValidateWriteAccessAsync(path, context));
    }

    [Fact]
    public async Task ValidateWriteAccess_ReadWriteContextAndAllowedPath_DoesNotThrow()
    {
        // Arrange
        var path = RegistryPath.Parse("HKEY_CURRENT_USER\\Software\\TestApp\\Settings");
        var context = CreateContext(AccessLevel.ReadWrite);

        // Act & Assert
        await _authService.ValidateWriteAccessAsync(path, context);
    }

    [Fact]
    public async Task ValidateWriteAccess_ReadWriteContextButReadOnlyPath_ThrowsAccessDenied()
    {
        // Arrange
        var path = RegistryPath.Parse("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows");
        var context = CreateContext(AccessLevel.ReadWrite);

        // Act & Assert
        await Assert.ThrowsAsync<RegistryAccessDeniedException>(() =>
            _authService.ValidateWriteAccessAsync(path, context));
    }

    [Fact]
    public async Task ValidateDeleteAccess_NonAdminContext_ThrowsAccessDenied()
    {
        // Arrange
        var path = RegistryPath.Parse("HKEY_CURRENT_USER\\Software\\TestApp");
        var context = CreateContext(AccessLevel.ReadWrite);

        // Act & Assert
        await Assert.ThrowsAsync<RegistryAccessDeniedException>(() =>
            _authService.ValidateDeleteAccessAsync(path, context));
    }

    [Fact]
    public async Task ValidateEnumerationAccess_ExceedsMaxDepth_ReturnsReducedDepth()
    {
        // Arrange
        var path = RegistryPath.Parse("HKEY_CURRENT_USER\\Software\\TestApp");
        var context = CreateContext(AccessLevel.ReadOnly);

        // Act
        var allowedDepth = await _authService.ValidateEnumerationAccessAsync(path, 10, context);

        // Assert - Should be limited to rule's MaxDepth (3)
        Assert.Equal(3, allowedDepth);
    }

    [Fact]
    public async Task ValidateEnumerationAccess_WithinLimits_ReturnsRequestedDepth()
    {
        // Arrange
        var path = RegistryPath.Parse("HKEY_CURRENT_USER\\Software\\TestApp");
        var context = CreateContext(AccessLevel.ReadOnly);

        // Act
        var allowedDepth = await _authService.ValidateEnumerationAccessAsync(path, 2, context);

        // Assert
        Assert.Equal(2, allowedDepth);
    }

    private static RequestContext CreateContext(AccessLevel level)
    {
        return new RequestContext(
            correlationId: "test-123",
            accessLevel: level,
            cancellationToken: CancellationToken.None);
    }
}

