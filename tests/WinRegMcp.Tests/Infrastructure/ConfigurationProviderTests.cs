using Microsoft.Extensions.Logging.Abstractions;
using WinRegMcp.Domain.Models;
using WinRegMcp.Infrastructure.Configuration;
using Xunit;

namespace WinRegMcp.Tests.Infrastructure;

public class ConfigurationProviderTests
{
    private readonly ConfigurationProvider _provider;

    public ConfigurationProviderTests()
    {
        _provider = new ConfigurationProvider(NullLogger<ConfigurationProvider>.Instance);
    }

    [Fact]
    public async Task LoadAllowedPaths_NullPath_ReturnsDefaultConfiguration()
    {
        // Act
        var config = await _provider.LoadAllowedPathsAsync(null);

        // Assert
        Assert.NotNull(config);
        Assert.NotEmpty(config.AllowedRoots);
        Assert.NotEmpty(config.DeniedPaths);
    }

    [Fact]
    public async Task LoadAllowedPaths_NonExistentFile_ReturnsDefaultConfiguration()
    {
        // Act
        var config = await _provider.LoadAllowedPathsAsync("nonexistent.json");

        // Assert
        Assert.NotNull(config);
        Assert.NotEmpty(config.AllowedRoots);
    }

    [Fact]
    public void AllowedPathsConfiguration_GetDefault_ContainsSafePaths()
    {
        // Act
        var config = AllowedPathsConfiguration.GetDefault();

        // Assert
        Assert.Contains(config.AllowedRoots,
            r => r.Path.Contains("CurrentVersion", StringComparison.OrdinalIgnoreCase));
        
        Assert.Contains(config.DeniedPaths,
            p => p.Contains("SECURITY", StringComparison.OrdinalIgnoreCase));
        
        Assert.Contains(config.DeniedPaths,
            p => p.Contains("SAM", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PathAccessRule_GetAccessLevel_ParsesCorrectly()
    {
        // Arrange
        var readRule = new PathAccessRule { Access = "read" };
        var readWriteRule = new PathAccessRule { Access = "read_write" };
        var adminRule = new PathAccessRule { Access = "admin" };

        // Act & Assert
        Assert.Equal(AccessLevel.ReadOnly, readRule.GetAccessLevel());
        Assert.Equal(AccessLevel.ReadWrite, readWriteRule.GetAccessLevel());
        Assert.Equal(AccessLevel.Admin, adminRule.GetAccessLevel());
    }
}

