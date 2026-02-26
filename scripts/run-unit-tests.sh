#!/bin/bash
# =============================================================================
# Скрипт для запуска тестов проекта
#
# Использование:
#   ./run-unit-tests.sh                        — запустить все тесты
#   ./run-unit-tests.sh repo                   — только тесты репозиториев
#   ./run-unit-tests.sh service                — только тесты сервисов
#   ./run-unit-tests.sh controller             — только тесты контроллеров
#   ./run-unit-tests.sh company                — тесты Company (repo + service)
#   ./run-unit-tests.sh controller company     — только тесты CompanyController
#   ./run-unit-tests.sh repo employee          — тесты EmployeeRepository
#   ./run-unit-tests.sh service education      — тесты EducationService
#   ./run-unit-tests.sh --filter "FullyQualifiedName~Add"  — произвольный фильтр
#   ./run-unit-tests.sh --verbose              — подробный вывод
#   ./run-unit-tests.sh --coverage             — с отчётом покрытия
#   ./run-unit-tests.sh --allure               — сгенерировать Allure-отчёт
#   ./run-unit-tests.sh --allure-open          — сгенерировать и открыть Allure-отчёт
#   ./run-unit-tests.sh --list                 — список доступных сущностей
# =============================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC_DIR="$(dirname "$SCRIPT_DIR")/src"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
REPO_TESTS="$SRC_DIR/Tests/Project.Repository.Tests/Project.Repository.Tests.csproj"
SERVICE_TESTS="$SRC_DIR/Tests/Project.Service.Tests/Project.Service.Tests.csproj"
CONTROLLER_TESTS="$SRC_DIR/Tests/Project.Controller.Tests/Project.Controller.Tests.csproj"
ALLURE_BIN="$ROOT_DIR/tools/allure-2.36.0/bin/allure"
ALLURE_RESULTS="$ROOT_DIR/allure-results"
ALLURE_REPORT="$ROOT_DIR/allure-report"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

# Доступные сущности для фильтрации
ENTITIES="company employee education position positionhistory post posthistory score"

TARGET=""
ENTITY_LIST=""
FILTER=""
VERBOSE=""
COVERAGE=""
ALLURE=""
ALLURE_OPEN=""
EXTRA_ARGS=""

is_entity() {
    local arg
    arg=$(echo "$1" | tr '[:upper:]' '[:lower:]')
    for e in $ENTITIES; do
        if [ "$arg" = "$e" ]; then
            return 0
        fi
    done
    return 1
}

entity_to_pascal() {
    case "$(echo "$1" | tr '[:upper:]' '[:lower:]')" in
        company)          echo "Company" ;;
        employee)         echo "Employee" ;;
        education)        echo "Education" ;;
        position)         echo "Position" ;;
        positionhistory)  echo "PositionHistory" ;;
        post)             echo "Post" ;;
        posthistory)      echo "PostHistory" ;;
        score)            echo "Score" ;;
        *)                echo "$1" ;;
    esac
}

# Построение фильтра для сущности с учётом TARGET
build_entity_filter() {
    local entity_pascal="$1"
    local target="$2"

    case "$target" in
        repo)
            echo "FullyQualifiedName~${entity_pascal}Repository"
            ;;
        service)
            echo "FullyQualifiedName~${entity_pascal}Service"
            ;;
        controller)
            echo "FullyQualifiedName~${entity_pascal}Controller"
            ;;
        *)
            echo "FullyQualifiedName~${entity_pascal}"
            ;;
    esac
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        repo|repository)
            TARGET="repo"
            shift
            ;;
        service|services)
            TARGET="service"
            shift
            ;;
        controller|controllers)
            TARGET="controller"
            shift
            ;;
        --filter)
            FILTER="$2"
            shift 2
            ;;
        --verbose|-v)
            VERBOSE="--verbosity detailed"
            shift
            ;;
        --coverage)
            COVERAGE='--collect:"XPlat Code Coverage"'
            shift
            ;;
        --allure)
            ALLURE=1
            shift
            ;;
        --allure-open)
            ALLURE=1
            ALLURE_OPEN=1
            shift
            ;;
        --list)
            echo -e "${CYAN}Доступные сущности:${NC}"
            echo "  company          — CompanyService / CompanyRepository"
            echo "  employee         — EmployeeService / EmployeeRepository"
            echo "  education        — EducationService / EducationRepository"
            echo "  position         — PositionService / PositionRepository"
            echo "  positionhistory  — PositionHistoryService / PositionHistoryRepository"
            echo "  post             — PostService / PostRepository"
            echo "  posthistory      — PostHistoryService / PostHistoryRepository"
            echo "  score            — ScoreService / ScoreRepository"
            echo ""
            echo -e "${CYAN}Примеры:${NC}"
            echo "  ./run-unit-tests.sh company               — тесты Company (repo + service)"
            echo "  ./run-unit-tests.sh repo company           — только CompanyRepository"
            echo "  ./run-unit-tests.sh service education      — только EducationService"
            echo "  ./run-unit-tests.sh controller company     — только CompanyController"
            echo "  ./run-unit-tests.sh repo employee post     — EmployeeRepo + PostRepo"
            exit 0
            ;;
        --help|-h)
            echo -e "${CYAN}Использование:${NC}"
            echo "  ./run-unit-tests.sh                        — запустить все тесты"
            echo "  ./run-unit-tests.sh repo                   — только тесты репозиториев"
            echo "  ./run-unit-tests.sh service                — только тесты сервисов"
            echo "  ./run-unit-tests.sh controller             — только тесты контроллеров"
            echo "  ./run-unit-tests.sh company                — тесты Company (repo + service)"
            echo "  ./run-unit-tests.sh repo employee          — тесты EmployeeRepository"
            echo "  ./run-unit-tests.sh service education      — тесты EducationService"
            echo "  ./run-unit-tests.sh controller education   — тесты EducationController"
            echo "  ./run-unit-tests.sh repo employee post     — несколько сущностей"
            echo "  ./run-unit-tests.sh --filter \"FullyQualifiedName~Add\"  — произвольный фильтр"
            echo "  ./run-unit-tests.sh --verbose              — подробный вывод"
            echo "  ./run-unit-tests.sh --coverage             — с отчётом покрытия"
            echo "  ./run-unit-tests.sh --allure               — сгенерировать Allure-отчёт"
            echo "  ./run-unit-tests.sh --allure-open          — сгенерировать и открыть Allure-отчёт"
            echo "  ./run-unit-tests.sh --list                 — список доступных сущностей"
            exit 0
            ;;
        *)
            if is_entity "$1"; then
                ENTITY_LIST="$ENTITY_LIST $1"
            else
                EXTRA_ARGS="$EXTRA_ARGS $1"
            fi
            shift
            ;;
    esac
done

# Формируем фильтр dotnet test
DOTNET_FILTER=""
if [ -n "$ENTITY_LIST" ]; then
    PARTS=""
    for e in $ENTITY_LIST; do
        pascal=$(entity_to_pascal "$e")
        part=$(build_entity_filter "$pascal" "$TARGET")
        if [ -n "$PARTS" ]; then
            PARTS="${PARTS}|${part}"
        else
            PARTS="$part"
        fi
    done
    if [ -n "$FILTER" ]; then
        DOTNET_FILTER="--filter (${PARTS})&${FILTER}"
    else
        DOTNET_FILTER="--filter ${PARTS}"
    fi
elif [ -n "$FILTER" ]; then
    DOTNET_FILTER="--filter ${FILTER}"
fi

echo -e "${CYAN}Запуск тестов${NC}"
if [ -n "$ENTITY_LIST" ]; then
    echo -e "${CYAN}Сущности:${ENTITY_LIST}${NC}"
fi
if [ -n "$DOTNET_FILTER" ]; then
    echo -e "${CYAN}Фильтр: ${DOTNET_FILTER}${NC}"
fi
echo ""

run_tests() {
    local project="$1"
    local name="$2"

    echo -e "${YELLOW}Запуск: ${name}${NC}"
    echo ""

    if dotnet test "$project" \
        --no-restore \
        --logger "console;verbosity=normal" \
        $DOTNET_FILTER $VERBOSE $COVERAGE $EXTRA_ARGS \
        -- xUnit.ReporterSwitch=allure; then
        echo -e "${GREEN}${name}: PASSED${NC}"
        echo ""
        return 0
    else
        echo -e "${RED}${name}: FAILED${NC}"
        echo ""
        return 1
    fi
}

build_project() {
    local project="$1"
    local name="$2"

    echo -e "${YELLOW}Сборка: ${name}...${NC}"
    if ! dotnet build "$project" -q 2>&1; then
        echo -e "${YELLOW}Очистка кэша и пересборка...${NC}"
        find "$(dirname "$project")" -type d -name obj -exec rm -rf {} + 2>/dev/null || true
        dotnet restore "$project"
        dotnet build "$project"
    fi
    echo -e "${GREEN}${name}: сборка успешна${NC}"
}

case "$TARGET" in
    repo)
        build_project "$REPO_TESTS" "Тесты репозиториев"
        ;;
    service)
        build_project "$SERVICE_TESTS" "Тесты сервисов"
        ;;
    controller)
        build_project "$CONTROLLER_TESTS" "Тесты контроллеров"
        ;;
    *)
        build_project "$REPO_TESTS" "Тесты репозиториев"
        build_project "$SERVICE_TESTS" "Тесты сервисов"
        build_project "$CONTROLLER_TESTS" "Тесты контроллеров"
        ;;
esac
echo ""

rm -rf "$ALLURE_RESULTS" 2>/dev/null

FAILED=0

case "$TARGET" in
    repo)
        run_tests "$REPO_TESTS" "Тесты репозиториев" || FAILED=1
        ;;
    service)
        run_tests "$SERVICE_TESTS" "Тесты сервисов" || FAILED=1
        ;;
    controller)
        run_tests "$CONTROLLER_TESTS" "Тесты контроллеров" || FAILED=1
        ;;
    *)
        run_tests "$REPO_TESTS" "Тесты репозиториев" || FAILED=1
        run_tests "$SERVICE_TESTS" "Тесты сервисов" || FAILED=1
        run_tests "$CONTROLLER_TESTS" "Тесты контроллеров" || FAILED=1
        ;;
esac

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}Все тесты пройдены${NC}"
else
    echo -e "${RED}Тесты не пройдены${NC}"
fi

# Генерация Allure-отчёта
if [ -n "$ALLURE" ]; then
    echo ""
    echo -e "${CYAN}Генерация Allure-отчёта...${NC}"

    if [ ! -f "$ALLURE_BIN" ]; then
        echo -e "${RED}Allure CLI не найден: $ALLURE_BIN${NC}"
        echo -e "${YELLOW}Установите: скачайте https://github.com/allure-framework/allure2/releases${NC}"
        echo -e "${YELLOW}и распакуйте в директорию tools/ проекта.${NC}"
        exit 1
    fi

    if [ ! -d "$ALLURE_RESULTS" ] || [ -z "$(ls -A "$ALLURE_RESULTS" 2>/dev/null)" ]; then
        echo -e "${RED}Директория allure-results пуста или не найдена.${NC}"
        echo -e "${YELLOW}Убедитесь, что Allure.Xunit установлен в тестовых проектах.${NC}"
        exit 1
    fi

    "$ALLURE_BIN" generate "$ALLURE_RESULTS" -o "$ALLURE_REPORT" --clean 2>&1
    echo -e "${GREEN}Allure-отчёт сгенерирован: ${ALLURE_REPORT}/index.html${NC}"

    if [ -n "$ALLURE_OPEN" ]; then
        echo -e "${CYAN}Открытие отчёта в браузере...${NC}"
        "$ALLURE_BIN" open "$ALLURE_REPORT" 2>&1 &
    fi
fi

exit $FAILED
