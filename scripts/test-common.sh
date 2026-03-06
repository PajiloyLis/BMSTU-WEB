#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
SRC_DIR="$ROOT_DIR/src"

LOCK_FILE="$ROOT_DIR/.run-tests.lock"

REPO_TESTS="$SRC_DIR/Tests/Project.Repository.Tests/Project.Repository.Tests.csproj"
SERVICE_TESTS="$SRC_DIR/Tests/Project.Service.Tests/Project.Service.Tests.csproj"
CONTROLLER_TESTS="$SRC_DIR/Tests/Project.Controller.Tests/Project.Controller.Tests.csproj"
E2E_TESTS="$SRC_DIR/Tests/Project.E2E.Tests/Project.E2E.Tests.csproj"

ALLURE_BIN="$ROOT_DIR/tools/allure-2.36.0/bin/allure"
ALLURE_REPORT="$ROOT_DIR/allure-report"
ALLURE_MERGED_RESULTS="$ROOT_DIR/.allure-merged-results"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

ENTITIES="company employee education position positionhistory post posthistory score"

LOCK_ACQUIRED=0
ENTITY_LIST=""
FILTER=""
VERBOSE=""
COVERAGE=""
EXTRA_ARGS=""
LIST_MODE=0

acquire_run_lock() {
    exec 9>"$LOCK_FILE"
    echo -e "${YELLOW}Ожидание блокировки запуска тестов (lock: $LOCK_FILE)...${NC}"
    flock 9
    LOCK_ACQUIRED=1
}

release_run_lock() {
    if [ "$LOCK_ACQUIRED" -eq 1 ]; then
        flock -u 9 || true
    fi
    exec 9>&- || true
}

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

build_entity_filter() {
    local entity_pascal="$1"
    local target="$2"

    case "$target" in
        repo)       echo "FullyQualifiedName~${entity_pascal}Repository" ;;
        service)    echo "FullyQualifiedName~${entity_pascal}Service" ;;
        controller) echo "FullyQualifiedName~${entity_pascal}Controller" ;;
        *)          echo "FullyQualifiedName~${entity_pascal}" ;;
    esac
}

parse_group_args() {
    ENTITY_LIST=""
    FILTER=""
    VERBOSE=""
    COVERAGE=""
    EXTRA_ARGS=""
    LIST_MODE=0

    while [[ $# -gt 0 ]]; do
        case "$1" in
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
            --list)
                LIST_MODE=1
                shift
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
}

compose_dotnet_filter() {
    local target="$1"

    if [ -n "$ENTITY_LIST" ]; then
        local parts=""
        for e in $ENTITY_LIST; do
            local pascal part
            pascal=$(entity_to_pascal "$e")
            part=$(build_entity_filter "$pascal" "$target")
            if [ -n "$parts" ]; then
                parts="${parts}|${part}"
            else
                parts="$part"
            fi
        done
        if [ -n "$FILTER" ]; then
            echo "--filter (${parts})&${FILTER}"
        else
            echo "--filter ${parts}"
        fi
        return
    fi

    if [ -n "$FILTER" ]; then
        echo "--filter ${FILTER}"
    else
        echo ""
    fi
}

print_entities_help() {
    echo -e "${CYAN}Доступные сущности:${NC}"
    echo "  company employee education position positionhistory post posthistory score"
}

build_project() {
    local project="$1"
    local name="$2"

    echo -e "${YELLOW}Сборка: ${name}...${NC}"
    if ! dotnet build "$project" -q 2>&1; then
        echo -e "${YELLOW}Очистка кэша и пересборка...${NC}"
        dotnet restore "$project"
        dotnet build "$project"
    fi
    echo -e "${GREEN}${name}: сборка успешна${NC}"
}

run_project_tests() {
    local project="$1"
    local name="$2"
    local trx_file="$3"
    local dotnet_filter="$4"

    echo -e "${YELLOW}Запуск: ${name}${NC}"
    echo ""

    if dotnet test "$project" \
        --no-restore \
        --logger "trx;LogFileName=${trx_file}" \
        --results-directory TestResults \
        $dotnet_filter $VERBOSE $COVERAGE $EXTRA_ARGS \
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

prepare_allure_results_dirs() {
    find "$ROOT_DIR" -type d -name allure-results -prune -exec rm -rf {} + 2>/dev/null || true
    rm -rf "$ALLURE_MERGED_RESULTS" 2>/dev/null || true
}

generate_allure_report() {
    local open_report="$1"

    echo ""
    echo -e "${CYAN}Генерация Allure-отчёта...${NC}"

    if [ ! -f "$ALLURE_BIN" ]; then
        echo -e "${RED}Allure CLI не найден: $ALLURE_BIN${NC}"
        echo -e "${YELLOW}Установите Allure CLI в tools/.${NC}"
        return 1
    fi

    mkdir -p "$ALLURE_MERGED_RESULTS"
    local found=0
    while IFS= read -r dir; do
        found=1
        cp -R "$dir"/. "$ALLURE_MERGED_RESULTS"/ 2>/dev/null || true
    done < <(find "$ROOT_DIR" -type d -name allure-results)

    if [ "$found" -eq 0 ] || [ -z "$(ls -A "$ALLURE_MERGED_RESULTS" 2>/dev/null)" ]; then
        echo -e "${RED}Allure-результаты не найдены.${NC}"
        return 1
    fi

    "$ALLURE_BIN" generate "$ALLURE_MERGED_RESULTS" -o "$ALLURE_REPORT" --clean 2>&1
    echo -e "${GREEN}Allure-отчёт: ${ALLURE_REPORT}/index.html${NC}"

    if [ "$open_report" -eq 1 ]; then
        "$ALLURE_BIN" open "$ALLURE_REPORT" 2>&1 &
    fi
}
