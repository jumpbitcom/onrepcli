# Generic Features
- This app has no Single Sign On (SSO) and no user accounts.
- It just reads news from onlinereports.ch and displays them natively, without an embedded browser.
- It is a client-only desktop application (Avalonia, .NET 10) for Windows, macOS and Linux; there is no backend.
- The whole interface is available in German, French and English. German is the default; the choice is not saved between launches.
- Saves the full articles and their main images locally, so previously downloaded reporting remains available offline.
- Uses the Jumpbit theme assets (colors, Inter font, styles) and follows the operating system's light or dark appearance.
- Uses the Jumpbit app icon: `.ico` for the Windows executable and window, `.icns` for the macOS app bundle. On macOS the Dock icon is also set at startup, so it shows when the app is started with `dotnet run`. The `.icns` uses the standard macOS icon margin and is generated with `scripts/build-macos-icon.sh`.

## App main layout
It has 3 parts: the top row is the header, the left side (fixed width, about a quarter of the window) shows the sections, and the right side is the body.

### Header
- No logo: shows "Online Report" in bold and the tagline "Macht bekannt." in small, non-bold letters.
- An update button ("Aktualisieren") is always visible. It is disabled while an update is running.
- A small round "i" info button sits next to the update button. Clicking it opens a popup with the date and time of the last update and how many feed articles were loaded.
- A language drop-down (Deutsch, Français, English) with rounded edges, styled like the other buttons.
- On start, the app loads the local archive and then runs an update before showing the main page. While this runs, a full-page "Wird aktualisiert…" message is shown.

### Left side
- Title "Artikel" above the section menu.
- Sections, in this order: Top News (selected on start), News, Politics, Economy, Culture & Society, Columns, All articles.
- Each section is a single rounded box with simple highlighting (light on hover, dark when selected); there are no buttons or nested boxes inside.
- Articles are assigned to sections by their onlinereports.ch categories (e.g. Politik, Wirtschaft, Kultur & Gesellschaft, Kolumnen / Das Wort zum Bild).
- At the bottom, update errors and warnings are shown, for example how many articles could not be loaded and the reason for the first one.

### Right side - Main Page
- Shows the articles of the selected section as article summary boxes, newest first.
- Top News shows only the 3 newest articles across all sections.
- News, Politics, Economy, Culture & Society and Columns show all their saved articles; All articles shows every saved article. The list scrolls if needed.
- Each summary box shows the title, publication date and time, the summary (up to 3 lines) and the category tags.
- Articles that were added by the latest update are marked with a small green circle at the top right of their summary box.
- No update information is shown here, just the articles. A message is shown only when a section has no articles.

### Right side - Detailed news page
Clicking a summary box opens a native detail page for that article:
- A "Back" button ("Zurück zur Übersicht") at the top returns to the section list.
- The article title, with the article's main image directly below it. If the article has several images, the first one is used. There is no manual "load image" action; images are downloaded during updates.
- Publication time, author and sections.
- The full article text. If the full text is not available, the summary is shown instead, or a note that the full text is not available offline.
- Media links (e.g. videos) from the article, if any, open in the default browser.
- At the bottom, a button opens the original article on onlinereports.ch in the default browser.

## Fetching of articles and offline feature
- The public feed is fetched from `https://onlinereports.ch/api/rss-feed`.
- Each update refreshes the feed and fetches the article page (full text, author, main image, media links) only for articles that are not yet saved with full text. Articles already saved are not downloaded again.
- Each update also downloads the main image for any saved article in the current feed that does not have one yet (for example after a failed download).
- Article and image requests run in parallel with a limit of 3 at a time.
- Images are only downloaded from the OnlineReports media server over HTTPS, as JPEG, PNG, GIF or WebP, up to 10 MB each.
- The archive is stored in the user's local application data folder under `OnlineReports/` (`articles.json` and an `images/` folder). Articles that drop out of the feed stay in the archive.
- Offline, or when an update fails, the previously saved articles and images remain available, and the error is shown at the bottom of the left side.
