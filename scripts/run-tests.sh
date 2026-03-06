#!/bin/bash

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

source "$SCRIPT_DIR/test-common.sh"

TARGET=""
ALLURE=0
ALLURE_OPEN=0
SETUP=1
LIST_ONLY=0
FORWARDED_ARGS=()

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
        integration|integrations|it)
            TARGET="integration"
            shift
            ;;
        e2e)
            TARGET="e2e"
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
        --no-setup)
            SETUP=0
            shift
            ;;
        --list)
            LIST_ONLY=1
            FORWARDED_ARGS+=("$1")
            shift
            ;;
        --help|-h)
            cat <<EOF
Использование:
  ./scripts/run-tests.sh [target] [options] [entities|filter args]

Target:
  repo | service | controller | integration | e2e
  (если target не указан: repo -> service -> controller)

Options:
  --allure        Сгенерировать Allure-отчёт после запуска
  --allure-open   Сгенерировать и открыть Allure-отчёт
  --list          Показать список сущностей для фильтрации
  --no-setup      Не вызывать setup-test-env.sh
  --help          Показать эту справку

Примеры:
  ./scripts/run-tests.sh
  ./scripts/run-tests.sh repo employee
  ./scripts/run-tests.sh controller --filter "FullyQualifiedName~Company"
  ./scripts/run-tests.sh integration
EOF
            exit 0
            ;;
        *)
            FORWARDED_ARGS+=("$1")
            shift
            ;;
    esac
done

if [ "$LIST_ONLY" -eq 1 ] && [ -z "$TARGET" ]; then
    print_entities_help
    exit 0
fi

if [ "$SETUP" -eq 1 ]; then
    bash "$SCRIPT_DIR/setup-test-env.sh"
fi

if [ "$ALLURE" -eq 1 ]; then
    prepare_allure_results_dirs
fi

FAILED=0

run_group_script() {
    local script="$1"
    if ! bash "$script" "${FORWARDED_ARGS[@]}"; then
        FAILED=1
    fi
}

case "$TARGET" in
    repo)
        run_group_script "$SCRIPT_DIR/run-tests-repositories.sh"
        ;;
    service)
        run_group_script "$SCRIPT_DIR/run-tests-services.sh"
        ;;
    controller)
        run_group_script "$SCRIPT_DIR/run-tests-controllers.sh"
        ;;
    integration)
        if ! bash "$SCRIPT_DIR/run-integration-external.sh" "${FORWARDED_ARGS[@]}"; then
            FAILED=1
        fi
        ;;
    e2e)
        run_group_script "$SCRIPT_DIR/run-tests-e2e.sh"
        ;;
    *)
        run_group_script "$SCRIPT_DIR/run-tests-repositories.sh"
        run_group_script "$SCRIPT_DIR/run-tests-services.sh"
        run_group_script "$SCRIPT_DIR/run-tests-controllers.sh"
        ;;
esac

if [ "$FAILED" -eq 0 ]; then
    echo -e "${GREEN}Все тесты пройдены${NC}"
else
    echo -e "${RED}Тесты не пройдены${NC}"
fi

if [ "$ALLURE" -eq 1 ]; then
    generate_allure_report "$ALLURE_OPEN" || FAILED=1
fi

exit "$FAILED"
