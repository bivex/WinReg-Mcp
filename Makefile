.PHONY: help build test run clean restore publish docker-build

help:
	@echo "Windows Registry MCP Server - Make targets:"
	@echo "  restore       - Restore NuGet packages"
	@echo "  build         - Build the solution"
	@echo "  test          - Run tests"
	@echo "  run           - Run the server"
	@echo "  clean         - Clean build artifacts"
	@echo "  publish       - Publish release build"
	@echo "  docker-build  - Build Docker image"

restore:
	dotnet restore

build: restore
	dotnet build --configuration Release --no-restore

test: build
	dotnet test --configuration Release --no-build --verbosity normal

run:
	dotnet run --project src/WinRegMcp.Server/WinRegMcp.Server.csproj

clean:
	dotnet clean
	rm -rf src/*/bin src/*/obj tests/*/bin tests/*/obj

publish: build
	dotnet publish src/WinRegMcp.Server/WinRegMcp.Server.csproj \
		--configuration Release \
		--runtime win-x64 \
		--self-contained true \
		--output ./publish

docker-build:
	docker build -t winreg-mcp-server:latest .

