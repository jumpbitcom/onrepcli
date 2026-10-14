# Generic Features
This app has no Single Sign On (SSO).
It just reads news from onlinereports.ch and displays them.
It is a full client only application.
The whole app is available as German, French and English


- Shows the three newest stories for the selected OnlineReports section.
- Sections: all stories, News, Politics, Economy, Culture & Society, and Columns.
- Refreshes the RSS feed and fetches full text only for articles not already in the local archive. Article requests run in parallel with a small concurrency limit.
- Opens a native article view with the full text, author, publication time, sections, and media links.
- Does not download article images during feed updates. Readers can request the article image from its detail view; video links open only when selected.
- Includes a German, French, and English interface. The selected language and section are saved locally.
- Saves the latest feed and full article archive locally so previously downloaded reporting remains available offline.
- Uses the Jumpbit theme assets and follows the operating system's light or dark appearance.
- Does not include sign-in or SSO.

The public feed is fetched from `https://onlinereports.ch/api/rss-feed`. The local article archive is stored in the user's application data folder. In offline mode, only previously downloaded feed entries and article details are available.

## Main Page
See (Main Page)[page-start.md]

## Detailed news page
Per news item show a detailed news page with all information from the selected news articel. Load images or videos on demand.