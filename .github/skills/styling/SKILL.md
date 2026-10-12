---
name: styling
description: >
  Making client-side user interface corporate identity compliant. Styling should adhere to the Ricola brand guidelines.
---

# Client Skill

## When to use
* if it is a HTML based client-side application or a fat client application.

## Steps
1. Understand the client side framework used in the application.
2. Identify the UI components and their styling requirements.
3. Apply the Ricola brand guidelines to the UI elements whenever in global way possible.
4. If there is no root page or entry page defined, follow these substeps:
    1. If it is HTML based use this template as the main layout for the application and make it center aligned.
```html
<body>
    <div class="container">
      <div class="logo">
          <img src="/static/logo.svg" alt="Ricola">
      </div>
      <h1>Welcome to Ricola Metadata Service</h1>
      <p>Manage your data platform metadata with ease</p>
      <a href="/auth/login" class="btn">Login with Ricola SSO</a>
    </div>

    <footer>
      © 2026 Ricola Group AG. All rights reserved.
    </footer>
</body>
```
Use as background for the body "background: linear-gradient(135deg, var(--meadow) 0%, var(--forest) 100%);".
    2. If it is a fat client application, make a styled main screen.

## Details
All digital assets are in the main folder assets/.

### Ricola Color Palette
Use those colors for all HTML pages and UI elements.

#### Main colors
- Sunglow #FED33C
- Meadow #286140
- Forest #00281F
- Cloud #F9F9F4

##### dark
Forest #00281F
Dark Neutral 01 #203D32
Dark Neutral 02 #2D3F38
Dark Neutral 03 #747A73

##### light
Light Neutral 01 #D5D1C9
Light Neutral 02 #E1DFD8
Light Neutral 03 #EDECE6
Cloud #F9F9F4

#### Extra colors
##### Bloom
- Bloom #EBA1C6
- Bloom tint 02 #F1B6D2
- Bloom tint 03 #F7CBDD

##### Sunrise
- Sunrise #F7A93B
- Sunrise tint 02 #FFC66A
- Sunrise tint 03 #FFDA9D

##### Sunglow
- Sunglow #FED33C
- Sunglow tint 02 #FFDE6E
- Sunglow tint 03 #FFEDB0

##### Sky
- Sky #54B4D2
- Sky tint 02 #86C8DC
- Sky tint 03 #B7DCE5

##### Moss
- Moss #86C75F
- Moss 02 #A4D886
- Moss tint 03 #C3E8AC

##### Meadow
- Meadow #286140
- Meadow tint 02 #538166
- Meadow tint 03 #93B09F


### Ricola Font
The standard Ricola font is "Ricola Regular". Use the provided Ricola-Regular.woff file as font in the web service. Use this font for all HTML pages.
Use appropriate font fallbacks for browsers that do not support woff.


Use this skill when styling client-side applications:
- HTML based (CSS, JavaScript, Typescript, Alpine.js, React, Vue.js)
- Python RICH command line interface
- Avalonia UI fat clients
