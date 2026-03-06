#!/bin/bash

set -e

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/test-common.sh"

acquire_run_lock
trap release_run_lock EXIT INT TERM

parse_group_args "$@"

DOTNET_FILTER="$(compose_dotnet_filter e2e)"
build_project "$E2E_TESTS" "E2E тесты"
run_project_tests "$E2E_TESTS" "E2E тесты" "e2e-tests.trx" "$DOTNET_FILTER"
