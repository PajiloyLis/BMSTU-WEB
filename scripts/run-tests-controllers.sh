#!/bin/bash

set -e

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/test-common.sh"

acquire_run_lock
trap release_run_lock EXIT INT TERM

parse_group_args "$@"
if [ "$LIST_MODE" -eq 1 ]; then
    print_entities_help
    exit 0
fi

DOTNET_FILTER="$(compose_dotnet_filter controller)"
build_project "$CONTROLLER_TESTS" "Тесты контроллеров"
run_project_tests "$CONTROLLER_TESTS" "Тесты контроллеров" "controller-tests.trx" "$DOTNET_FILTER"
