---
applyTo: "**/*.py,**/*.pyi,**/pyproject.toml"
description: "Python language, tooling, testing and review rules"
---

# Python Guidelines

## Purpose
Rules for writing and reviewing Python code. Targets **Python 3.14** (current stable) for new projects and **Python 3.12+** as the minimum for existing ones. Python 3.9 is out of support and 3.10 reaches end of life in October 2026; don't start new work on either.

Always check `requires-python` in `pyproject.toml` (and `.python-version`) first. Only use language features available in the lowest supported version; never raise the minimum version silently. If the project has more specific instructions (e.g. [Rich](rich-python.instructions.md) for terminal output), follow them as well.

## Project setup
- Manage projects, dependencies, Python versions and virtual environments with **`uv`** unless the project already uses another tool (`poetry`, `pdm`, `pip-tools`). Don't mix tools in one project.
- All metadata and tool configuration lives in `pyproject.toml` (PEP 621); no `setup.py`, `setup.cfg` or `requirements.txt` in new projects. Commit `uv.lock`.
- Use the `src/` layout (`src/<package>/`) for packages and libraries; flat layout is fine for single-file scripts.
- Declare dependencies with compatible version ranges (`httpx>=0.28,<1`); put dev tools in `[dependency-groups]` (`uv add --dev`), not in runtime dependencies.
- Pin the interpreter with `.python-version` (`uv python pin 3.14`) and keep it consistent with `requires-python`.
- For standalone scripts, use inline script metadata (PEP 723) and run them with `uv run script.py` instead of creating a project:
  ```python
  # /// script
  # requires-python = ">=3.14"
  # dependencies = ["httpx>=0.28"]
  # ///
  ```
- Run everything through the environment (`uv run pytest`, `uv run python -m app`); never install packages into the system Python.

## Formatting and linting
- Format and lint with **Ruff** (`ruff format`, `ruff check --fix`); it replaces Black, isort, Flake8 and pyupgrade. Configuration belongs in `[tool.ruff]`.
- Line length 100 unless the project configures otherwise. Four spaces for indentation, never tabs.
- Recommended rule set as a baseline: `select = ["E", "W", "F", "I", "B", "UP", "SIM", "RUF", "N", "S", "PTH", "C4", "RET", "PL", "TC"]`. Don't disable rules globally to silence one warning; use a targeted `# noqa: <code>` with a reason.
- Type-check with **mypy** (`strict = true`) or **pyright** (`typeCheckingMode = "strict"`) as configured by the project; new projects use strict mode. CI must run formatter check, linter, type checker and tests.
- Use `pre-commit` (or the project's hook tool) to run Ruff before commits.

## Naming and structure
- PEP 8 naming: `snake_case` for functions, variables and modules; `PascalCase` for classes and type aliases; `UPPER_SNAKE_CASE` for constants; `_leading_underscore` for internal names.
- Clear, descriptive names; no abbreviations or single letters except loop indexes and well-known math variables. Boolean names read as questions (`is_active`, `has_items`).
- Small functions with a single responsibility; prefer early returns over deep nesting.
- Define the public API of a package in `__init__.py` with `__all__`; don't use wildcard imports (`from x import *`).
- Imports at the top of the file, grouped standard library → third party → local (Ruff sorts them). Use absolute imports; relative imports only within a package.
- Guard script entry points with `if __name__ == "__main__":` and keep module import free of side effects (no I/O, network calls or configuration loading at import time).

## Types
- Annotate all function signatures (parameters and return types) and class attributes; locals only when the type isn't inferable.
- Use modern syntax: built-in generics (`list[str]`, `dict[str, int]`), unions with `|` (`str | None`), `collections.abc` for abstract types (`Iterable`, `Mapping`, `Callable`, `Sequence`).
- Accept the most general type (`Iterable[str]`, `Mapping[str, int]`), return the most specific one (`list[str]`).
- Use PEP 695 syntax (3.12+) for generics and aliases: `def first[T](items: Sequence[T]) -> T:`, `class Box[T]:`, `type UserId = int`. No `TypeVar` boilerplate in new code.
- Use `typing.Self`, `@override`, `Literal`, `TypedDict` (with `Required`/`NotRequired`/`ReadOnly`), `Protocol` for structural interfaces, `TypeIs` for narrowing functions, and `Final` for constants.
- On 3.14+, annotations are evaluated lazily (PEP 649); don't add `from __future__ import annotations` in new 3.14-only code, and don't quote forward references. Keep it where the project still supports older versions.
- Avoid `Any`; use `object` when any value is accepted, and `cast()` only with a comment explaining why. `# type: ignore[code]` must name the error code.
- Put imports only needed for typing under `if TYPE_CHECKING:`.

## Idioms
- f-strings for interpolation; t-strings (PEP 750, 3.14+) when a library needs to process the interpolated parts safely (SQL, HTML, shell). Never build SQL, HTML or shell commands with f-strings or `%` from untrusted input.
- `pathlib.Path` for all filesystem paths; open files with an explicit `encoding="utf-8"`.
- `with` statements for every resource (files, locks, connections, sessions); `contextlib.contextmanager` or `ExitStack` for custom ones.
- `@dataclass(slots=True, frozen=True, kw_only=True)` for plain data; Pydantic v2 models for validating external data (API payloads, configuration); `enum.Enum` / `StrEnum` instead of magic strings.
- Comprehensions and generator expressions for simple transformations; a regular loop when there's more than one condition or side effect.
- `match` statements for structural dispatch on shapes or types, not as a replacement for a simple `if`/`elif`.
- Never use mutable default arguments (`def f(x: list[str] = [])`); use `None` or `field(default_factory=list)`.
- Use `datetime.now(tz=UTC)` and timezone-aware datetimes; never naive `datetime.now()` / `utcnow()`. Store and transmit times in UTC, ISO 8601.
- Use `decimal.Decimal` for money, `secrets` for security-related randomness, `tomllib` for reading TOML, `zoneinfo` for time zones.
- `functools.cache` / `lru_cache` for pure functions only; `itertools` and `collections` (`Counter`, `defaultdict`, `deque`) instead of hand-written equivalents.

## Error handling
- Raise specific exceptions (built-in or a project exception hierarchy rooted in one base class); never raise bare `Exception`.
- Catch only what you can handle, as narrowly as possible. No bare `except:`; no broad `except Exception` that hides failures. A top-level handler at the entry point may catch broadly to log and exit.
- Chain exceptions with `raise NewError(...) from err` to preserve the cause; re-raise with plain `raise`.
- Use `ExceptionGroup` / `except*` for concurrent failures (e.g. from `TaskGroup`). On 3.14+ multiple exception types may be written without parentheses (`except ValueError, TypeError:`) only when not using `as`; keep parentheses if older versions are supported.
- Validate inputs at the boundaries (CLI, API, configuration, file parsing) and fail fast with clear messages; trust validated data internally.
- Don't use `assert` for runtime validation; it's stripped with `-O`.

## Logging and output
- Use the `logging` module with `log = logging.getLogger(__name__)` per module; configure handlers only once at the application entry point, never in libraries.
- Use lazy formatting (`log.info("Loaded %d items from %s", count, path)`), not f-strings, in log calls; `log.exception()` inside `except` blocks.
- Use structured (JSON) logging for services and containers; never log secrets, tokens or personal data.
- No `print()` in library or service code; CLI output goes through the project's output layer (see the Rich instructions).

## Async and concurrency
- Use `asyncio` for I/O-bound concurrency with `async`/`await` all the way; never call blocking I/O (`requests`, `time.sleep`, file-heavy work) inside coroutines — use async libraries (`httpx.AsyncClient`, `asyncpg`) or `asyncio.to_thread()`.
- Use `asyncio.TaskGroup` for structured concurrency and `asyncio.timeout()` for deadlines instead of bare `create_task` / `gather` / `wait_for`. Keep references to background tasks.
- Start programs with `asyncio.run(main())`; don't manage event loops manually.
- Use `concurrent.futures.ThreadPoolExecutor` for blocking I/O and `ProcessPoolExecutor` (or `InterpreterPoolExecutor` on 3.14+) for CPU-bound work. Don't rely on the free-threaded build unless the project explicitly targets it.
- Protect shared mutable state with locks or avoid it; prefer passing data through queues.

## Configuration and security
- Keep secrets out of source code, tests and logs. Read configuration from environment variables or an approved secret store (e.g. Azure Key Vault); use `pydantic-settings` for typed configuration with validation at startup.
- Don't commit `.env` files; provide `.env.example` instead.
- Use parameterized queries or an ORM (SQLAlchemy 2.x with typed `Mapped[]` models) for databases; never string-built SQL.
- Never use `eval`, `exec`, `pickle` or `yaml.load` (use `yaml.safe_load`) on untrusted data; use `subprocess.run([...], check=True)` with an argument list, never `shell=True` with interpolated input.
- Set timeouts on every network call (`httpx.Client(timeout=...)`); verify TLS certificates.
- Check dependencies for vulnerabilities in CI (e.g. `pip-audit`) and keep them updated.
- For code that creates or configures Azure resource names, follow [the Azure naming convention](azure-naming-convention.instructions.md) and respect each service's naming restrictions.

## Documentation
- Concise docstrings (Google style unless the project uses another) on public modules, classes and functions whose purpose or behavior isn't obvious. Document parameters, return values and raised exceptions; don't repeat the type annotations in prose.
- Comments explain *why*, not *what*. Remove commented-out code.
- Keep `README.md` up to date with setup (`uv sync`), run and test commands.

## Testing
- **pytest** with plain `assert`; tests in `tests/` mirroring the package structure, files named `test_<module>.py`.
- Name tests `test_<behavior>_<condition>_<expected>`; Arrange-Act-Assert; one behavior per test.
- Use fixtures (with the narrowest scope) for setup, `@pytest.mark.parametrize` for input variations, `tmp_path` for files and `monkeypatch` for environment variables.
- Mock only at boundaries (network, time, external services) with `unittest.mock` / `pytest-mock`; use `respx` for `httpx`, Testcontainers for real databases instead of in-memory substitutes.
- Use `pytest-asyncio` (or `anyio`) for async tests; `hypothesis` for property-based tests of parsing and pure logic.
- Measure coverage with `pytest-cov`; new or changed behavior must have tests. Tests must be deterministic and independent of execution order.

## Code review focus
Flag in this order: secrets in code or logs, injection risks (SQL, shell, `eval`, `pickle`, unsafe YAML), swallowed or overly broad exceptions, blocking calls inside async code, missing timeouts on network calls, mutable default arguments, naive datetimes, missing or `Any`-heavy type annotations, features newer than `requires-python`, side effects at import time, missing tests for changed behavior.
