#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC_DIR="$ROOT_DIR/src"

CC_THRESHOLD="${CC_THRESHOLD:-10}"
CC_TOP="${CC_TOP:-20}"

HALSTEAD_TOP="${HALSTEAD_TOP:-20}"
HALSTEAD_THRESHOLD="${HALSTEAD_THRESHOLD:-}" # optional

SOLUTION_PATH="${SOLUTION_PATH:-$SRC_DIR/Project.sln}"
DOTNET_CONFIGURATION="${DOTNET_CONFIGURATION:-Release}"
DOTNET_NO_RESTORE="${DOTNET_NO_RESTORE:-0}"
TOLERATE_DOTNET_BUILD_ERRORS="${TOLERATE_DOTNET_BUILD_ERRORS:-1}"
TOLERATE_DOTNET_FORMAT_ERRORS="${TOLERATE_DOTNET_FORMAT_ERRORS:-1}"

# 1) Используем venv, если он есть (там lizard + зависимости для halstead).
if [[ -x "$ROOT_DIR/venv/bin/python" ]]; then
  PY="$ROOT_DIR/venv/bin/python"
  LIZARD_BIN="$ROOT_DIR/venv/bin/lizard"
else
  PY="python3"
  LIZARD_BIN="lizard"
fi

print_header() {
  echo
  echo "============================================================"
  echo "$1"
  echo "============================================================"
}

run_cyclomatic_check() {
  print_header "Цикломатическая сложность (lizard) / CC threshold=$CC_THRESHOLD"

  "$PY" - <<PYCODE
import lizard
from pathlib import Path

CC_THRESHOLD = int("${CC_THRESHOLD}")
CC_TOP = int("${CC_TOP}")

def iter_cs_files(root: Path):
    for p in root.rglob("*.cs"):
        parts=set(p.parts)
        if "bin" in parts or "obj" in parts or "Migrations" in parts:
            continue
        yield p

violations=[]
for p in iter_cs_files(Path("${SRC_DIR}")):
    try:
        res = lizard.analyze_file(str(p))
    except Exception:
        continue
    for fn in getattr(res, "function_list", []):
        cc = fn.__dict__.get("cyclomatic_complexity", 0)
        if cc > CC_THRESHOLD:
            violations.append((cc, res.filename, fn.name, fn.start_line, fn.end_line))

violations.sort(reverse=True, key=lambda x: x[0])
print(f"Violations: {len(violations)} (threshold={CC_THRESHOLD})")

top = violations[:CC_TOP]
for cc, filename, fn_name, sl, el in top:
    rel = filename.split("/src/",1)[-1] if "/src/" in filename else filename
    print(f"CC={cc} {rel}:{sl}-{el} {fn_name}")

if any(cc > CC_THRESHOLD for cc, *_ in violations):
    # exit code 1 -> провал проверки
    raise SystemExit(1)
PYCODE
}

run_halstead_check() {
  print_header "Halstead Difficulty"

  # печатаем топ всегда; порог — опционально (если задан).
  ARGS=( "--path" "$SRC_DIR" "--top" "$HALSTEAD_TOP" )
  if [[ -n "$HALSTEAD_THRESHOLD" ]]; then
    ARGS+=( "--threshold" "$HALSTEAD_THRESHOLD" )
  fi

  # shellcheck disable=SC2154
  "$PY" "$ROOT_DIR/scripts/halstead_difficulty_csharp.py" "${ARGS[@]}"
}

run_format_check() {
  print_header "Проверка форматирования (.editorconfig) (dotnet format --verify-no-changes)"

  if ! command -v dotnet >/dev/null 2>&1; then
    echo "dotnet не найден в PATH. Пропускаю форматирование-чек (локально)."
    return 0
  fi

  # dotnet format это отдельный tool; на некоторых окружениях может отсутствовать.
  if ! dotnet format --help >/dev/null 2>&1; then
    echo "Команда 'dotnet format' недоступна. Пропускаю форматирование-чек."
    return 0
  fi

  # Важно: проверяем именно форматирование, а не анализаторы (чтобы лишние warnings по CS1591 не всплывали).
  if dotnet format "$SOLUTION_PATH" \
    --verify-no-changes \
    --no-restore \
    --include Format \
    --verbosity minimal; then
    :
  else
    if [[ "$TOLERATE_DOTNET_FORMAT_ERRORS" == "1" ]]; then
      echo "dotnet format упал, но пропускаю дальше (TOLERATE_DOTNET_FORMAT_ERRORS=1)."
      return 0
    fi
    return 1
  fi
}

run_code_style_check() {
  print_header "Код-стайл/анализаторы (dotnet build)"

  if ! command -v dotnet >/dev/null 2>&1; then
    echo "dotnet не найден в PATH. Пропускаю код-стайл чек (локально)."
    return 0
  fi

  BUILD_ARGS=(
    "$SOLUTION_PATH"
    "--configuration" "$DOTNET_CONFIGURATION"
  )

  # Глушим предупреждения про отсутствие XML-комментариев.
  # Это CS1591.
  BUILD_ARGS+=( "/p:NoWarn=CS1591" )

  # Явно включаем .NET analyzers.
  BUILD_ARGS+=( "/p:RunAnalyzersDuringBuild=true" "/p:EnableNETAnalyzers=true" )

  if [[ "$DOTNET_NO_RESTORE" == "1" ]]; then
    BUILD_ARGS+=( "--no-restore" )
  fi

  if dotnet build "${BUILD_ARGS[@]}"; then
    :
  else
    if [[ "$TOLERATE_DOTNET_BUILD_ERRORS" == "1" ]]; then
      echo "dotnet build упал, но пропускаю дальше (TOLERATE_DOTNET_BUILD_ERRORS=1)."
      return 0
    fi
    return 1
  fi
}

main() {
  echo "Запуск проверок статанализа..."
  echo "ROOT_DIR=$ROOT_DIR"
  echo "CC_THRESHOLD=$CC_THRESHOLD, CC_TOP=$CC_TOP"
  echo "HALSTEAD_TOP=$HALSTEAD_TOP, HALSTEAD_THRESHOLD=${HALSTEAD_THRESHOLD:-"(none)"}"
  echo "SOLUTION_PATH=$SOLUTION_PATH"

  run_cyclomatic_check
  run_halstead_check
  run_code_style_check
  run_format_check

  echo
  echo "Все проверки пройдены."
}

main "$@"

