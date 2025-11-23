# Windows Registry MCP Server - Configuration Reference

## Overview

The Windows Registry MCP Server is configured through environment variables and configuration files. This document describes all available configuration options.

## Configuration Sources

Configuration is loaded in the following priority order:

1. **Environment Variables** (highest priority)
2. **Configuration Files**
3. **Built-in Defaults** (lowest priority)

## Environment Variables

### Server Configuration

#### WINREG_MCP_SERVER_NAME

**Description**: Name of the MCP server

**Type**: String

**Default**: `winreg-mcp-server`

**Example**:
```bash
WINREG_MCP_SERVER_NAME=my-registry-server
```

---

#### WINREG_MCP_LOG_LEVEL

**Description**: Minimum log level

**Type**: String

**Valid Values**: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`

**Default**: `Information`

**Example**:
```bash
WINREG_MCP_LOG_LEVEL=Debug
```

---

#### WINREG_MCP_WORKER_THREADS

**Description**: Number of worker threads for the server

**Type**: Integer

**Default**: `4`

**Range**: 1-16

**Example**:
```bash
WINREG_MCP_WORKER_THREADS=8
```

---

### Authorization Configuration

#### WINREG_MCP_AUTHORIZATION_LEVEL

**Description**: Global authorization level

**Type**: String

**Valid Values**: `READ_ONLY`, `READ_WRITE`, `ADMIN`

**Default**: `READ_ONLY`

**Example**:
```bash
WINREG_MCP_AUTHORIZATION_LEVEL=READ_WRITE
```

**Security Note**: Use READ_ONLY in production environments.

---

#### WINREG_MCP_ALLOWED_PATHS_FILE

**Description**: Path to allowed paths configuration file

**Type**: File Path

**Default**: None (uses built-in defaults)

**Example**:
```bash
WINREG_MCP_ALLOWED_PATHS_FILE=config/allowed_paths.json
```

---

### Operational Limits

#### WINREG_MCP_MAX_ENUMERATION_DEPTH

**Description**: Maximum depth for key enumeration

**Type**: Integer

**Default**: `3`

**Range**: 1-10

**Example**:
```bash
WINREG_MCP_MAX_ENUMERATION_DEPTH=5
```

---

#### WINREG_MCP_MAX_VALUES_PER_QUERY

**Description**: Maximum number of values returned per query

**Type**: Integer

**Default**: `100`

**Range**: 1-1000

**Example**:
```bash
WINREG_MCP_MAX_VALUES_PER_QUERY=200
```

---

#### WINREG_MCP_MAX_VALUE_SIZE_BYTES

**Description**: Maximum size of a single registry value in bytes

**Type**: Integer

**Default**: `1048576` (1 MB)

**Range**: 1024-10485760 (1 KB - 10 MB)

**Example**:
```bash
WINREG_MCP_MAX_VALUE_SIZE_BYTES=2097152
```

---

#### WINREG_MCP_OPERATION_TIMEOUT_MS

**Description**: Timeout for registry operations in milliseconds

**Type**: Integer

**Default**: `5000` (5 seconds)

**Range**: 100-60000 (0.1s - 60s)

**Example**:
```bash
WINREG_MCP_OPERATION_TIMEOUT_MS=10000
```

---

#### WINREG_MCP_RATE_LIMIT_PER_MINUTE

**Description**: Maximum requests per minute

**Type**: Integer

**Default**: `100`

**Range**: 1-1000

**Example**:
```bash
WINREG_MCP_RATE_LIMIT_PER_MINUTE=200
```

---

## Configuration Files

### Allowed Paths Configuration

**File**: `allowed_paths.json`

**Location**: Specified by `WINREG_MCP_ALLOWED_PATHS_FILE`

**Format**: JSON

**Schema**:

```json
{
  "allowed_roots": [
    {
      "path": "string",
      "access": "read | read_write | admin",
      "max_depth": number
    }
  ],
  "denied_paths": ["string"]
}
```

#### Fields

##### allowed_roots

Array of allowed registry paths.

**Fields**:

- **path** (required): Full registry path (e.g., `HKEY_CURRENT_USER\Software\MyApp`)
- **access** (required): Access level for this path
  - `read`: Read-only access
  - `read_write`: Read and write access
  - `admin`: Full access including key deletion
- **max_depth** (required): Maximum enumeration depth for this path (1-10)

##### denied_paths

Array of explicitly denied registry paths. These paths are blocked regardless of allowed_roots.

**Example**:

```json
{
  "allowed_roots": [
    {
      "path": "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion",
      "access": "read",
      "max_depth": 2
    },
    {
      "path": "HKEY_CURRENT_USER\\Software",
      "access": "read",
      "max_depth": 3
    },
    {
      "path": "HKEY_CURRENT_USER\\Software\\MyApp",
      "access": "read_write",
      "max_depth": 5
    }
  ],
  "denied_paths": [
    "HKEY_LOCAL_MACHINE\\SECURITY",
    "HKEY_LOCAL_MACHINE\\SAM",
    "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Lsa",
    "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services"
  ]
}
```

---

## Default Configuration

### Default Allowed Paths

If no configuration file is provided, the following default paths are used:

```json
{
  "allowed_roots": [
    {
      "path": "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion",
      "access": "read",
      "max_depth": 2
    },
    {
      "path": "HKEY_CURRENT_USER\\Software",
      "access": "read",
      "max_depth": 3
    }
  ],
  "denied_paths": [
    "HKEY_LOCAL_MACHINE\\SECURITY",
    "HKEY_LOCAL_MACHINE\\SAM",
    "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Lsa"
  ]
}
```

### Default Limits

```
Max Enumeration Depth: 3
Max Values Per Query: 100
Max Value Size: 1 MB
Operation Timeout: 5000ms
Rate Limit: 100 requests/minute
```

---

## Configuration Examples

### Development Environment

Permissive configuration for local development:

**Environment**:
```bash
WINREG_MCP_SERVER_NAME=winreg-mcp-dev
WINREG_MCP_LOG_LEVEL=Debug
WINREG_MCP_AUTHORIZATION_LEVEL=READ_WRITE
WINREG_MCP_ALLOWED_PATHS_FILE=config/allowed_paths.dev.json
WINREG_MCP_OPERATION_TIMEOUT_MS=10000
```

**allowed_paths.dev.json**:
```json
{
  "allowed_roots": [
    {
      "path": "HKEY_CURRENT_USER\\Software",
      "access": "read_write",
      "max_depth": 5
    },
    {
      "path": "HKEY_LOCAL_MACHINE\\SOFTWARE",
      "access": "read",
      "max_depth": 3
    }
  ],
  "denied_paths": [
    "HKEY_LOCAL_MACHINE\\SECURITY",
    "HKEY_LOCAL_MACHINE\\SAM"
  ]
}
```

---

### Production Environment

Restrictive configuration for production:

**Environment**:
```bash
WINREG_MCP_SERVER_NAME=winreg-mcp-prod
WINREG_MCP_LOG_LEVEL=Information
WINREG_MCP_AUTHORIZATION_LEVEL=READ_ONLY
WINREG_MCP_ALLOWED_PATHS_FILE=config/allowed_paths.prod.json
WINREG_MCP_MAX_ENUMERATION_DEPTH=2
WINREG_MCP_MAX_VALUES_PER_QUERY=50
WINREG_MCP_OPERATION_TIMEOUT_MS=3000
WINREG_MCP_RATE_LIMIT_PER_MINUTE=50
```

**allowed_paths.prod.json**:
```json
{
  "allowed_roots": [
    {
      "path": "HKEY_CURRENT_USER\\Software\\MyCompany\\MyApp",
      "access": "read",
      "max_depth": 2
    },
    {
      "path": "HKEY_LOCAL_MACHINE\\SOFTWARE\\MyCompany\\MyApp",
      "access": "read",
      "max_depth": 1
    }
  ],
  "denied_paths": [
    "HKEY_LOCAL_MACHINE\\SECURITY",
    "HKEY_LOCAL_MACHINE\\SAM",
    "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Lsa",
    "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services",
    "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion"
  ]
}
```

---

### Testing Environment

Configuration for automated testing:

**Environment**:
```bash
WINREG_MCP_SERVER_NAME=winreg-mcp-test
WINREG_MCP_LOG_LEVEL=Warning
WINREG_MCP_AUTHORIZATION_LEVEL=ADMIN
WINREG_MCP_ALLOWED_PATHS_FILE=config/allowed_paths.test.json
WINREG_MCP_OPERATION_TIMEOUT_MS=1000
WINREG_MCP_RATE_LIMIT_PER_MINUTE=1000
```

**allowed_paths.test.json**:
```json
{
  "allowed_roots": [
    {
      "path": "HKEY_CURRENT_USER\\Software\\WinRegMCPTests",
      "access": "admin",
      "max_depth": 10
    }
  ],
  "denied_paths": []
}
```

---

## Configuration Validation

### Startup Validation

The server validates configuration at startup:

1. **Environment Variables**: Parsed and validated
2. **Configuration File**: Loaded and parsed (JSON syntax check)
3. **Path Format**: Registry paths validated
4. **Numeric Ranges**: Limits checked against min/max values
5. **Fallback**: Invalid configs fall back to safe defaults

### Validation Errors

If configuration is invalid:
- Error logged to stderr
- Server falls back to default configuration
- Warning displayed in logs

**Example Log**:
```
[Warning] Invalid allowed paths file: config/allowed_paths.json
[Warning] Using default configuration
```

---

## Runtime Configuration

### Configuration Reloading

**NOT SUPPORTED**: Configuration is loaded once at startup.

To apply configuration changes:
1. Update configuration files or environment variables
2. Restart the server

### Configuration Changes

Best practices for configuration updates:
1. **Test First**: Validate changes in non-production environment
2. **Version Control**: Keep configuration files in version control
3. **Audit Trail**: Document all configuration changes
4. **Gradual Rollout**: Apply changes incrementally

---

## Troubleshooting

### Configuration Not Loaded

**Problem**: Server not using expected configuration

**Solution**:
1. Check environment variable names (case-sensitive on Linux)
2. Verify file paths are correct
3. Check file permissions (must be readable)
4. Review startup logs for errors

---

### Invalid JSON

**Problem**: `allowed_paths.json` has syntax errors

**Solution**:
1. Validate JSON syntax using online validator
2. Check for missing commas, brackets
3. Ensure proper escaping of backslashes in paths
4. Use `allowed_paths.example.json` as template

**Common Mistakes**:
```json
// ❌ BAD: Single backslash
"path": "HKEY_LOCAL_MACHINE\SOFTWARE"

// ✅ GOOD: Double backslash
"path": "HKEY_LOCAL_MACHINE\\SOFTWARE"
```

---

### Access Denied

**Problem**: Operations failing with `PATH_NOT_ALLOWED`

**Solution**:
1. Check if path is in `allowed_roots`
2. Verify `access` level is sufficient
3. Ensure path is not in `denied_paths`
4. Check `WINREG_MCP_AUTHORIZATION_LEVEL`

---

## Security Considerations

### Sensitive Configuration

**Never commit**:
- Production configuration files with real paths
- Files with overly permissive access

**Do commit**:
- Example/template configuration files
- Development configuration (non-sensitive paths)

### File Permissions

Protect configuration files with appropriate permissions:

**Windows**:
```powershell
icacls config\allowed_paths.json /inheritance:r
icacls config\allowed_paths.json /grant:r "SYSTEM:(R)"
icacls config\allowed_paths.json /grant:r "Administrators:(R)"
```

### Environment Variable Security

For production, use secure secret management:
- Windows Credential Manager
- Azure Key Vault
- AWS Secrets Manager
- Docker Secrets

---

## Configuration Schema

### JSON Schema for allowed_paths.json

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "allowed_roots": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "path": {
            "type": "string",
            "pattern": "^HKEY_[A-Z_]+\\\\.*$"
          },
          "access": {
            "type": "string",
            "enum": ["read", "read_write", "admin"]
          },
          "max_depth": {
            "type": "integer",
            "minimum": 1,
            "maximum": 10
          }
        },
        "required": ["path", "access", "max_depth"]
      }
    },
    "denied_paths": {
      "type": "array",
      "items": {
        "type": "string",
        "pattern": "^HKEY_[A-Z_]+\\\\.*$"
      }
    }
  },
  "required": ["allowed_roots", "denied_paths"]
}
```

---

## References

- [Windows Registry Reference](https://learn.microsoft.com/en-us/windows/win32/sysinfo/registry)
- [JSON Schema Documentation](https://json-schema.org/)
- [Environment Variables Best Practices](https://12factor.net/config)

