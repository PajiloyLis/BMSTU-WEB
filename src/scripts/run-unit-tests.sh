#!/bin/bash
# =============================================================================
# Скрипт для запуска тестов проекта
# Использование:
#   ./run-unit-tests.sh              — запустить все тесты
#   ./run-unit-tests.sh repo         — только тесты репозиториев
#   ./run-unit-tests.sh service      — только тесты сервисов
#   ./run-unit-tests.sh --filter "FullyQualifiedName~Company"  — фильтрация тестов
#   ./run-unit-tests.sh --verbose    — подробный вывод
#   ./run-unit-tests.sh --coverage   — с отчётом покрытия
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC_DIR="$(dirname "$SCRIPT_DIR")/src"
SOLUTION="$SRC_DIR/Project.sln"
REPO_TESTS="$SRC_DIR/Tests/Project.Repository.Tests/Project.Repository.Tests.csproj"
SERVICE_TESTS="$SRC_DIR/Tests/Project.Service.Tests/Project.Service.Tests.csproj"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

TARGET=""
FILTER=""
VERBOSE=""
COVERAGE=""
EXTRA_ARGS=""

# Парсинг аргументов
while [[ $# -gt 0 ]]; do
    case $1 in
        repo|repository)
            TARGET="repo"
            shift
            ;;
        service|services)
            TARGET="service"
            shift
            ;;
        --filter)
            FILTER="--filter $2"
            shift 2
            ;;
        --verbose|-v)
            VERBOSE="--verbosity detailed"
            shift
            ;;
        --coverage)
            COVERAGE="--collect:\"XPlat Code Coverage\""
            shift
            ;;
        --help|-h)
            echo -e "${CYAN}Использование:${NC}"
            echo "  ./run-unit-tests.sh              — запустить все тесты"
            echo "  ./run-unit-tests.sh repo         — только тесты репозиториев"
            echo "  ./run-unit-tests.sh service      — только тесты сервисов"
            echo "  ./run-unit-tests.sh --filter \"FullyQualifiedName~Company\"  — фильтрация"
            echo "  ./run-unit-tests.sh --verbose    — подробный вывод"
            echo "  ./run-unit-tests.sh --coverage   — с отчётом покрытия"
            echo ""
            echo -e "${CYAN}Примеры фильтров:${NC}"
            echo "  --filter \"FullyQualifiedName~CompanyRepository\"   — тесты CompanyRepository"
            echo "  --filter \"FullyQualifiedName~Add\"                 — все тесты Add*"
            echo "  --filter \"ClassName=CompanyServiceTests\"          — тесты класса"
            exit 0
            ;;
        *)
            EXTRA_ARGS="$EXTRA_ARGS $1"
            shift
            ;;
    esac
done

echo -e "${CYAN}=============================================${NC}"
echo -e "${CYAN}       Запуск тестов проекта                 ${NC}"
echo -e "${CYAN}=============================================${NC}"
echo ""

run_tests() {
    local project="$1"
    local name="$2"

    echo -e "${YELLOW}▶ Запуск: ${name}${NC}"
    echo -e "${YELLOW}  Проект: ${project}${NC}"
    echo ""

    if dotnet test "$project" \
        --no-restore \
        --logger "console;verbosity=normal" \
        $FILTER $VERBOSE $COVERAGE $EXTRA_ARGS; then
        echo -e "${GREEN}✓ ${name}: PASSED${NC}"
        echo ""
        return 0
    else
        echo -e "${RED}✗ ${name}: FAILED${NC}"
        echo ""
        return 1
    fi
}

# Сначала собираем решение
echo -e "${YELLOW}▶ Сборка решения...${NC}"
if dotnet build "$SOLUTION" --no-restore -q 2>/dev/null; then
    echo -e "${GREEN}✓ Сборка успешна${NC}"
else
    echo -e "${YELLOW}▶ Восстановление пакетов и сборка...${NC}"
    dotnet restore "$SOLUTION"
    dotnet build "$SOLUTION" -q
    echo -e "${GREEN}✓ Сборка успешна${NC}"
fi
echo ""

FAILED=0

case $TARGET in
    repo)
        run_tests "$REPO_TESTS" "Тесты репозиториев" || FAILED=1
        ;;
    service)
        run_tests "$SERVICE_TESTS" "Тесты сервисов" || FAILED=1
        ;;
    *)
        run_tests "$REPO_TESTS" "Тесты репозиториев" || FAILED=1
        run_tests "$SERVICE_TESTS" "Тесты сервисов" || FAILED=1
        ;;
esac

echo -e "${CYAN}=============================================${NC}"
if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}  ✓ Все тесты прошли успешно!${NC}"
else
    echo -e "${RED}  ✗ Некоторые тесты не прошли!${NC}"
fi
echo -e "${CYAN}=============================================${NC}"

exit $FAILED

