# Windows Registry MCP Server - Security Model

## Overview

The Windows Registry MCP Server is designed with security as a primary concern. This document describes the security model, threat considerations, and mitigations in place.

## Security Principles

### Defense in Depth

Multiple layers of security controls:
1. **Authorization Layer**: Path-based access control
2. **Limit Layer**: Rate limiting and size restrictions
3. **Audit Layer**: Comprehensive logging
4. **Validation Layer**: Input validation and sanitization

### Least Privilege

- Default access level: READ_ONLY
- Minimal default allowed paths
- Explicit opt-in for write and admin operations
- Per-path access granularity

### Fail Secure

- Unknown paths are denied by default
- Invalid configurations fall back to safe defaults
- Errors do not leak sensitive information

## Threat Model

### Threats Addressed

#### 1. Data Exfiltration

**Threat**: Malicious actor attempts to extract sensitive Windows Registry data.

**Mitigations**:
- Path allow-list restricts accessible registry locations
- Explicitly denied paths for security-critical hives (SAM, SECURITY, LSA)
- Enumeration depth limits (max 3 levels)
- Value count limits (max 100 per query)
- Value size limits (max 1 MB)

**Example Denied Paths**:
```
HKEY_LOCAL_MACHINE\SECURITY
HKEY_LOCAL_MACHINE\SAM
HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa
```

#### 2. Unauthorized Modification

**Threat**: Malicious actor attempts to modify critical system settings.

**Mitigations**:
- READ_ONLY default authorization level
- Write operations require explicit READ_WRITE level
- Per-path access control (some paths may be read-only even with READ_WRITE)
- Authorization validated before every operation
- Audit logging of all write operations

#### 3. Privilege Escalation

**Threat**: Malicious actor attempts to elevate privileges or delete critical keys.

**Mitigations**:
- Admin operations (delete_key) require explicit ADMIN level
- Cannot modify authorization level at runtime
- No path traversal or injection vulnerabilities (paths are validated)

#### 4. Denial of Service

**Threat**: Malicious actor attempts to overwhelm the server or consume excessive resources.

**Mitigations**:
- Operation timeouts (default 5 seconds)
- Cancellation token support
- Rate limiting (100 requests/minute)
- Enumeration limits
- Value size limits
- Concurrent operation tracking

### Threats NOT Addressed

These threats require OS-level or network-level controls:

#### 1. Process Memory Inspection

**Threat**: Attacker with local access inspects process memory.

**Mitigation**: Use Windows security features (process isolation, DEP, ASLR)

#### 2. Physical Access

**Threat**: Attacker with physical access to the machine.

**Mitigation**: Use Windows security features (BitLocker, secure boot)

#### 3. Malicious MCP Host

**Threat**: The MCP host (LLM client) is compromised.

**Note**: The trust boundary is at the MCP protocol. The server trusts the host to enforce its own security policies.

## Authorization Model

### Access Levels

```
READ_ONLY < READ_WRITE < ADMIN
```

#### READ_ONLY

Default level. Can only read from allowed paths.

**Allowed Operations**:
- `read_value`
- `enumerate_keys`
- `enumerate_values`
- `get_key_info`

**Use Case**: Production environments, untrusted clients

#### READ_WRITE

Can read and write to allowed paths.

**Additional Operations**:
- `write_value`
- `delete_value`

**Use Case**: Application configuration, user data management

#### ADMIN

Full access including destructive operations.

**Additional Operations**:
- `delete_key`

**Use Case**: System administration, testing environments

### Path-Based Access Control

#### Allowed Paths Configuration

```json
{
  "allowed_roots": [
    {
      "path": "HKEY_CURRENT_USER\\Software\\MyApp",
      "access": "read_write",
      "max_depth": 5
    }
  ]
}
```

**Fields**:
- `path`: Root path that is accessible
- `access`: Access level for this path (read, read_write, admin)
- `max_depth`: Maximum enumeration depth for this path

#### Denied Paths Configuration

```json
{
  "denied_paths": [
    "HKEY_LOCAL_MACHINE\\SECURITY",
    "HKEY_LOCAL_MACHINE\\SAM"
  ]
}
```

**Priority**: Denied paths take precedence over allowed paths.

### Authorization Flow

```
1. Parse registry path
   ↓
2. Check if path is explicitly denied
   → If YES: Deny access (PATH_NOT_ALLOWED)
   ↓
3. Find matching allowed root
   → If NO MATCH: Deny access (PATH_NOT_ALLOWED)
   ↓
4. Check if allowed root grants sufficient access level
   → If INSUFFICIENT: Deny access (PATH_NOT_ALLOWED)
   ↓
5. Validate user's authorization level
   → If INSUFFICIENT: Deny access (PATH_NOT_ALLOWED)
   ↓
6. Allow operation
```

## Operational Limits

### Rate Limiting

**Default**: 100 requests per minute

**Configuration**: `WINREG_MCP_RATE_LIMIT_PER_MINUTE`

**Behavior**: Requests exceeding limit are rejected with error

### Enumeration Limits

**Max Depth**: 3 levels (default)

**Max Values**: 100 per query

**Configuration**:
- `WINREG_MCP_MAX_ENUMERATION_DEPTH`
- `WINREG_MCP_MAX_VALUES_PER_QUERY`

**Behavior**: Operations exceeding limits return `LIMIT_EXCEEDED` error

### Value Size Limits

**Max Size**: 1 MB (1,048,576 bytes)

**Configuration**: `WINREG_MCP_MAX_VALUE_SIZE_BYTES`

**Behavior**: Reading values exceeding limit returns `LIMIT_EXCEEDED` error

### Operation Timeouts

**Default**: 5000ms (5 seconds)

**Configuration**: `WINREG_MCP_OPERATION_TIMEOUT_MS`

**Behavior**: Long-running operations are cancelled and return timeout error

## Audit and Logging

### Audit Trail

Every operation is logged with:
- **Correlation ID**: Unique identifier for request tracing
- **Operation Type**: read_value, write_value, etc.
- **Registry Path**: Accessed path (not value data)
- **Result**: Success or failure
- **Error Details**: If failed, error code and message
- **Timestamp**: UTC timestamp
- **User/Workspace**: If available from request context

### Log Format

```json
{
  "timestamp": "2024-01-01T12:00:00Z",
  "level": "Information",
  "correlationId": "req-1704110400000-ABCD1234",
  "operation": "write_value",
  "path": "HKEY_CURRENT_USER\\Software\\MyApp",
  "valueName": "Setting1",
  "result": "success"
}
```

### Sensitive Data Handling

**Never Logged**:
- Registry value data (could contain passwords, tokens)
- Full stack traces in production
- Internal system paths

**Always Logged**:
- Operation type
- Registry path
- Success/failure
- Error codes
- Correlation IDs

### Log Storage

**Recommendation**: Use centralized log aggregation
- Forward logs to SIEM (Security Information and Event Management)
- Retain logs for compliance requirements
- Monitor for suspicious patterns

## Data Protection

### Data in Transit

**MCP Protocol**: Uses stdio transport (local process communication)

**No Network Exposure**: Server does not listen on network ports by default

**For HTTP Transport**: Use TLS 1.3+ with strong cipher suites

### Data at Rest

**Registry Values**: Stored by Windows Registry (OS responsibility)

**Configuration Files**: Should be protected with filesystem permissions

**Recommendation**: Use Windows NTFS permissions to restrict access to:
- Server executable
- Configuration files
- Log files

### Secrets Management

**Never Store in Code**: All secrets in environment variables or secure stores

**Configuration Files**: No secrets in `allowed_paths.json`

**Environment Variables**: Use Windows Credential Manager or Azure Key Vault for production

## Vulnerability Prevention

### Input Validation

All inputs are validated:
- Registry paths parsed and normalized
- Value types validated against enum
- Numeric parameters range-checked
- String parameters length-limited

**Protection Against**:
- Path traversal
- Injection attacks
- Buffer overflows
- Integer overflows

### Output Sanitization

Registry value data is sanitized before returning:
- Binary data base64-encoded
- Multi-strings properly escaped
- No raw binary in JSON responses

### Error Handling

**Secure Error Messages**:
- No stack traces in error responses
- No internal system paths
- Generic error messages for security errors

**Example**:
- ❌ `"Access denied to C:\Windows\System32\config\SAM"`
- ✅ `"Access denied: Path is in the denied list"`

## Deployment Security

### Least Privilege Execution

**Windows Service Account**: Run as dedicated service account with minimal permissions

**Required Permissions**:
- Read access to allowed registry paths
- Write access only if using READ_WRITE level
- No administrator privileges unless using ADMIN level

### Container Security

**Base Image**: Use minimal Windows Server Core image

**User Context**: Run as non-administrator user in container

**Resource Limits**:
- Memory: 256 MB
- CPU: 0.5 cores
- No network access (except for logging/metrics export)

### File System Permissions

```
Server Executable: Read + Execute only
Configuration Files: Read only
Log Directory: Write only
```

## Compliance Considerations

### GDPR

If registry contains personal data:
- Log data retention policies
- Right to erasure implementation
- Data minimization in logs

### SOC 2

- Comprehensive audit logging
- Access control documentation
- Incident response procedures

### PCI DSS

If registry contains payment data:
- Strong access controls
- Encryption for sensitive data
- Regular security assessments

## Security Recommendations

### Production Deployment

1. **Use READ_ONLY by default**: Only enable write access when necessary
2. **Minimal Allowed Paths**: Only allow paths required for your use case
3. **Enable Audit Logging**: Forward logs to centralized SIEM
4. **Monitor for Anomalies**: Alert on unusual patterns (high error rates, denied access attempts)
5. **Regular Reviews**: Periodically review and update allowed paths

### Development/Testing

1. **Separate Configuration**: Use different allowed paths for dev/test/prod
2. **Test Authorization**: Verify access controls work as expected
3. **Security Testing**: Test with malicious inputs and paths

### Configuration Security

```json
// ❌ BAD: Too permissive
{
  "allowed_roots": [
    {
      "path": "HKEY_LOCAL_MACHINE",
      "access": "admin",
      "max_depth": 10
    }
  ]
}

// ✅ GOOD: Specific and limited
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

## Incident Response

### Detection

Monitor for:
- High volume of `PATH_NOT_ALLOWED` errors
- Repeated attempts to access denied paths
- Unusual enumeration patterns
- Timeout errors (possible DoS)

### Response

1. **Isolate**: Stop the server if compromised
2. **Investigate**: Review audit logs for suspicious activity
3. **Remediate**: Update configuration, patch vulnerabilities
4. **Recovery**: Restore from known good configuration

### Post-Incident

1. **Root Cause Analysis**: Determine how breach occurred
2. **Update Controls**: Strengthen security controls
3. **Update Documentation**: Document lessons learned

## Security Updates

### Vulnerability Disclosure

Report security vulnerabilities to: [your-security-email]

### Update Process

1. Security patches released as soon as possible
2. Critical vulnerabilities announced via security advisory
3. Update instructions provided with each release

## Security Checklist

Before deploying to production:

- [ ] Reviewed and minimized allowed paths
- [ ] Set appropriate authorization level (READ_ONLY recommended)
- [ ] Configured audit logging
- [ ] Tested access controls
- [ ] Set appropriate rate limits
- [ ] Configured operation timeouts
- [ ] Reviewed denied paths list
- [ ] Secured configuration files
- [ ] Implemented log monitoring
- [ ] Documented security procedures

## References

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [CWE - Common Weakness Enumeration](https://cwe.mitre.org/)
- [Windows Registry Security](https://learn.microsoft.com/en-us/windows/win32/sysinfo/registry-security)
- [MCP Security Best Practices](https://modelcontextprotocol.io/docs/security)

