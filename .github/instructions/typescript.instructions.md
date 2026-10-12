---
applyTo: "**/*.ts,**/*.tsx,**/*.mts,**/*.cts"
---

# TypeScript Guidelines
https://www.typescriptlang.org

## Purpose
Rules for writing and reviewing TypeScript in this repository. Target: **TypeScript 7.x** (native Go compiler, `tsc`). Apply to all `.ts`, `.tsx`, `.mts` and `.cts` files.

## Toolchain
- Use TypeScript 7 (`npm install -D typescript`). Run type checks with `npx tsc --noEmit`.
- Do not suggest `@typescript/native-preview` or `tsgo`; they were preview names. Nightlies use `typescript@next`.
- If a tool needs the compiler API (e.g. typescript-eslint, webpack loaders), keep TypeScript 6 side by side via the alias `"typescript": "npm:@typescript/typescript6@^6.0.2"` and `"@typescript/native": "npm:typescript@^7.0.2"`. Do not import the `typescript` API in new code until 7.1 ships a stable API.
- Vue, Svelte, Astro, MDX and Angular template checking still depend on TypeScript 6 for editor support.

## tsconfig rules
- `strict` is on by default; never disable it or individual strict flags.
- Also enable: `noUncheckedIndexedAccess`, `exactOptionalPropertyTypes`, `noImplicitOverride`, `noFallthroughCasesInSwitch`, `verbatimModuleSyntax`, `isolatedModules`.
- Set `rootDir` explicitly (e.g. `"./src"`); it now defaults to `./`.
- List global type packages explicitly in `types` (e.g. `["node"]`); it now defaults to `[]`.
- Module settings: Node apps/libraries → `"module": "nodenext"`; bundled apps → `"module": "esnext"` or `"preserve"` with `"moduleResolution": "bundler"`.
- Never use (removed, hard errors in 7.0): `target: es5`, `downlevelIteration`, `baseUrl`, `moduleResolution: node`/`node10`/`classic`, `module: amd`/`umd`/`systemjs`/`none`, `esModuleInterop: false`, `allowSyntheticDefaultImports: false`, `alwaysStrict: false`.
- `paths` entries are relative to the tsconfig location, not `baseUrl`.
- In CI with few cores, consider `--checkers 1` or `2`; keep the value identical across environments.

## Types
- No `any`. Use `unknown` and narrow with type guards, `in`, `instanceof` or schema validation.
- No non-null assertions (`!`) and no `as` casts except at validated boundaries; prefer `satisfies` to check literals without widening.
- Prefer `type` for unions/compositions, `interface` for extendable object contracts. Be consistent within a file.
- Model states with discriminated unions and handle them exhaustively (`default: assertNever(x)`).
- Use `readonly` properties and `ReadonlyArray<T>` / `readonly T[]` for data that must not mutate.
- Prefer `as const` objects or string-literal unions over `enum`. No `namespace` (and never the `module` keyword for namespaces).
- Annotate return types of exported functions. Let inference handle locals.
- Use utility types (`Pick`, `Omit`, `Partial`, `Record`, `Awaited`, `ReturnType`) instead of duplicating shapes.
- Validate all external input (HTTP, env vars, JSON, files) at runtime (e.g. Zod or Valibot) and derive the static type from the schema.

## Modules and imports
- ES modules only; no `require` / `module.exports` in `.ts` files.
- Use `import type` / `export type` for type-only imports (required by `verbatimModuleSyntax`).
- Import attributes use `with`, never `assert`: `import data from "./data.json" with { type: "json" };`
- With `nodenext`, include file extensions in relative imports (`./util.js`).
- Named exports only; no default exports.

## Code style
- `const` by default, `let` only when reassigned, never `var`.
- `async`/`await` over `.then()` chains. Never leave a floating promise; `await` it or mark it deliberately with `void`.
- Throw `Error` subclasses, never strings. Type `catch (err)` as `unknown` and narrow before use.
- Use `?.` and `??`; do not use `||` for defaults when `0`, `""` or `false` are valid values.
- Small, pure functions; early returns over deep nesting.
- Naming: `PascalCase` for types/classes/components, `camelCase` for variables/functions, `UPPER_SNAKE_CASE` for true constants. No `I` prefix on interfaces.
- Files: `kebab-case.ts`; tests next to source as `*.test.ts`.
- Use TSDoc (`/** ... */`) on exported APIs; explain *why*, not *what*.

## Testing
- Write tests with the project's test runner (Vitest by default). Test through public APIs.
- Use type-level tests (`expectTypeOf`) for exported generic utilities.
- Never weaken types in production code to make a test compile.

## Security
- Never hardcode secrets, tokens or credentials; read them from validated environment variables.
- No `eval`, `new Function` or dynamic code execution.
- Sanitize anything rendered as HTML; avoid `dangerouslySetInnerHTML` / `innerHTML` with user input.
- Use parameterized queries; never build SQL with string concatenation.

## Code review focus
When reviewing, flag in this order: `any` or unsafe casts, unhandled promises, missing input validation, non-exhaustive union handling, removed tsconfig options, missing return types on exports.
