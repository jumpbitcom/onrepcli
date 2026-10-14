---
name: styling
description: >
  Making client-side user interface jumpbit.com corporate identity compliant.
---

# Styling Skill

## When to use
* if it is a HTML based client-side application or a fat client application.

## Source of truth
The **Jump Bit design system** (https://claude.ai/artifact/FchxsJbujLEuHSzFKx4VK8) defines the identity. The files in `assets/` implement it:
- `jumpbit-tokens.json` — colour, type and radius tokens.
- `jumpbit-theme.css` — tokens as CSS custom properties plus the base, `.btn`, `.card`, `.text-*` and `.landing` styles for HTML clients.
- `jumpbit-theme.axaml` and `jumpbit-styles.axaml` — the same tokens and styles for Avalonia.

Use these files; don't redefine colours, fonts or radii in the application.

## Steps
1. Understand the client side framework used in the application.
2. Identify the UI components and their styling requirements.
3. Apply the Jump Bit design system globally: include `jumpbit-theme.css` (HTML) or merge the Avalonia theme files, and style through the semantic tokens.
4. If there is no root page or entry page defined, follow these substeps:
    1. If it is HTML based, use the landing layout as the entry page. It is a centred column on the brand gradient (`--gradient-brand`) with the white logo:
```html
<body>
    <main class="landing">
      <div class="logo">
          <img src="/assets/jumpbitlogoweiss.png" alt="jumpbit.com">
      </div>
      <h1>Welcome to jumpbit.com Service</h1>
      <p>Purpose of the jumpbit.com Service</p>
      <a href="/auth/login" class="btn">Login with Provider</a>
      <footer>
        © 2026 jumpbit.com. All rights reserved.
      </footer>
    </main>
</body>
```
    2. If it is a fat client application, make a styled main screen (`Border.landing` uses the brand gradient).

## Identity rules
- Name: **Jump Bit** (two words) in prose and app names; **jumpbit.com** in service copy and footers. Tagline **Empower your IT**, uppercase and widely tracked, only beside the logo or on a landing page. Plain, factual English; no emoji.
- Colour: monochrome (ink, charcoal, greys, white). Always use the semantic `--color-*` tokens, never the `--jb-*` palette directly, so light and dark themes both work.
- Primary actions: `.btn` (fill `--color-primary`, label `--color-on-primary`); `.btn .btn-outline` for secondary actions.
- `--color-accent` (gold) is optional and rare: at most one highlight per view.
- Status: `.text-success`, `.text-warning`, `.text-danger`, `.text-info`, always with a word ("Saved", "Error"), never colour alone.
- Shape: `--radius-md` (8px) for buttons and inputs, `--radius-lg` (14px) for cards, `--radius-sm` (4px) for small elements. Cards use `--shadow-md` with a `--color-border` hairline.
- Focus: a solid 2px `--color-focus` outline with 2px offset on every focusable element.
- Content pages use `.card` on `--color-surface`.

## Logo and icons
All digital assets are in the main folder `assets/`.
- `jumpbitlogo.png` (black) on light grounds, `jumpbitlogoweiss.png` (white) on dark grounds or the brand gradient, `jumpbitlogo-inv.png` where the ground is uncontrolled.
- `jumpbit-mark.svg` is the mark alone; `favicon.svg`, the `favicon-*.png`, `icon-*.png` and `site.webmanifest` are the web and app icons.
- Never redraw, recolour or stretch the logo. No icon library is part of the system.

## Font
The only typeface is **Inter** (https://fonts.google.com/specimen/Inter) for headings, body text and buttons, with Arial, sans-serif as fallback. Code uses the mono stack (`--font-mono`).

- Headings: weight 600, letter-spacing 0.02em, line-height 1.15.
- Button labels: weight 600, uppercase, letter-spacing 0.08em.
- Body: 1rem, line-height 1.5.

`jumpbit-theme.css` loads Inter from the variable fonts in `assets/font/` (weights 100–900, upright and italic). Serve the `font/` folder next to the CSS. Only if the font files can't be served, embed Inter from Google Fonts in the HTML head instead:
```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:ital,opsz,wght@0,14..32,100..900;1,14..32,100..900&display=swap" rel="stylesheet">
```

## Python Rich command line interfaces
Keep the terminal monochrome: an `accent` style of `bold` (no hue), and terminal-named colours for status so they stay readable on light and dark terminals:
```python
THEME = Theme({"accent": "bold", "success": "bold green", "warning": "yellow", "error": "bold red", "info": "blue"})
```

Use this skill when styling client-side applications:
- HTML based (CSS, JavaScript, Typescript, Alpine.js, React, Vue.js)
- Python RICH command line interface
- Avalonia UI fat clients
