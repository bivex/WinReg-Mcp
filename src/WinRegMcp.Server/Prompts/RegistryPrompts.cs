using System.ComponentModel;
using ModelContextProtocol.Server;

namespace WinRegMcp.Server.Prompts;

/// <summary>
/// MCP prompts for registry operations.
/// </summary>
[McpServerPromptType]
public static class RegistryPrompts
{
    [McpServerPrompt(Name = "safe_registry_query")]
    [Description("Template for constructing safe registry queries with proper path validation")]
    public static string SafeRegistryQuery(
        [Description("The registry path pattern to query")] string path_pattern)
    {
        return $@"
When querying the Windows Registry at path: {path_pattern}

Please ensure:
1. The path follows the format HKEY_*\SubKey\SubKey...
2. Avoid accessing security-critical paths (SECURITY, SAM, LSA)
3. Limit enumeration depth to avoid excessive data retrieval
4. Validate that the path is within allowed boundaries

Example safe query:
read_value(path=""HKEY_CURRENT_USER\Software\MyApp"", value_name=""Setting1"")

Always check the response for access denied errors and respect authorization levels.
";
    }

    [McpServerPrompt(Name = "registry_troubleshooting")]
    [Description("Guide for troubleshooting registry access issues")]
    public static string RegistryTroubleshooting()
    {
        return @"
Windows Registry MCP Server - Troubleshooting Guide

Common Issues and Solutions:

1. ACCESS_DENIED Error:
   - Check that the path is in the allowed list
   - Verify your authorization level (READ_ONLY, READ_WRITE, ADMIN)
   - Ensure you're not trying to write with READ_ONLY access

2. KEY_NOT_FOUND Error:
   - Verify the registry path exists
   - Check for typos in the path
   - Ensure you're using the correct hive name (HKEY_LOCAL_MACHINE, HKEY_CURRENT_USER, etc.)

3. PATH_NOT_ALLOWED Error:
   - The path is not in the configured allowed list
   - Check the allowed_paths.json configuration file
   - The path may be explicitly denied for security reasons

4. LIMIT_EXCEEDED Error:
   - Reduce enumeration depth
   - Query a more specific path
   - Break the query into smaller chunks

Safe Paths to Query:
- HKEY_CURRENT_USER\Software (user application settings)
- HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion (Windows version info)

Always Denied Paths (for security):
- HKEY_LOCAL_MACHINE\SECURITY
- HKEY_LOCAL_MACHINE\SAM
- HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa
";
    }
}

