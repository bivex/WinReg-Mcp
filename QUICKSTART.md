# Quick Start Guide - Windows Registry MCP Server

## Prerequisites

- Windows 10/11 or Windows Server 2016+
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code (optional)

## Installation

### Option 1: Build from Source

```powershell
# Clone the repository
git clone <repository-url>
cd winregcsharp-mcp

# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run the server
dotnet run --project src/WinRegMcp.Server/WinRegMcp.Server.csproj
```

### Option 2: Using Make

```powershell
# Restore and build
make build

# Run tests
make test

# Run the server
make run

# Publish release build
make publish
```

## Configuration

### Quick Configuration

Create a `config/allowed_paths.json` file:

```json
{
  "allowed_roots": [
    {
      "path": "HKEY_CURRENT_USER\\Software\\MyApp",
      "access": "read_write",
      "max_depth": 3
    }
  ],
  "denied_paths": [
    "HKEY_LOCAL_MACHINE\\SECURITY",
    "HKEY_LOCAL_MACHINE\\SAM"
  ]
}
```

### Environment Variables

Set the authorization level (default is READ_ONLY):

```powershell
$env:WINREG_MCP_AUTHORIZATION_LEVEL="READ_WRITE"
$env:WINREG_MCP_ALLOWED_PATHS_FILE="config/allowed_paths.json"
```

## Usage Examples

### Using with Claude Desktop (or any MCP client)

1. **Configure MCP client** to connect to the server via stdio

2. **Read a registry value:**

```json
{
  "tool": "read_value",
  "arguments": {
    "path": "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer",
    "value_name": "ShellState"
  }
}
```

3. **Write a registry value:**

```json
{
  "tool": "write_value",
  "arguments": {
    "path": "HKEY_CURRENT_USER\\Software\\MyApp",
    "value_name": "Setting1",
    "value_data": "MyValue",
    "value_type": "String"
  }
}
```

4. **Enumerate subkeys:**

```json
{
  "tool": "enumerate_keys",
  "arguments": {
    "path": "HKEY_CURRENT_USER\\Software",
    "max_depth": 2
  }
}
```

### Testing the Server

Use the included tests:

```powershell
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~RegistryPathTests"
```

## Common Use Cases

### 1. Application Configuration Management

**Scenario**: Store and retrieve application settings

```json
{
  "allowed_roots": [
    {
      "path": "HKEY_CURRENT_USER\\Software\\MyCompany\\MyApp",
      "access": "read_write",
      "max_depth": 5
    }
  ]
}
```

### 2. System Information Retrieval

**Scenario**: Read Windows version and system info

```json
{
  "allowed_roots": [
    {
      "path": "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion",
      "access": "read",
      "max_depth": 2
    }
  ]
}
```

### 3. User Preferences

**Scenario**: Manage user-specific settings

```json
{
  "allowed_roots": [
    {
      "path": "HKEY_CURRENT_USER\\Software",
      "access": "read",
      "max_depth": 3
    }
  ]
}
```

## Security Best Practices

### For Production

1. **Use READ_ONLY by default**
   ```powershell
   $env:WINREG_MCP_AUTHORIZATION_LEVEL="READ_ONLY"
   ```

2. **Minimize allowed paths**
   - Only allow paths required for your use case
   - Use specific paths, not broad roots

3. **Always deny security-critical paths**
   ```json
   "denied_paths": [
     "HKEY_LOCAL_MACHINE\\SECURITY",
     "HKEY_LOCAL_MACHINE\\SAM",
     "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Lsa"
   ]
   ```

4. **Set appropriate limits**
   ```powershell
   $env:WINREG_MCP_MAX_ENUMERATION_DEPTH="2"
   $env:WINREG_MCP_MAX_VALUES_PER_QUERY="50"
   ```

### For Development

1. **Use test-specific registry hive**
   ```
   HKEY_CURRENT_USER\Software\MyApp_DEV
   ```

2. **Enable debug logging**
   ```powershell
   $env:WINREG_MCP_LOG_LEVEL="Debug"
   ```

3. **Use READ_WRITE for development**
   ```powershell
   $env:WINREG_MCP_AUTHORIZATION_LEVEL="READ_WRITE"
   ```

## Troubleshooting

### Issue: PATH_NOT_ALLOWED Error

**Solution**: Check that the path is in your allowed_paths.json

```powershell
# Verify your configuration file
cat config/allowed_paths.json
```

### Issue: Server won't start

**Solution**: Check logs in stderr

```powershell
# Run with debug logging
$env:WINREG_MCP_LOG_LEVEL="Debug"
dotnet run --project src/WinRegMcp.Server/WinRegMcp.Server.csproj 2> error.log
cat error.log
```

### Issue: Access Denied from Windows

**Solution**: Check Windows permissions

```powershell
# Run as Administrator (only if needed)
Start-Process powershell -Verb RunAs
```

## Docker Deployment (Windows Containers)

```powershell
# Build Docker image
docker build -t winreg-mcp-server:latest .

# Run container
docker run -it --rm `
  -e WINREG_MCP_AUTHORIZATION_LEVEL=READ_ONLY `
  -v ${PWD}/config:/config `
  winreg-mcp-server:latest
```

## Next Steps

1. **Read the full documentation:**
   - [Architecture Guide](docs/ARCHITECTURE.md)
   - [Security Model](docs/SECURITY.md)
   - [API Reference](docs/API.md)
   - [Configuration Reference](docs/CONFIGURATION.md)

2. **Explore examples:**
   - Check `config/allowed_paths.example.json`
   - Review tests in `tests/WinRegMcp.Tests/`

3. **Integrate with your MCP client:**
   - Configure stdio transport
   - Add to your MCP client configuration
   - Test with simple read operations first

## Support

For issues and questions:
- Check the [documentation](docs/)
- Review [test cases](tests/WinRegMcp.Tests/)
- Open an issue on GitHub

## License

MIT License - See LICENSE file for details

