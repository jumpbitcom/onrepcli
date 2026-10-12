# Development Environment Guidelines

These rules apply to every file and language in this repository. Language-specific rules live in `.github/instructions/*.instructions.md` and take precedence for their file types.

## Instruction files
Path-specific files are applied automatically when their `applyTo` pattern matches the file being worked on. When working on a file type listed below, follow the linked file in addition to this one.

| Topic | File | Applies to |
| --- | --- | --- |
| Node.js | [nodejs.instructions.md](instructions/nodejs.instructions.md) | `*.js`, `*.mjs`, `*.cjs`, `*.ts`, `package.json` |
| TypeScript | [typescript.instructions.md](instructions/typescript.instructions.md) | `*.ts`, `*.tsx`, `*.mts`, `*.cts` |
| Alpine.js | [alpinejs.instructions.md](instructions/alpinejs.instructions.md) | HTML/template files, `src/alpine/**` |
| C# and .NET | [csharp-dotnet.instructions.md](instructions/csharp-dotnet.instructions.md) | `*.cs`, `*.csproj`, `*.props`, `*.razor` |
| Avalonia UI | [avalonia.instructions.md](instructions/avalonia.instructions.md) | `*.axaml`, `Views/**`, `ViewModels/**` |
| Python | [python.instructions.md](instructions/python.instructions.md) | `*.py`, `*.pyi`, `pyproject.toml` |
| Python (Rich) | [rich-python.instructions.md](instructions/rich-python.instructions.md) | `*.py` (terminal output and rich command line interface) |
| SQL Server / T-SQL | [sqlserver.instructions.md](instructions/sqlserver.instructions.md) | `sqlserver/**/*.sql`, `mssql/**/*.sql` |
| MariaDB | [mariadb.instructions.md](instructions/mariadb.instructions.md) | `mariadb/**/*.sql` |
| PowerShell | [powershell.instructions.md](instructions/powershell.instructions.md) | `*.ps1`, `*.psm1`, `*.psd1` |
| Bash | [bash.instructions.md](instructions/bash.instructions.md) | `*.sh`, `*.bash` |
| Docker | [dockerfile.instructions.md](instructions/dockerfile.instructions.md) | `Dockerfile*`, `compose*.yaml` |

Precedence when rules conflict: the more specific file wins (e.g. Alpine.js or Electron over TypeScript, Avalonia over C#/.NET, Electron over Node.js for renderer code, Rich over Python for terminal output); path-specific files win over this file. Delete rows for technologies this repository doesn't use.

## How to work
- Read existing code, configuration and documentation before suggesting changes; follow the conventions already in the repository over general preferences.
- Make the smallest change that solves the problem. Don't reformat, rename or refactor unrelated code in the same change.
- When requirements are unclear or a change is risky (data loss, security, breaking API), stop and ask instead of guessing.
- Always use the latest stable versions of tools, libraries and services unless the repository pins a version; never suggest deprecated or end-of-life versions.
- Prefer official vendor documentation over blog posts when looking up how something works.
- Explain non-obvious decisions in the pull request description, not in chat only.

## Repository layout
- `README.md` explains purpose, prerequisites, setup, how to run, test and deploy — keep it current with every change that affects these.
- `docs/` for longer documentation and architecture decision records (`docs/adr/NNNN-title.md`).
- `src/` for source code, `tests/` (or colocated test files, per language convention), `scripts/` for automation, `.github/` for workflows and Copilot instructions.
- Generated files, build output and dependencies are never committed; keep `.gitignore` complete.

## Editor and formatting
- `.editorconfig` at the root defines indentation, line endings and charset for all file types; follow it.
- Files are UTF-8 without BOM, LF line endings (except where a tool requires CRLF, declared in `.gitattributes`), a final newline, and no trailing whitespace.
- Every language uses an automatic formatter and a linter configured in the repository; code must pass both with zero warnings before it's committed.
- Don't disable lint rules inline without a comment explaining why.

## Reproducible environment
- The repository runs in a Dev Container (`.devcontainer/devcontainer.json`) so local, Codespaces and CI environments match. Add new tools there, not only on your own machine.
- Pin tool and runtime versions in the repository (e.g. `global.json`, `.nvmrc`, `.tool-versions`, container image tags with digests).
- Commit lock files (`package-lock.json`, `pnpm-lock.yaml`, `packages.lock.json`, etc.) and install from them in CI.
- Common tasks have one entry point each (`make`, `just`, npm scripts or `scripts/*`): `setup`, `build`, `test`, `lint`, `format`, `run`. Document them in the README.
- Use .env files for loccal environment-specific configuration and secrets; do not commit them to the repository. Add `.env` to `.gitignore` if not already present. Use a `.env.example` file to document required environment variables and as a starter template.
- Load environment variables from the appropriate .env file at the start of the application.
- For Backend services, prepare environment variables in production on Azure environment and use Azure Key Vault for secrets management.
- Use github secrets and environment variables for managing secrets in CI/CD workflows. Document that as todo in the README.

## Git workflow
- Trunk-based development: short-lived branches from `main`, merged via pull request; `main` is always releasable and protected.
- Branch names: `<type>/<short-description>` (e.g. `feat/user-export`, `fix/login-timeout`).
- Commit messages follow Conventional Commits: `<type>(<scope>): <summary>` with types `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`; imperative mood, summary ≤ 72 characters; `BREAKING CHANGE:` footer for breaking changes.
- One logical change per commit; no "WIP" or "fix typo" commits in the merged history (squash or rebase).
- Never commit secrets, credentials, personal data, large binaries or generated files. Use Git LFS for required binary assets.

## Pull requests
- Small and focused (aim for under ~400 changed lines); split larger work.
- Description: what changed, why, how it was tested, risks, and screenshots for UI changes. Link the issue (`Closes #123`).
- All CI checks green and at least one approving review before merge.
- Update tests, documentation and the changelog in the same pull request as the code.

## Testing
- Every bug fix includes a test that fails without the fix; every feature includes tests for its behavior and edge cases.
- Test pyramid: many fast unit tests, fewer integration tests (with real dependencies in containers), few end-to-end tests.
- Tests are deterministic and independent: no reliance on order, wall-clock time, network access or shared state.
- Keep coverage from decreasing; focus on meaningful assertions, not coverage numbers.

## CI/CD (GitHub Actions)
- Every pull request runs: format check, lint, build, tests, dependency and security scans.
- Pin third-party actions to a full commit SHA (with the version in a comment); set minimal `permissions:` per workflow (default `contents: read`).
- Use OpenID Connect (OIDC) for cloud deployments instead of long-lived secrets; store remaining secrets in GitHub environments with protection rules.
- Cache dependencies; fail fast; keep workflows reusable (`workflow_call`) instead of copy-pasting.
- Deployments are automated, repeatable and reversible; production requires an approval on the environment.

## Dependencies
- Add a dependency only when it clearly beats writing the code; prefer well-maintained, widely used, permissively licensed packages. Check the license before adding.
- Keep dependencies current with Dependabot (or Renovate); review and merge updates regularly.
- Remove unused dependencies.

## Security
- Treat all input as untrusted: validate, encode output, use parameterized queries, never build commands or queries from strings.
- Secrets come from a secret manager or environment variables; never from code, config files in the repository, logs or error messages. Provide a `.env.example` without real values; `.env` is git-ignored.
- Enable GitHub secret scanning with push protection, Dependabot alerts and code scanning (CodeQL) on the repository.
- Principle of least privilege for service accounts, tokens, database users and CI permissions.
- Follow `SECURITY.md` for reporting vulnerabilities.

## Configuration and logging
- Configuration via environment variables or config files per environment; validate configuration at startup and fail fast.
- Structured logging (JSON in production) with levels, timestamps in UTC (ISO 8601) and a correlation ID; never log secrets or personal data.
- Expose health checks and metrics for services; use OpenTelemetry for traces, metrics and logs where the stack supports it.

## Documentation
- Public APIs, modules and non-obvious code have doc comments in the language's standard format.
- Comments explain *why*, not *what*; delete commented-out code.
- Record significant technical decisions as ADRs.
- Keep `CHANGELOG.md` in the Keep a Changelog format; version releases with Semantic Versioning.

## Accessibility and internationalization
- User interfaces meet WCAG 2.2 AA: semantic markup, keyboard access, sufficient contrast, labels and alt texts.
- No hardcoded user-facing strings where the project supports multiple languages; use the project's localization mechanism.
- Store dates in UTC and format them for the user's locale and time zone only at display time.

## Code review focus
Flag in this order: security issues and secrets, missing or failing tests, breaking changes without documentation, unpinned tools or actions, over-broad permissions, unrelated changes mixed into the pull request, missing documentation or changelog updates.

## Naming conventions
Use consistent naming conventions for files, variables, functions, classes, and other identifiers throughout the project. Follow the language-specific style guides and established patterns to improve readability and maintainability.

## File headers
- Every source file should have a header that includes a brief description, and any relevant copyright or license information.
- Follow the project's standard format for file headers consistently across all files.
