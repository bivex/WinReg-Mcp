# Windows Registry MCP Server

A production-ready Model Context Protocol (MCP) server that provides controlled, secure access to the Windows Registry for AI models.

## Architecture

This server follows a clean, layered architecture with strict separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                    MCP Protocol Layer                        │
│  (JSON-RPC, stdio transport, request/response handling)     │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                  Application/Use-Case Layer                  │
│  - Tool Handlers (RegistryToolHandlers)                     │
│  - Resource Handlers (RegistryResourceHandlers)             │
│  - Authorization & Access Control                           │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                      Domain Layer                            │
│  - Registry Operations (IRegistryService)                   │
│  - Domain Models (RegistryKey, RegistryValue)               │
│  - Business Rules & Validation                              │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│                  Infrastructure Layer                        │
│  - WinReg Adapter (wraps Win32 Registry API)                │
│  - Configuration Provider                                   │
│  - Logging & Metrics                                        │
└─────────────────────────────────────────────────────────────┘
```

## Features

### Core Capabilities
- ✅ **Read Registry Values** - Query specific registry keys/values with path validation
- ✅ **Enumerate Keys** - List subkeys under a specific parent key
- ✅ **Enumerate Values** - List values under a specific key
- ✅ **Write Registry Values** - Create/update registry values with authorization
- ✅ **Delete Registry Items** - Remove keys/values with strict permission controls
- ✅ **Query Key Metadata** - Get information about keys (modification time, value count)

### Security Features
- 🔒 **Path Allow-List** - Only configured registry paths are accessible
- 🔒 **Authorization Levels** - READ_ONLY, READ_WRITE, ADMIN access control
- 🔒 **Data Exfiltration Protection** - Limits on enumeration depth and value counts
- 🔒 **Audit Logging** - All operations logged with correlation IDs
- 🔒 **Rate Limiting** - Configurable request rate limits
- 🔒 **Timeout Controls** - All operations have execution time limits

### Observability
- 📊 **Metrics** - Prometheus-compatible metrics for operations, latency, errors
- 📝 **Structured Logging** - JSON-formatted logs with correlation IDs
- 🏥 **Health Checks** - Liveness, readiness, and startup health endpoints

## Quick Start

### Prerequisites
- .NET 8.0 or later
- Windows OS (Server 2016+ or Windows 10+)

### Installation

```bash
# Clone the repository
git clone <repository-url>
cd winregcsharp-mcp

# Build the solution
dotnet build

# Run the server
dotnet run --project src/WinRegMcp.Server
```

### Configuration

Create a `config/allowed_paths.json` file:

```json
{
  "allowed_roots": [
    {
      "path": "HKEY_CURRENT_USER\\Software\\MyApp",
      "access": "read_write",
      "max_depth": 5
    }
  ],
  "denied_paths": [
    "HKEY_LOCAL_MACHINE\\SECURITY",
    "HKEY_LOCAL_MACHINE\\SAM"
  ]
}
```

Set environment variables:

```bash
WINREG_MCP_AUTHORIZATION_LEVEL=READ_ONLY
WINREG_MCP_ALLOWED_PATHS_FILE=config/allowed_paths.json
WINREG_MCP_LOG_LEVEL=info
```

## Available Tools

### read_value
Read a specific registry value.

**Parameters:**
- `path` (string): Full registry path (e.g., "HKEY_CURRENT_USER\\Software\\MyApp")
- `value_name` (string): Name of the value to read

**Returns:** Value data and type information

### write_value
Write or update a registry value.

**Parameters:**
- `path` (string): Full registry path
- `value_name` (string): Name of the value
- `value_data` (string): Data to write
- `value_type` (string): Registry type (String, DWord, QWord, Binary, etc.)

### enumerate_keys
List subkeys under a registry path.

**Parameters:**
- `path` (string): Parent registry path
- `max_depth` (integer, optional): Maximum enumeration depth (default: 1)

**Returns:** List of subkey names

### enumerate_values
List all values in a registry key.

**Parameters:**
- `path` (string): Registry key path

**Returns:** List of value names and types

### get_key_info
Get metadata about a registry key.

**Parameters:**
- `path` (string): Registry key path

**Returns:** Key information (subkey count, value count, last modified time)

### delete_value
Delete a registry value.

**Parameters:**
- `path` (string): Registry key path
- `value_name` (string): Name of the value to delete

### delete_key
Delete a registry key (requires ADMIN authorization).

**Parameters:**
- `path` (string): Registry key path to delete

## Security

### Default Allowed Paths (READ_ONLY)
- `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion`
- `HKEY_CURRENT_USER\Software` (limited depth)

### Always Denied Paths
- `HKEY_LOCAL_MACHINE\SECURITY`
- `HKEY_LOCAL_MACHINE\SAM`
- `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa`

### Authorization Levels
- **READ_ONLY**: Can only read from allowed paths
- **READ_WRITE**: Can read and write to allowed paths
- **ADMIN**: Full access (requires explicit configuration)

## Development

### Project Structure

```
src/
├── WinRegMcp.Domain/          # Domain models and interfaces
│   ├── Models/                # Registry domain models
│   ├── Services/              # Domain service interfaces
│   └── Exceptions/            # Domain exceptions
├── WinRegMcp.Infrastructure/  # External adapters
│   ├── Registry/              # Win32 Registry adapter
│   ├── Configuration/         # Config providers
│   └── Observability/         # Logging and metrics
├── WinRegMcp.Application/     # Use cases and handlers
│   ├── Handlers/              # MCP tool handlers
│   ├── Authorization/         # Access control
│   └── DTOs/                  # MCP contract DTOs
└── WinRegMcp.Server/          # MCP server entry point
    └── Program.cs

tests/
└── WinRegMcp.Tests/           # Unit and integration tests
```

### Running Tests

```bash
dotnet test
```

### Building for Production

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## Versioning

Current version: **1.0.0**

- Breaking changes increment major version
- New optional parameters increment minor version
- Bug fixes increment patch version
- Deprecation notice period: 3 months

## License

MIT License - See LICENSE file for details

## Contributing

Contributions welcome! Please read CONTRIBUTING.md for guidelines.

## Documentation

- [Architecture Guide](docs/ARCHITECTURE.md)
- [Security Model](docs/SECURITY.md)
- [API Reference](docs/API.md)
- [Configuration Reference](docs/CONFIGURATION.md)

