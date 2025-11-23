# Windows Registry MCP Server - API Reference

## Tool Schemas (Version 1.0.0)

### read_value

Read a specific registry value from the Windows Registry.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `path` | string | Yes | Full registry path (e.g., "HKEY_CURRENT_USER\\Software\\MyApp") |
| `value_name` | string | Yes | Name of the value to read |

**Returns:**

```json
{
  "name": "Setting1",
  "data": "Value data",
  "type": "String",
  "path": "HKEY_CURRENT_USER\\Software\\MyApp",
  "sizeBytes": 24
}
```

**Errors:**

- `KEY_NOT_FOUND`: Registry key does not exist
- `VALUE_NOT_FOUND`: Registry value does not exist
- `PATH_NOT_ALLOWED`: Access to path is denied
- `LIMIT_EXCEEDED`: Value size exceeds maximum

**Example:**

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "read_value",
    "arguments": {
      "path": "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer",
      "value_name": "ShellState"
    }
  },
  "id": 1
}
```

---

### write_value

Write or update a registry value in the Windows Registry.

**Authorization Required:** READ_WRITE or ADMIN

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `path` | string | Yes | Full registry path |
| `value_name` | string | Yes | Name of the value to write |
| `value_data` | string | Yes | Value data to write |
| `value_type` | string | Yes | Registry type: String, DWord, QWord, Binary, MultiString, ExpandString |

**Returns:**

```json
"Successfully wrote value 'Setting1' to HKEY_CURRENT_USER\\Software\\MyApp"
```

**Errors:**

- `PATH_NOT_ALLOWED`: Write access denied (check authorization level and allowed paths)
- `INVALID_VALUE_TYPE`: Unsupported value type
- `ACCESS_DENIED`: Windows permission error

**Example:**

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "write_value",
    "arguments": {
      "path": "HKEY_CURRENT_USER\\Software\\MyApp",
      "value_name": "Setting1",
      "value_data": "NewValue",
      "value_type": "String"
    }
  },
  "id": 2
}
```

---

### enumerate_keys

List subkeys under a registry path.

**Parameters:**

| Name | Type | Required | Default | Description |
|------|------|----------|---------|-------------|
| `path` | string | Yes | - | Full registry path to enumerate |
| `max_depth` | integer | No | 1 | Maximum enumeration depth (1-3) |

**Returns:**

```json
[
  "SubKey1",
  "SubKey2",
  "SubKey3"
]
```

**Errors:**

- `PATH_NOT_ALLOWED`: Access to path is denied
- `KEY_NOT_FOUND`: Registry key does not exist
- `LIMIT_EXCEEDED`: Too many keys (max 100)

**Example:**

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "enumerate_keys",
    "arguments": {
      "path": "HKEY_CURRENT_USER\\Software",
      "max_depth": 2
    }
  },
  "id": 3
}
```

---

### enumerate_values

List all values in a registry key.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `path` | string | Yes | Full registry path |

**Returns:**

```json
[
  {
    "name": "Setting1",
    "data": "Value1",
    "type": "String",
    "path": "HKEY_CURRENT_USER\\Software\\MyApp",
    "sizeBytes": 12
  },
  {
    "name": "Count",
    "data": "42",
    "type": "DWord",
    "path": "HKEY_CURRENT_USER\\Software\\MyApp",
    "sizeBytes": 4
  }
]
```

**Errors:**

- `PATH_NOT_ALLOWED`: Access to path is denied
- `KEY_NOT_FOUND`: Registry key does not exist
- `LIMIT_EXCEEDED`: Too many values (max 100)

**Example:**

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "enumerate_values",
    "arguments": {
      "path": "HKEY_CURRENT_USER\\Software\\MyApp"
    }
  },
  "id": 4
}
```

---

### get_key_info

Get metadata about a registry key.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `path` | string | Yes | Full registry path |

**Returns:**

```json
{
  "path": "HKEY_CURRENT_USER\\Software\\MyApp",
  "name": "MyApp",
  "subKeyCount": 3,
  "valueCount": 5,
  "subKeyNames": ["Config", "Data", "Logs"]
}
```

**Errors:**

- `PATH_NOT_ALLOWED`: Access to path is denied
- `KEY_NOT_FOUND`: Registry key does not exist

**Example:**

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "get_key_info",
    "arguments": {
      "path": "HKEY_CURRENT_USER\\Software\\MyApp"
    }
  },
  "id": 5
}
```

---

### delete_value

Delete a registry value from the Windows Registry.

**Authorization Required:** READ_WRITE or ADMIN

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `path` | string | Yes | Full registry path |
| `value_name` | string | Yes | Name of the value to delete |

**Returns:**

```json
"Successfully deleted value 'Setting1' from HKEY_CURRENT_USER\\Software\\MyApp"
```

**Errors:**

- `PATH_NOT_ALLOWED`: Delete access denied
- `VALUE_NOT_FOUND`: Registry value does not exist

**Example:**

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "delete_value",
    "arguments": {
      "path": "HKEY_CURRENT_USER\\Software\\MyApp",
      "value_name": "OldSetting"
    }
  },
  "id": 6
}
```

---

### delete_key

Delete a registry key and all its subkeys.

**Authorization Required:** ADMIN

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `path` | string | Yes | Full registry path to delete |

**Returns:**

```json
"Successfully deleted key: HKEY_CURRENT_USER\\Software\\MyApp\\TempData"
```

**Errors:**

- `PATH_NOT_ALLOWED`: Admin access required
- `KEY_NOT_FOUND`: Registry key does not exist

**Warning:** This operation is irreversible and deletes all subkeys and values.

**Example:**

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "delete_key",
    "arguments": {
      "path": "HKEY_CURRENT_USER\\Software\\MyApp\\TempData"
    }
  },
  "id": 7
}
```

---

## Prompts

### safe_registry_query

Template for constructing safe registry queries with proper path validation.

**Parameters:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `path_pattern` | string | Yes | The registry path pattern to query |

**Returns:** Guidance text for safe registry querying

---

### registry_troubleshooting

Guide for troubleshooting registry access issues.

**Parameters:** None

**Returns:** Troubleshooting guide with common issues and solutions

---

## Registry Value Types

### String (REG_SZ)

UTF-16 string value.

**Example:** `"Hello World"`

---

### ExpandString (REG_EXPAND_SZ)

String with environment variable references that get expanded.

**Example:** `"%USERPROFILE%\\Documents"`

---

### Binary (REG_BINARY)

Binary data encoded as base64 string.

**Example:** `"SGVsbG8gV29ybGQh"` (base64 of "Hello World!")

---

### DWord (REG_DWORD)

32-bit unsigned integer.

**Example:** `42`

---

### QWord (REG_QWORD)

64-bit unsigned integer.

**Example:** `9223372036854775807`

---

### MultiString (REG_MULTI_SZ)

Array of strings separated by newlines.

**Example:** `"Line1\nLine2\nLine3"`

---

## Error Codes

### KEY_NOT_FOUND

Registry key does not exist at the specified path.

**HTTP Status Equivalent:** 404 Not Found

---

### VALUE_NOT_FOUND

Registry value does not exist in the specified key.

**HTTP Status Equivalent:** 404 Not Found

---

### PATH_NOT_ALLOWED

Access to the registry path is denied by authorization rules.

**HTTP Status Equivalent:** 403 Forbidden

**Common Causes:**
- Path not in allowed list
- Insufficient authorization level
- Path explicitly denied

---

### LIMIT_EXCEEDED

Operation exceeded configured limits.

**HTTP Status Equivalent:** 429 Too Many Requests

**Limit Types:**
- Enumeration depth
- Values per query
- Value size

---

### INVALID_VALUE_TYPE

Registry value type is invalid or mismatched.

**HTTP Status Equivalent:** 400 Bad Request

---

### ACCESS_DENIED

Windows denied access to the registry path.

**HTTP Status Equivalent:** 403 Forbidden

**Common Causes:**
- Insufficient Windows permissions
- Path requires administrator privileges

---

## Rate Limiting

Default rate limit: 100 requests per minute

Configure via: `WINREG_MCP_RATE_LIMIT_PER_MINUTE`

When exceeded, operations will return an error.

---

## Timeouts

Default operation timeout: 5000ms (5 seconds)

Configure via: `WINREG_MCP_OPERATION_TIMEOUT_MS`

Long-running operations (large enumerations) may hit this timeout.

---

## Limits

### Enumeration Depth

- Default max: 3 levels
- Configure via: `WINREG_MCP_MAX_ENUMERATION_DEPTH`
- Can be further restricted per path in allowed_paths.json

### Values Per Query

- Default max: 100 values
- Configure via: `WINREG_MCP_MAX_VALUES_PER_QUERY`

### Value Size

- Default max: 1 MB (1,048,576 bytes)
- Configure via: `WINREG_MCP_MAX_VALUE_SIZE_BYTES`

---

## Authorization Levels

### READ_ONLY

Can only read registry values and enumerate keys.

**Allowed Operations:**
- read_value
- enumerate_keys
- enumerate_values
- get_key_info

---

### READ_WRITE

Can read and write registry values (but not delete keys).

**Allowed Operations:**
- All READ_ONLY operations
- write_value
- delete_value

---

### ADMIN

Full access including key deletion.

**Allowed Operations:**
- All READ_WRITE operations
- delete_key

---

## Best Practices

### Path Format

Always use full registry paths:
- ✅ `HKEY_CURRENT_USER\\Software\\MyApp`
- ✅ `HKCU\\Software\\MyApp` (abbreviated form)
- ❌ `Software\\MyApp` (missing hive)

### Enumeration Depth

Start with shallow depth and increase if needed:
- Depth 1: Immediate children only
- Depth 2: Children and grandchildren
- Depth 3: Maximum (default)

### Error Handling

Always check for error responses:
```javascript
if (response.error) {
  console.error(`Error: ${response.error.code} - ${response.error.message}`);
  // Handle specific error codes
}
```

### Value Types

Always specify the correct value type when writing:
- String data → "String"
- Numbers → "DWord" (32-bit) or "QWord" (64-bit)
- Binary data → "Binary" (base64 encoded)

---

## Versioning

Current API Version: **1.0.0**

Version format: `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes

Deprecation period: 3 months before removal of deprecated features.

