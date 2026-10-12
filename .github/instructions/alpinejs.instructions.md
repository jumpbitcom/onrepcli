---
applyTo: "**/*.html,**/*.astro,**/*.php,**/*.twig,**/*.njk,src/alpine/**/*.ts"
---

# Alpine.js Guidelines
https://alpinejs.dev

## Purpose
Rules for building and reviewing Alpine.js components. Target: **Alpine.js 3.x** (latest 3.17), installed via npm and bundled, written in TypeScript.

**All TypeScript rules apply:** follow [typescript.instructions.md](./typescript.instructions.md) for every `.ts` file (strict tsconfig, no `any`, `import type`, named exports, runtime validation of external data). This file only adds Alpine-specific rules; where both files cover the same topic, this file wins for Alpine code.

## When to use Alpine
- Use Alpine for small, local interactivity on server-rendered or static HTML: toggles, dropdowns, tabs, modals, form helpers, simple filtering.
- Don't build a single-page application or complex client state with Alpine; use a component framework for that.
- In Astro projects, prefer an Alpine `<script>` or `@astrojs/alpinejs` over hydrating a framework island for simple interactions.

## Setup
- Install with npm (`alpinejs`, official plugins as `@alpinejs/*`) and type definitions (`@types/alpinejs`). No CDN `<script>` tags in production code.
- Single entry point (e.g. `src/alpine/main.ts`) that registers plugins, components, stores and directives, then calls `Alpine.start()` exactly once:
  ```ts
  import Alpine from "alpinejs";
  import focus from "@alpinejs/focus";
  import { dropdown } from "./components/dropdown";

  Alpine.plugin(focus);
  Alpine.data("dropdown", dropdown);
  Alpine.start();
  ```
- Set `window.Alpine = Alpine` only in development, declared in a global `.d.ts` file.
- Use the official plugins (`focus`, `persist`, `intersect`, `collapse`, `mask`, `morph`, `anchor`, `sort`, `resize`) before writing custom code.

## Components
- Define components as typed factory functions in their own `.ts` files and register them with `Alpine.data()`. Keep only trivial state inline in HTML (`x-data="{ open: false }"`).
- Type component state and methods with an interface; use `ThisType` so `this` is typed inside methods:
  ```ts
  import type { AlpineComponent } from "alpinejs";

  interface DropdownState {
    open: boolean;
    toggle(): void;
    close(): void;
  }

  export function dropdown(initialOpen = false): AlpineComponent<DropdownState> {
    return {
      open: initialOpen,
      toggle() {
        this.open = !this.open;
      },
      close() {
        this.open = false;
      },
    };
  }
  ```
- Pass initial data from the server as factory arguments (`x-data="dropdown(true)"`) or via `data-*` attributes read in `init()`; validate it before use.
- Use `init()` for setup and `destroy()` to remove listeners, timers and observers.
- Keep components small and focused; one responsibility per component. Share logic through plain TypeScript functions, not deeply nested `x-data` scopes.
- Use `Alpine.store()` only for truly global state (theme, auth user, cart); type every store and access it through a typed helper, not raw `$store` strings scattered in templates.

## Templates and directives
- Keep expressions in HTML short: call a method (`@click="toggle()"`) instead of writing logic in attributes. No multi-statement expressions in templates.
- Prefer `@event` and `:attr` shorthands consistently.
- Always add a unique `:key` to `x-for`, and put `x-for` and `x-if` on a `<template>` element with a single root element.
- Use `x-show` for frequent toggling and `x-if` when the content is expensive or must not exist in the DOM.
- Add `x-cloak` to elements hidden until Alpine initializes, with `[x-cloak] { display: none !important; }` in the CSS.
- Use `x-text` for text output. Use `x-html` only with sanitized, trusted content.
- Use `x-model` modifiers (`.number`, `.trim`, `.debounce`, `.lazy`) instead of manual conversion.
- Use `x-ref` / `$refs` instead of `document.querySelector` inside components; use `$dispatch` for communication between components instead of global variables.
- Use event modifiers (`.prevent`, `.stop`, `.outside`, `.window`, `.debounce`, `.throttle`) instead of manual event handling.
- Custom directives and magics are registered in the entry point with `Alpine.directive()` / `Alpine.magic()` and typed.

## Accessibility
- Interactive elements are real `<button>`, `<a>` or form controls, never clickable `<div>`s.
- Keep ARIA state in sync with component state (`:aria-expanded="open"`, `:aria-controls`, `aria-hidden`).
- Modals and menus trap and restore focus with the Focus plugin (`x-trap`), close on `Escape` (`@keydown.escape`) and on outside click (`@click.outside`).
- Respect `prefers-reduced-motion` for `x-transition` animations.
- Everything works with keyboard only; content must remain usable when JavaScript fails where possible (progressive enhancement).

## Security
- Never use `x-html` with user input or API data that hasn't been sanitized (e.g. DOMPurify).
- Don't put secrets or sensitive data in `x-data`, `data-*` attributes or stores; everything there is visible in the browser.
- When the site uses a strict Content Security Policy without `unsafe-eval`, use the CSP build (`@alpinejs/csp`) and register all components with `Alpine.data()`; inline expressions are then limited, so keep logic in TypeScript.
- Validate server-provided JSON with a schema (per the TypeScript instructions) before putting it into component state.

## Performance
- Load Alpine with `defer` (or as a module) and initialize once; don't re-initialize on the same DOM.
- Avoid large reactive objects; keep non-reactive data (big lists, class instances) outside Alpine state or wrap with `Alpine.raw()`.
- Debounce input-driven work; use the Intersect plugin to lazy-load content.
- When swapping HTML (htmx, Livewire, Astro view transitions), use `Alpine.initTree()` / `Alpine.destroyTree()` or the Morph plugin so components are set up and cleaned correctly.

## Testing
- Unit test component factories as plain TypeScript objects with Vitest (no DOM needed for pure logic).
- Test DOM behavior with Vitest and `happy-dom`/`jsdom`, or end-to-end with Playwright, including keyboard navigation and ARIA attributes.

## Code review focus
Flag in this order: `x-html` with untrusted data, secrets in the DOM, logic-heavy inline expressions, untyped components or stores, missing `:key` in `x-for`, missing `x-cloak`, inaccessible interactive elements or missing focus management, CDN script tags, violations of the TypeScript instructions.
