using WinRegMcp.Domain.Models;
using Xunit;

namespace WinRegMcp.Tests.Domain;

public class RegistryPathTests
{
    [Theory]
    [InlineData("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft", RegistryHive.LocalMachine, "SOFTWARE\\Microsoft")]
    [InlineData("HKLM\\SOFTWARE\\Test", RegistryHive.LocalMachine, "SOFTWARE\\Test")]
    [InlineData("HKEY_CURRENT_USER\\Software", RegistryHive.CurrentUser, "Software")]
    [InlineData("HKCU\\Software", RegistryHive.CurrentUser, "Software")]
    public void Parse_ValidPath_ReturnsCorrectComponents(string input, RegistryHive expectedHive, string expectedSubKey)
    {
        // Act
        var path = RegistryPath.Parse(input);

        // Assert
        Assert.Equal(expectedHive, path.Hive);
        Assert.Equal(expectedSubKey, path.SubKeyPath);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("INVALID_HIVE\\SOFTWARE")]
    public void Parse_InvalidPath_ThrowsArgumentException(string input)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => RegistryPath.Parse(input));
    }

    [Fact]
    public void GetNormalizedPath_ReturnsStandardizedPath()
    {
        // Arrange
        var path = RegistryPath.Parse("HKLM\\SOFTWARE\\Test");

        // Act
        var normalized = path.GetNormalizedPath();

        // Assert
        Assert.Equal("HKEY_LOCAL_MACHINE\\SOFTWARE\\Test", normalized);
    }

    [Theory]
    [InlineData("HKEY_LOCAL_MACHINE\\SOFTWARE", 1)]
    [InlineData("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows", 3)]
    [InlineData("HKEY_LOCAL_MACHINE", 0)]
    public void GetDepth_ReturnsCorrectDepth(string input, int expectedDepth)
    {
        // Arrange
        var path = RegistryPath.Parse(input);

        // Act
        var depth = path.GetDepth();

        // Assert
        Assert.Equal(expectedDepth, depth);
    }

    [Fact]
    public void IsParentOfOrEqual_SamePathDifferentCase_ReturnsTrue()
    {
        // Arrange
        var parent = RegistryPath.Parse("HKEY_LOCAL_MACHINE\\SOFTWARE");
        var child = RegistryPath.Parse("hkey_local_machine\\software");

        // Act
        var result = parent.IsParentOfOrEqual(child);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsParentOfOrEqual_ParentAndChild_ReturnsTrue()
    {
        // Arrange
        var parent = RegistryPath.Parse("HKEY_LOCAL_MACHINE\\SOFTWARE");
        var child = RegistryPath.Parse("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft");

        // Act
        var result = parent.IsParentOfOrEqual(child);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsParentOfOrEqual_DifferentHive_ReturnsFalse()
    {
        // Arrange
        var path1 = RegistryPath.Parse("HKEY_LOCAL_MACHINE\\SOFTWARE");
        var path2 = RegistryPath.Parse("HKEY_CURRENT_USER\\SOFTWARE");

        // Act
        var result = path1.IsParentOfOrEqual(path2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsParentOfOrEqual_UnrelatedPaths_ReturnsFalse()
    {
        // Arrange
        var path1 = RegistryPath.Parse("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft");
        var path2 = RegistryPath.Parse("HKEY_LOCAL_MACHINE\\SYSTEM");

        // Act
        var result = path1.IsParentOfOrEqual(path2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Equals_SamePathDifferentCase_ReturnsTrue()
    {
        // Arrange
        var path1 = RegistryPath.Parse("HKEY_LOCAL_MACHINE\\SOFTWARE\\Test");
        var path2 = RegistryPath.Parse("hkey_local_machine\\software\\test");

        // Act & Assert
        Assert.True(path1.Equals(path2));
        Assert.Equal(path1.GetHashCode(), path2.GetHashCode());
    }
}

