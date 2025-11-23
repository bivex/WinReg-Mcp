# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-windowsservercore-ltsc2022 AS build
WORKDIR /src

# Copy solution and project files
COPY WinRegMcp.sln .
COPY src/WinRegMcp.Domain/WinRegMcp.Domain.csproj src/WinRegMcp.Domain/
COPY src/WinRegMcp.Infrastructure/WinRegMcp.Infrastructure.csproj src/WinRegMcp.Infrastructure/
COPY src/WinRegMcp.Application/WinRegMcp.Application.csproj src/WinRegMcp.Application/
COPY src/WinRegMcp.Server/WinRegMcp.Server.csproj src/WinRegMcp.Server/

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY src/ src/

# Build and publish
RUN dotnet publish src/WinRegMcp.Server/WinRegMcp.Server.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/windows/servercore:ltsc2022
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Copy configuration
COPY config/ /config/

# Set environment variables
ENV WINREG_MCP_SERVER_NAME=winreg-mcp-server \
    WINREG_MCP_LOG_LEVEL=Information \
    WINREG_MCP_AUTHORIZATION_LEVEL=READ_ONLY \
    WINREG_MCP_ALLOWED_PATHS_FILE=/config/allowed_paths.json

# Healthcheck (if needed for orchestration)
HEALTHCHECK --interval=30s --timeout=5s --retries=3 \
    CMD powershell -Command "if (Get-Process -Name WinRegMcp.Server -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }"

# Run as non-administrator user
USER ContainerUser

ENTRYPOINT ["WinRegMcp.Server.exe"]

