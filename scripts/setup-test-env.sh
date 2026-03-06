#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
DOTNET_DIR="$ROOT_DIR/.dotnet"
DO_RESTORE=1

while [[ $# -gt 0 ]]; do
    case "$1" in
        --no-restore)
            DO_RESTORE=0
            shift
            ;;
        *)
            shift
            ;;
    esac
done

if ! command -v dotnet >/dev/null 2>&1; then
    echo "[setup] dotnet не найден, устанавливаю SDK 9.0 локально в $DOTNET_DIR"
    mkdir -p "$DOTNET_DIR"
    bash "$SCRIPT_DIR/dotnet-install.sh" --channel 9.0 --install-dir "$DOTNET_DIR"
    export PATH="$DOTNET_DIR:$PATH"
fi

echo "[setup] dotnet: $(dotnet --version)"

if [ "$DO_RESTORE" -eq 1 ]; then
    echo "[setup] Восстановление зависимостей основных тестовых проектов..."
    dotnet restore "$ROOT_DIR/src/Tests/Project.Repository.Tests/Project.Repository.Tests.csproj"
    dotnet restore "$ROOT_DIR/src/Tests/Project.Service.Tests/Project.Service.Tests.csproj"
    dotnet restore "$ROOT_DIR/src/Tests/Project.Controller.Tests/Project.Controller.Tests.csproj"
    dotnet restore "$ROOT_DIR/src/Tests/Project.E2E.Tests/Project.E2E.Tests.csproj"
    dotnet restore "$ROOT_DIR/src/Project.HttpServer/Project.HttpServer.csproj"
fi
