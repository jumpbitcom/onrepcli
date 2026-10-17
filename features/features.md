# Generic Features
- This app has no Single Sign On (SSO).
- It just reads news from onlinereports.ch and displays them.
- It is a full client only application.
- The whole app is available as German, French and English
- Saves the latest feed and full article archive locally so previously downloaded reporting remains available offline.
- Uses the Jumpbit theme assets and follows the operating system's light or dark appearance.
- Make a jumpbit app icon for this app running on macos or windows.

## App main layout
It has 3 parts, top row is the header, left side is around 30% and show the sections, right side is the body.

### Header
It has no Logo, just show on the header "Online Report" in bold and "Macht bekannt." in small letters non bold.
Show always an update button. This button triggers the scan of new news articles. On pressing the udpate button it reads all news articel in the background with all details including the article image just below the articel titel. It validates what is already available and only saves new news articles. Mark new articles with a small green circle on the top right of the articel summary box.

Show a small "i" info icon (round form) next to the update button. If you click on it a popup will show the last sync date / time of the last update and how many articles were loaded.
On start of the application make by default an udpate before showing the main page and show a running update message while loading.
It has a dropbox button to select the language, start with German as default. Make this dropbox button similar to the normal button with rounded edges.

### Left side
- Sections: all stories, News, Politics, Economy, Culture & Society, and Columns. It has a menu showing the different rubrics of onlinereports.ch. Make the boxes with rounded edges. But no button inside.
- On the bottom show error or warning messages in regard to updates
- 

### Right side - Main Page
- Shows the three newest acrticel stories for the selected OnlineReports section as articel summary box.
- Do not show update information of update button! Just the articels.


### Right side - Detailed news page
Show a detailed news page with all information from the selected news articel.
- The main image / video is always the first picture of the articel just below the header of the article
- Offer to see full article within your default browser at the bottom of the article. Opens a native article view with the full text, author, publication time, sections, and media links.

## Fetching of articels and offline feature
The public feed is fetched from `https://onlinereports.ch/api/rss-feed`. The local article archive is stored in the user's application data folder. In offline mode, only previously downloaded feed entries and article details are available.
- Refreshes the RSS feed and fetches full text only for articles not already in the local archive. Article requests run in parallel with a small concurrency limit.
