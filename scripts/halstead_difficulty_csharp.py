import argparse
import re
from pathlib import Path

import lizard


def strip_comments(code: str) -> str:
    # Remove // comments
    code = re.sub(r"//.*?$", "", code, flags=re.MULTILINE)
    # Remove /* */ comments
    code = re.sub(r"/\*.*?\*/", "", code, flags=re.DOTALL)
    return code


_STRING_RE = r'"(?:\\.|[^"\\])*"'  # "..."
_VERBATIM_STRING_RE = r'@' + _STRING_RE  # @"..."
_CHAR_RE = r"'(?:\\.|[^'\\])'"  # '...'
_NUMBER_RE = r"\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b"
_IDENT_RE = r"\b[A-Za-z_][A-Za-z0-9_]*\b"

# Multi-char operators first, then single-char.
_OP_TOKENS = [
    "==",
    "!=",
    "<=",
    ">=",
    "&&",
    "||",
    "??",
    "??=",
    "<<",
    ">>",
    "<<=",
    ">>=",
    "++",
    "--",
    "+=",
    "-=",
    "*=",
    "/=",
    "%=",
    "&=",
    "|=",
    "^=",
    "=>",
    "+",
    "-",
    "*",
    "/",
    "%",
    "=",
    "<",
    ">",
    "!",
    "~",
    "&",
    "|",
    "^",
    "?",
    ":",
]

_OP_KEYWORDS = {
    # Control flow / keywords commonly treated as operators in simplified Halstead.
    "if",
    "else",
    "for",
    "foreach",
    "while",
    "do",
    "switch",
    "case",
    "break",
    "continue",
    "return",
    "throw",
    "try",
    "catch",
    "finally",
    "using",
    "yield",
    "await",
    "var",
    "new",
    "sizeof",
    "typeof",
    "nameof",
    "is",
    "as",
    "in",
    "out",
    "ref",
    "get",
    "set",
}

_LITERAL_OPERANDS = {
    "true": "BOOL",
    "false": "BOOL",
    "null": "NULL",
}


def build_token_regex() -> re.Pattern:
    # Note: order matters: literals/operators first, then identifiers.
    parts = [
        _VERBATIM_STRING_RE,
        _STRING_RE,
        _CHAR_RE,
        _NUMBER_RE,
        "|".join(re.escape(op) for op in sorted(_OP_TOKENS, key=len, reverse=True)),
        _IDENT_RE,
    ]
    return re.compile("|".join(parts))


TOKEN_RE = build_token_regex()


def is_operator_token(tok: str) -> bool:
    # Operators are either explicit operator tokens or certain keywords.
    if tok in _OP_TOKENS:
        return True
    return tok in _OP_KEYWORDS


def operand_normalize(tok: str) -> str | None:
    if tok in _LITERAL_OPERANDS:
        return _LITERAL_OPERANDS[tok]

    # Identifiers are operands.
    if re.fullmatch(_IDENT_RE, tok):
        return tok

    # Literals are operands (normalize by kind).
    if re.fullmatch(_NUMBER_RE, tok):
        return "NUM"
    if re.fullmatch(_CHAR_RE, tok):
        return "CHAR"
    if re.fullmatch(_VERBATIM_STRING_RE, tok) or re.fullmatch(_STRING_RE, tok):
        return "STR"

    return None


def compute_halstead_difficulty(code: str) -> dict:
    code = strip_comments(code)

    unique_operators: set[str] = set()
    operands_total = 0
    unique_operands: set[str] = set()

    for m in TOKEN_RE.finditer(code):
        tok = m.group(0)

        if is_operator_token(tok):
            unique_operators.add(tok)
            continue

        opnd = operand_normalize(tok)
        if opnd is None:
            continue

        operands_total += 1
        unique_operands.add(opnd)

    unique_operators_count = len(unique_operators)
    unique_operands_count = len(unique_operands)

    if unique_operands_count == 0:
        difficulty = float("inf") if unique_operators_count > 0 else 0.0
    else:
        difficulty = (unique_operators_count / 2.0) * (operands_total / unique_operands_count)

    return {
        "UniqueOperators": unique_operators_count,
        "UniqueOperands": unique_operands_count,
        "Operands": operands_total,
        "Difficulty": difficulty,
    }


def iter_csharp_functions(root: Path):
    # Use lizard to discover function boundaries for C#.
    for path in root.rglob("*.cs"):
        parts = set(path.parts)
        if "bin" in parts or "obj" in parts or "Migrations" in parts:
            continue

        try:
            analysis = lizard.analyze_file(str(path))
        except Exception:
            continue

        # Read once; slice per function by lizard line numbers (1-based, inclusive).
        try:
            lines = path.read_text(encoding="utf-8", errors="ignore").splitlines()
        except Exception:
            continue

        for fn in analysis.function_list:
            start = getattr(fn, "start_line", None)
            end = getattr(fn, "end_line", None)
            if not start or not end:
                continue

            start_i = max(1, int(start))
            end_i = max(start_i, int(end))
            if start_i > len(lines):
                continue
            end_i = min(end_i, len(lines))

            fn_code = "\n".join(lines[start_i - 1 : end_i])
            yield path, fn.name, start_i, end_i, fn_code


def main():
    ap = argparse.ArgumentParser(description="Halstead Difficulty checker (C#) by token counting.")
    ap.add_argument("--path", default="src", help="Root folder to analyze (default: src).")
    ap.add_argument("--threshold", type=float, default=None, help="If set, exit non-zero on any function above it.")
    ap.add_argument("--top", type=int, default=25, help="How many worst functions to print.")
    args = ap.parse_args()

    root = Path(args.path)
    results = []

    for path, fn_name, start, end, fn_code in iter_csharp_functions(root):
        metrics = compute_halstead_difficulty(fn_code)
        results.append((metrics["Difficulty"], path, fn_name, start, end, metrics))

    results.sort(key=lambda x: x[0], reverse=True)

    worst = results[: args.top]
    for diff, path, fn_name, start, end, m in worst:
        print(
            f"{diff:.3f} Difficulty | {path.relative_to(root)}:{start}-{end} {fn_name} "
            f"(UniqueOperators={m['UniqueOperators']}, Operands={m['Operands']}, UniqueOperands={m['UniqueOperands']})"
        )

    if args.threshold is not None:
        failed = [r for r in results if r[0] > args.threshold]
        if failed:
            worst_fail = failed[0]
            diff, path, fn_name, start, end, m = worst_fail
            print(f"\nFAIL: at least one function exceeds threshold {args.threshold}. First: {diff:.3f}")
            print(
                f"       {path.relative_to(root)}:{start}-{end} {fn_name} "
                f"(UniqueOperators={m['UniqueOperators']}, Operands={m['Operands']}, UniqueOperands={m['UniqueOperands']})"
            )
            raise SystemExit(1)


if __name__ == "__main__":
    main()

