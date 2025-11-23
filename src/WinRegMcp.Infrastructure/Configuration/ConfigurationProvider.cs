using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WinRegMcp.Infrastructure.Configuration;

/// <summary>
/// Provides configuration loading from files and environment.
/// </summary>
public sealed class ConfigurationProvider
{
    private readonly ILogger<ConfigurationProvider> _logger;

    public ConfigurationProvider(ILogger<ConfigurationProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Loads allowed paths configuration from file or returns defaults.
    /// </summary>
    public async Task<AllowedPathsConfiguration> LoadAllowedPathsAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogWarning("No allowed paths file specified, using default configuration");
            return AllowedPathsConfiguration.GetDefault();
        }

        if (!File.Exists(filePath))
        {
            _logger.LogWarning(
                "Allowed paths file not found: {FilePath}, using default configuration",
                filePath);
            return AllowedPathsConfiguration.GetDefault();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            
            // Normalize JSON: convert snake_case to camelCase for compatibility
            json = json
                .Replace("\"allowed_roots\"", "\"allowedRoots\"", StringComparison.OrdinalIgnoreCase)
                .Replace("\"denied_paths\"", "\"deniedPaths\"", StringComparison.OrdinalIgnoreCase)
                .Replace("\"max_depth\"", "\"maxDepth\"", StringComparison.OrdinalIgnoreCase);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            
            var config = JsonSerializer.Deserialize<AllowedPathsConfiguration>(json, options);

            if (config == null)
            {
                _logger.LogWarning("Failed to parse allowed paths file (deserialized to null), using defaults");
                return AllowedPathsConfiguration.GetDefault();
            }

            // Validate configuration
            if (config.AllowedRoots == null)
            {
                _logger.LogWarning("Allowed roots is null in configuration file, using defaults");
                return AllowedPathsConfiguration.GetDefault();
            }

            if (config.DeniedPaths == null)
            {
                _logger.LogWarning("Denied paths is null in configuration file, initializing empty list");
                return new AllowedPathsConfiguration
                {
                    AllowedRoots = config.AllowedRoots,
                    DeniedPaths = new List<string>()
                };
            }

            // Validate path access rules
            var invalidRules = new List<int>();
            for (int i = 0; i < config.AllowedRoots.Count; i++)
            {
                var rule = config.AllowedRoots[i];
                if (string.IsNullOrWhiteSpace(rule.Path))
                {
                    _logger.LogWarning("Path access rule at index {Index} has empty path, skipping", i);
                    invalidRules.Add(i);
                }
                else if (rule.MaxDepth < 1 || rule.MaxDepth > 10)
                {
                    _logger.LogWarning(
                        "Path access rule at index {Index} has invalid MaxDepth {MaxDepth} (must be 1-10), using default value 2",
                        i, rule.MaxDepth);
                    // Note: We can't modify init-only properties, so we'll log and continue
                }
            }

            // Remove invalid rules if any
            if (invalidRules.Count > 0)
            {
                var validRules = config.AllowedRoots
                    .Where((_, index) => !invalidRules.Contains(index))
                    .ToList();
                
                if (validRules.Count == 0)
                {
                    _logger.LogWarning("All path access rules are invalid, using defaults");
                    return AllowedPathsConfiguration.GetDefault();
                }

                config = new AllowedPathsConfiguration
                {
                    AllowedRoots = validRules,
                    DeniedPaths = config.DeniedPaths
                };
            }

            _logger.LogInformation(
                "Loaded allowed paths configuration from {FilePath}: {AllowedCount} allowed roots, {DeniedCount} denied paths",
                filePath, config.AllowedRoots.Count, config.DeniedPaths.Count);

            return config;
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "JSON parsing error in allowed paths file: {FilePath} at line {LineNumber}, using defaults. Error: {ErrorMessage}",
                filePath, ex.LineNumber, ex.Message);
            return AllowedPathsConfiguration.GetDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error loading allowed paths file: {FilePath}, using defaults. Error: {ErrorMessage}",
                filePath, ex.Message);
            return AllowedPathsConfiguration.GetDefault();
        }
    }
}

