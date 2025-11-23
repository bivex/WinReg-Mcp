# Настройка и тестирование MCP сервера в Cursor

## Текущая ситуация

MCP инструменты (`mcp_winreg_read_value`, `mcp_winreg_enumerate_keys`) возвращают ошибки, что указывает на то, что сервер не запущен или не подключен к Cursor.

## Шаги для настройки

### 1. Сборка проекта

#### Вариант A: Простая сборка Release

```powershell
# Перейдите в директорию проекта
cd C:\Users\Admin\Desktop\Dev\winregcsharp-mcp

# Соберите проект в режиме Release
dotnet build --configuration Release
```

#### Вариант B: Публикация самодостаточного приложения (рекомендуется)

```powershell
# Перейдите в директорию проекта
cd C:\Users\Admin\Desktop\Dev\winregcsharp-mcp

# Опубликуйте приложение для Windows x64
dotnet publish src\WinRegMcp.Server\WinRegMcp.Server.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  --output src\WinRegMcp.Server\bin\Release\net8.0\win-x64\publish
```

**Результат:**
- **Вариант A:** `src\WinRegMcp.Server\bin\Release\net8.0\WinRegMcp.Server.dll`
- **Вариант B:** `src\WinRegMcp.Server\bin\Release\net8.0\win-x64\WinRegMcp.Server.dll` (или `.exe` если self-contained)

### 2. Настройка переменных окружения

```powershell
$env:WINREG_MCP_AUTHORIZATION_LEVEL="READ_ONLY"
$env:WINREG_MCP_ALLOWED_PATHS_FILE="config/allowed_paths.json"
$env:WINREG_MCP_LOG_LEVEL="Information"
```

### 3. Настройка MCP сервера в Cursor

Для подключения MCP сервера к Cursor необходимо:

1. Открыть настройки Cursor (Ctrl+,)
2. Найти раздел "MCP Servers" или "Model Context Protocol"
3. Добавить новый сервер со следующей конфигурацией:

```json
{
  "winreg-mcp-server": {
    "command": "dotnet",
    "args": [
      "run",
      "--project",
      "C:\\Users\\Admin\\Desktop\\Dev\\winregcsharp-mcp\\src\\WinRegMcp.Server\\WinRegMcp.Server.csproj"
    ],
    "env": {
      "WINREG_MCP_AUTHORIZATION_LEVEL": "READ_ONLY",
      "WINREG_MCP_ALLOWED_PATHS_FILE": "config/allowed_paths.json",
      "WINREG_MCP_LOG_LEVEL": "Information"
    }
  }
}
```

**Вариант 2: Запуск собранного Release билда (рекомендуется)**

После сборки проекта в Release режиме используйте скомпилированный `.dll` файл:

```json
{
  "winreg": {
    "command": "dotnet",
    "args": [
      "C:\\Users\\Admin\\Desktop\\Dev\\winregcsharp-mcp\\src\\WinRegMcp.Server\\bin\\Release\\net8.0\\win-x64\\WinRegMcp.Server.dll"
    ],
    "cwd": "C:\\Users\\Admin\\Desktop\\Dev\\winregcsharp-mcp",
    "env": {
      "WINREG_MCP_SERVER_NAME": "winreg-mcp-server",
      "WINREG_MCP_LOG_LEVEL": "Information",
      "WINREG_MCP_AUTHORIZATION_LEVEL": "READ_ONLY",
      "WINREG_MCP_ALLOWED_PATHS_FILE": "C:\\Users\\Admin\\Desktop\\Dev\\winregcsharp-mcp\\config\\allowed_paths.json",
      "WINREG_MCP_MAX_ENUMERATION_DEPTH": "3",
      "WINREG_MCP_MAX_VALUES_PER_QUERY": "100",
      "WINREG_MCP_OPERATION_TIMEOUT_MS": "5000"
    },
    "disabled": false,
    "autoApprove": [
      "read_value",
      "enumerate_keys",
      "enumerate_values",
      "get_key_info"
    ]
  }
}
```

**Примечание:** Путь может отличаться в зависимости от метода сборки:
- Простая сборка: `bin\\Release\\net8.0\\WinRegMcp.Server.dll`
- После publish: `bin\\Release\\net8.0\\win-x64\\WinRegMcp.Server.dll`

**Вариант 3: Запуск .exe файла (если используется self-contained publish)**

```json
{
  "winreg": {
    "command": "C:\\Users\\Admin\\Desktop\\Dev\\winregcsharp-mcp\\src\\WinRegMcp.Server\\bin\\Release\\net8.0\\win-x64\\publish\\WinRegMcp.Server.exe",
    "cwd": "C:\\Users\\Admin\\Desktop\\Dev\\winregcsharp-mcp",
    "env": {
      "WINREG_MCP_AUTHORIZATION_LEVEL": "READ_ONLY",
      "WINREG_MCP_ALLOWED_PATHS_FILE": "C:\\Users\\Admin\\Desktop\\Dev\\winregcsharp-mcp\\config\\allowed_paths.json"
    }
  }
}
```

### 4. Проверка сборки

Перед настройкой в Cursor убедитесь, что файлы существуют:

```powershell
# Проверьте существование DLL файла
Test-Path "C:\Users\Admin\Desktop\Dev\winregcsharp-mcp\src\WinRegMcp.Server\bin\Release\net8.0\win-x64\WinRegMcp.Server.dll"

# Или проверьте альтернативный путь
Test-Path "C:\Users\Admin\Desktop\Dev\winregcsharp-mcp\src\WinRegMcp.Server\bin\Release\net8.0\WinRegMcp.Server.dll"

# Проверьте конфигурационный файл
Test-Path "C:\Users\Admin\Desktop\Dev\winregcsharp-mcp\config\allowed_paths.json"
```

Если файлы не найдены, вернитесь к шагу 1 и выполните сборку.

### 5. Перезапуск Cursor

После настройки перезапустите Cursor, чтобы сервер подключился.

## Преимущества Release билда

**Использование Release билда вместо `dotnet run`:**
- ⚡ **Быстрее запускается** - нет компиляции при каждом старте
- 🔧 **Оптимизирован** - включены оптимизации компилятора
- 📦 **Меньше ресурсов** - не требуется загрузка исходников
- ✅ **Стабильнее** - предкомпилированный код более надежен

## Тестирование после настройки

После правильной настройки и перезапуска Cursor, выполните три проверки:

### Проверка 1: Чтение версии Windows
```json
{
  "tool": "read_value",
  "arguments": {
    "path": "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion",
    "value_name": "ProductName"
  }
}
```

### Проверка 2: Тест безопасности
```json
{
  "tool": "read_value",
  "arguments": {
    "path": "HKEY_LOCAL_MACHINE\\SECURITY\\SAM",
    "value_name": "test"
  }
}
```
Ожидается ошибка `PATH_NOT_ALLOWED`.

### Проверка 3: Перечисление ключей
```json
{
  "tool": "enumerate_keys",
  "arguments": {
    "path": "HKEY_CURRENT_USER\\Software\\Microsoft",
    "max_depth": 1
  }
}
```

## Проверка статуса сервера

Если сервер запущен правильно, вы должны увидеть в логах Cursor (или в консоли, если запускаете вручную):

```
Starting Windows Registry MCP Server v1.0.0 (Authorization: ReadOnly)
```

## Альтернативный способ тестирования

Если MCP сервер не подключается к Cursor, можно протестировать его напрямую через stdio:

### Способ 1: Запуск через dotnet run (для разработки)

```powershell
dotnet run --project src\WinRegMcp.Server\WinRegMcp.Server.csproj
```

### Способ 2: Запуск Release билда напрямую (рекомендуется)

```powershell
# Установите переменные окружения
$env:WINREG_MCP_AUTHORIZATION_LEVEL="READ_ONLY"
$env:WINREG_MCP_ALLOWED_PATHS_FILE="C:\Users\Admin\Desktop\Dev\winregcsharp-mcp\config\allowed_paths.json"
$env:WINREG_MCP_LOG_LEVEL="Information"

# Запустите скомпилированный DLL
dotnet "C:\Users\Admin\Desktop\Dev\winregcsharp-mcp\src\WinRegMcp.Server\bin\Release\net8.0\win-x64\WinRegMcp.Server.dll"
```

### Тестовый запрос

Сервер будет ожидать JSON-RPC запросы через stdin/stdout. Отправьте тестовый запрос:

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "read_value",
    "arguments": {
      "path": "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion",
      "value_name": "ProductName"
    }
  },
  "id": 1
}
```

## Конфигурация обновлена

Файл `config/allowed_paths.json` был обновлен и включает путь для тестирования:
- `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion` - добавлен для теста чтения версии Windows

## Troubleshooting для Release билдов

### Проблема: "Could not find file WinRegMcp.Server.dll"

**Решение:**
```powershell
# Пересоберите проект
dotnet clean
dotnet build --configuration Release

# Или выполните publish
dotnet publish src\WinRegMcp.Server\WinRegMcp.Server.csproj -c Release -r win-x64
```

### Проблема: Сервер запускается, но не отвечает

**Проверьте:**
1. Правильность путей в конфигурации:
   ```powershell
   # Проверьте путь к DLL
   Get-Item "C:\Users\Admin\Desktop\Dev\winregcsharp-mcp\src\WinRegMcp.Server\bin\Release\net8.0\win-x64\WinRegMcp.Server.dll"
   
   # Проверьте путь к конфигурации
   Get-Content "C:\Users\Admin\Desktop\Dev\winregcsharp-mcp\config\allowed_paths.json"
   ```

2. Логи Cursor:
   - Откройте: `Help` → `Toggle Developer Tools` → вкладка `Console`
   - Ищите сообщения от `winreg` или `MCP`

3. Переменные окружения в `mcp.json` правильно установлены

### Проблема: "Access Denied" или "PATH_NOT_ALLOWED"

**Это нормально!** Сервер работает правильно и блокирует доступ к неразрешенным путям.

**Решение:**
- Добавьте нужные пути в `config/allowed_paths.json`
- Или измените уровень авторизации на `ADMIN` (только для тестирования!)

### Быстрая пересборка и перезапуск

```powershell
# Пересоберите проект
dotnet build src\WinRegMcp.Server\WinRegMcp.Server.csproj -c Release

# Перезапустите Cursor (или используйте команду Reload Window)
# Ctrl+Shift+P → "Developer: Reload Window"
```

