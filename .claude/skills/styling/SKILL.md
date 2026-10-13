---
name: styling
description: >
  Making client-side user interface jumpbit.com corporate identity compliant.
---

# Client Skill

## When to use
* if it is a HTML based client-side application or a fat client application.

## Steps
1. Understand the client side framework used in the application.
2. Identify the UI components and their styling requirements.
3. Apply the jumpbit.com brand guidelines to the UI elements whenever in global way possible.
4. If there is no root page or entry page defined, follow these substeps:
    1. If it is HTML based use this template as the main layout for the application and make it center aligned.
```html
<body>
    <div class="container">
      <div class="logo">
          <img src="/static/logo.svg" alt="jumpbit.com">
      </div>
      <h1>Welcome to jumpbit.com Service</h1>
      <p>Purpose of the jumpbit.com Service</p>
      <a href="/auth/login" class="btn">Login with Provider</a>
    </div>

    <footer>
      © 2026 jumpbit.com. All rights reserved.
    </footer>
</body>
```
Use as background for the body "background: linear-gradient(135deg, var(--meadow) 0%, var(--forest) 100%);".
    2. If it is a fat client application, make a styled main screen.

## Details
All digital assets are in the main folder assets/.

## My main font
Main font is "Inter" https://fonts.google.com/specimen/Inter. As fall back Arial, sans-serif.

Embed font in your HTML head section using the following links:
```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:ital,opsz,wght@0,14..32,100..900;1,14..32,100..900&display=swap" rel="stylesheet">
```
or with import:
```html
<style>
@import url('https://fonts.googleapis.com/css2?family=Inter:ital,opsz,wght@0,14..32,100..900;1,14..32,100..900&display=swap');
</style>
```
or as a reusable CSS class:
```css
.inter-<uniquifier> {
  font-family: "Inter", sans-serif;
  font-optical-sizing: auto;
  font-weight: <weight>;
  font-style: normal;
}
```

Use this skill when styling client-side applications:
- HTML based (CSS, JavaScript, Typescript, Alpine.js, React, Vue.js)
- Python RICH command line interface
- Avalonia UI fat clients
