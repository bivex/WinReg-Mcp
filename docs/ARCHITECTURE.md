# Windows Registry MCP Server - Architecture Guide

## Overview

The Windows Registry MCP Server is designed following clean architecture principles with strict separation of concerns across four main layers: Domain, Infrastructure, Application, and MCP Protocol.

## Architectural Layers

### 1. Domain Layer (`WinRegMcp.Domain`)

**Responsibility**: Core business logic and domain models

**Key Components**:
- **Models**: `RegistryPath`, `RegistryKey`, `RegistryValue`, `AccessLevel`, `RequestContext`
- **Services**: `IRegistryService`, `IAuthorizationService` (interfaces only)
- **Exceptions**: Domain-specific exceptions with error codes

**Key Principles**:
- No dependencies on external frameworks
- Pure business logic
- Technology-agnostic
- Immutable domain models

**Example - Registry Path Validation**:
```csharp
public sealed class RegistryPath
{
    public static RegistryPath Parse(string fullPath)
    {
        // Validates and normalizes registry paths
        // Ensures consistent format
        // No Win32 API dependencies
    }
}
```

### 2. Infrastructure Layer (`WinRegMcp.Infrastructure`)

**Responsibility**: External system adapters and technical concerns

**Key Components**:
- **Registry Adapter**: `WindowsRegistryService` - wraps Win32 Registry API
- **Configuration**: `ConfigurationProvider`, `AllowedPathsConfiguration`
- **Observability**: `MetricsCollector`, `CorrelationIdGenerator`

**Key Principles**:
- Implements domain service interfaces
- Encapsulates all Win32 Registry API calls
- Handles technical concerns (config loading, metrics)
- Thread-safe implementations

**Example - Registry Adapter**:
```csharp
public sealed class WindowsRegistryService : IRegistryService
{
    public async Task<RegistryValue> ReadValueAsync(
        RegistryPath path,
        string valueName,
        RequestContext context)
    {
        // Validate authorization
        // Call Win32 Registry API
        // Convert to domain model
        // Handle errors
    }
}
```

### 3. Application Layer (`WinRegMcp.Application`)

**Responsibility**: Use case orchestration and authorization

**Key Components**:
- **Handlers**: `RegistryToolHandlers` - coordinates operations
- **Authorization**: `PathAuthorizationService` - validates access
- **DTOs**: MCP contract objects (separate from domain models)

**Key Principles**:
- Orchestrates domain services
- Enforces authorization rules
- Translates between MCP DTOs and domain models
- Handles cross-cutting concerns (logging, metrics)

**Example - Tool Handler**:
```csharp
public async Task<RegistryValueResponse> HandleReadValueAsync(
    ReadValueRequest request,
    RequestContext context)
{
    // Parse and validate input
    // Call domain service
    // Record metrics
    // Convert to DTO
    // Handle errors
}
```

### 4. MCP Protocol Layer (`WinRegMcp.Server`)

**Responsibility**: MCP protocol implementation and server hosting

**Key Components**:
- **Tools**: `RegistryTools` - MCP tool definitions
- **Prompts**: `RegistryPrompts` - MCP prompt templates
- **Error Handling**: `McpErrorHandler` - converts domain exceptions to MCP errors
- **Program**: Server entry point with DI configuration

**Key Principles**:
- Thin adapter layer over application
- MCP-specific concerns only
- Protocol error handling
- Request correlation

**Example - MCP Tool**:
```csharp
[McpServerTool(Name = "read_value")]
[Description("Read a specific registry value")]
public async Task<RegistryValueResponse> ReadValueAsync(
    string path,
    string value_name,
    CancellationToken cancellationToken)
{
    var context = CreateRequestContext(cancellationToken);
    var request = new ReadValueRequest { Path = path, ValueName = value_name };
    return await _handlers.HandleReadValueAsync(request, context);
}
```

## Data Flow

### Read Operation Flow

```
1. MCP Client → Stdio Transport
   ↓
2. MCP Protocol Layer (RegistryTools.ReadValueAsync)
   ↓
3. Application Layer (RegistryToolHandlers.HandleReadValueAsync)
   ↓
4. Authorization Service (ValidateReadAccessAsync)
   ↓
5. Domain Service (IRegistryService.ReadValueAsync)
   ↓
6. Infrastructure (WindowsRegistryService - Win32 API call)
   ↓
7. Domain Model (RegistryValue)
   ↓
8. DTO Conversion (RegistryValueResponse)
   ↓
9. MCP Protocol Layer (JSON-RPC response)
   ↓
10. MCP Client ← Stdio Transport
```

## Security Architecture

### Authorization Flow

1. **Request Context Creation**: Every operation creates a `RequestContext` with:
   - Correlation ID (for tracing)
   - Access Level (READ_ONLY, READ_WRITE, ADMIN)
   - Cancellation Token (for timeouts)
   - User/Workspace identifiers (optional)

2. **Path Validation**: `PathAuthorizationService` checks:
   - Is path explicitly denied? → Reject
   - Is path under allowed root? → Continue
   - Does allowed rule grant sufficient access? → Continue
   - Otherwise → Reject

3. **Operation Execution**: Only after authorization passes

### Path Allow-List Strategy

**Allowed Roots**: Explicitly configured paths with access levels
```json
{
  "path": "HKEY_CURRENT_USER\\Software\\MyApp",
  "access": "read_write",
  "max_depth": 5
}
```

**Denied Paths**: Always blocked regardless of other rules
```json
[
  "HKEY_LOCAL_MACHINE\\SECURITY",
  "HKEY_LOCAL_MACHINE\\SAM"
]
```

**Path Hierarchy**: Authorization checks parent-child relationships
- `HKLM\SOFTWARE` allows `HKLM\SOFTWARE\Microsoft`
- But not `HKLM\SYSTEM`

## Concurrency Model

### Thread Safety

- **Stateless Design**: Server holds no request-specific state
- **Immutable Models**: Domain models are immutable
- **Thread-Safe Services**: All services support concurrent operations
- **Win32 Registry**: Thread-safe by design (handle-based access)

### Cancellation Support

Every operation supports `CancellationToken`:
- Timeout enforcement (configurable, default 5s)
- Long-running enumerations can be cancelled
- Graceful shutdown handling

### Parallel Operations

The MCP server can handle multiple tool calls in parallel:
- Each operation gets its own `RequestContext`
- No shared mutable state
- Independent Win32 Registry handles

## Error Handling

### Exception Hierarchy

```
Exception
└── RegistryDomainException (base)
    ├── RegistryKeyNotFoundException
    ├── RegistryValueNotFoundException
    ├── RegistryAccessDeniedException
    ├── RegistryLimitExceededException
    └── RegistryInvalidValueTypeException
```

### Error Propagation

1. **Domain Layer**: Throws typed domain exceptions
2. **Application Layer**: Logs and re-throws
3. **MCP Protocol Layer**: Converts to `McpProtocolException` with appropriate error codes

### Error Response Format

```json
{
  "error": {
    "code": "PATH_NOT_ALLOWED",
    "message": "Access to registry path is not permitted",
    "data": {
      "correlationId": "req-12345",
      "path": "HKLM\\SECURITY",
      "reason": "Path is in the denied list"
    }
  }
}
```

## Dependency Injection

### Service Registration

```csharp
// Infrastructure services
services.AddSingleton<MetricsCollector>();
services.AddSingleton<ConfigurationProvider>();
services.AddSingleton<AllowedPathsConfiguration>();
services.AddSingleton<RegistryLimits>();

// Domain services
services.AddSingleton<IAuthorizationService, PathAuthorizationService>();
services.AddSingleton<IRegistryService, WindowsRegistryService>();

// Application handlers
services.AddSingleton<RegistryToolHandlers>();

// MCP tools
services.AddSingleton<RegistryTools>();
```

### Constructor Injection

All services use constructor injection:
```csharp
public class WindowsRegistryService : IRegistryService
{
    private readonly ILogger<WindowsRegistryService> _logger;
    private readonly IAuthorizationService _authorizationService;
    private readonly RegistryLimits _limits;

    public WindowsRegistryService(
        ILogger<WindowsRegistryService> logger,
        IAuthorizationService authorizationService,
        RegistryLimits limits)
    {
        _logger = logger;
        _authorizationService = authorizationService;
        _limits = limits;
    }
}
```

## Configuration Management

### Configuration Sources

1. **Environment Variables**: Primary configuration source
2. **Configuration Files**: `allowed_paths.json`
3. **Defaults**: Built-in safe defaults

### Configuration Hierarchy

```
Environment Variables
    ↓ (override)
Configuration Files
    ↓ (fallback)
Default Values
```

### Configuration Loading

```csharp
// Server config from environment
var serverConfig = ServerConfiguration.LoadFromEnvironment();

// Allowed paths from file (with fallback to defaults)
var allowedPaths = await configProvider.LoadAllowedPathsAsync(
    serverConfig.AllowedPathsFile);

// Registry limits from environment with defaults
var limits = new RegistryLimits
{
    MaxEnumerationDepth = GetEnvInt("WINREG_MCP_MAX_ENUMERATION_DEPTH", 3),
    MaxValuesPerQuery = GetEnvInt("WINREG_MCP_MAX_VALUES_PER_QUERY", 100)
};
```

## Observability

### Correlation IDs

Every request gets a unique correlation ID:
- Format: `req-{timestamp}-{counter}`
- Propagated through all layers
- Included in all log entries

### Metrics

Collected metrics:
- `registry_operations_total{operation, status}` - Counter
- `registry_operation_duration_seconds{operation}` - Histogram
- `registry_errors_total{error_type}` - Counter
- `registry_concurrent_operations` - Gauge

### Logging

Structured logging with:
- Correlation ID in every log entry
- Operation details
- Error stack traces
- No sensitive data (registry values not logged)

### Example Log Entry

```json
{
  "timestamp": "2024-01-01T12:00:00Z",
  "level": "Information",
  "correlationId": "req-1704110400000-ABCD1234",
  "message": "Reading registry value",
  "path": "HKEY_CURRENT_USER\\Software\\MyApp",
  "valueName": "Setting1"
}
```

## Testability

### Testing Strategy

1. **Unit Tests**: Domain logic, authorization rules, path validation
2. **Integration Tests**: Real registry operations (isolated test hive)
3. **Contract Tests**: MCP DTO mapping

### Test Isolation

```csharp
// Use test-specific registry hive
const string TestHive = "HKEY_CURRENT_USER\\Software\\WinRegMCPTests";

// Clean up after tests
[Fact]
public async Task TestRegistryOperation()
{
    // Arrange
    CreateTestKey();
    
    try
    {
        // Act & Assert
    }
    finally
    {
        DeleteTestKey(); // Cleanup
    }
}
```

### Mocking

Domain service interfaces can be easily mocked:
```csharp
var mockRegistryService = new Mock<IRegistryService>();
mockRegistryService
    .Setup(s => s.ReadValueAsync(It.IsAny<RegistryPath>(), ...))
    .ReturnsAsync(expectedValue);
```

## Deployment

### Container Packaging

```dockerfile
FROM mcr.microsoft.com/windows/servercore:ltsc2022
COPY bin/Release/net8.0/win-x64/publish/ /app/
COPY config/ /config/
ENTRYPOINT ["C:\\app\\WinRegMcp.Server.exe"]
```

### Resource Requirements

- **Memory**: 256 MB
- **CPU**: 0.5 cores
- **Disk**: 50 MB

### Health Checks

- **Startup**: Configuration loaded successfully
- **Liveness**: Process responding
- **Readiness**: Can access registry

## Extension Points

### Adding New Tools

1. Add method to `RegistryTools` class
2. Decorate with `[McpServerTool]` attribute
3. Add handler method in `RegistryToolHandlers`
4. Update tests

### Custom Authorization Rules

Implement `IAuthorizationService`:
```csharp
public class CustomAuthorizationService : IAuthorizationService
{
    public Task ValidateReadAccessAsync(RegistryPath path, RequestContext context)
    {
        // Custom authorization logic
    }
}
```

### Additional Transports

MCP SDK supports multiple transports:
- Stdio (current implementation)
- HTTP/SSE
- WebSocket

Change in `Program.cs`:
```csharp
builder.Services
    .AddMcpServer()
    .WithHttpServerTransport() // Instead of WithStdioServerTransport()
    .WithToolsFromAssembly();
```

## Performance Considerations

### Operation Timeouts

All operations have configurable timeouts (default: 5s):
- Prevents hanging operations
- Enforced via `CancellationToken`
- Configurable per deployment

### Enumeration Limits

Protect against excessive data retrieval:
- Max depth: 3 levels (configurable)
- Max values per query: 100 (configurable)
- Max value size: 1 MB (configurable)

### Caching Strategy

Currently no caching (registry is authoritative source):
- Every read goes to actual registry
- Ensures data consistency
- Could add caching layer for read-heavy workloads

## Security Considerations

### Threat Model

**Threats Mitigated**:
- **Data Exfiltration**: Path allow-lists and enumeration limits
- **Unauthorized Modification**: Write access requires explicit configuration
- **Privilege Escalation**: Admin operations require ADMIN level
- **DoS**: Rate limiting, timeouts, size limits

**Threats NOT Mitigated** (by design):
- **Physical Access**: Requires Windows security
- **Process Memory Inspection**: Requires OS-level protections
- **Malicious MCP Host**: Trust boundary is at MCP protocol

### Least Privilege

- Default: READ_ONLY access
- Minimal default allowed paths
- Explicit configuration required for write access
- Admin operations explicitly opt-in

### Audit Trail

All operations logged with:
- Who (user/workspace if available)
- What (operation type)
- Where (registry path)
- When (timestamp)
- Result (success/failure)
- Correlation ID (for tracing)

## Versioning Strategy

### Schema Versioning

Tool schemas are versioned:
- Current version: 1.0.0
- Breaking changes → major version bump
- New optional parameters → minor version bump
- Bug fixes → patch version bump

### Backward Compatibility

Maintained for at least 2 major versions:
- Old tools marked deprecated
- Deprecation warnings in logs
- Removal after deprecation period (3 months)

### API Evolution Example

```csharp
// v1.0.0
[McpServerTool(Name = "read_value")]
public Task<RegistryValueResponse> ReadValueAsync(string path, string value_name) { }

// v2.0.0 - Added optional parameter (minor version bump)
[McpServerTool(Name = "read_value")]
public Task<RegistryValueResponse> ReadValueAsync(
    string path,
    string value_name,
    bool include_metadata = false) { }

// v3.0.0 - Changed response format (major version bump)
[McpServerTool(Name = "read_value")]
public Task<RegistryValueResponseV3> ReadValueAsync(string path, string value_name) { }
```

## References

- [MCP C# SDK Documentation](https://github.com/modelcontextprotocol/csharp-sdk)
- [MCP Protocol Specification](https://modelcontextprotocol.io/docs)
- [Clean Architecture Principles](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Win32 Registry API](https://learn.microsoft.com/en-us/windows/win32/sysinfo/registry)

