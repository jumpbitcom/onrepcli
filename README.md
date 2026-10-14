# OnlineReports Client

OnlineReports is a Basel-based local news provider. This cross-platform native desktop client reads its public feed and article pages without embedding a browser. Version 2 is an Avalonia desktop app for Windows, macOS and Linux.

## Features
The product brief is in [features/features.md](features/features.md). The client displays the three newest stories in each section, reads article text natively, and keeps downloaded articles available offline. Feed updates do not download images; readers can load each article image on demand. German is the default interface language, with French and English also available. The app follows the operating system's light or dark appearance.

The public RSS feed is `https://onlinereports.ch/api/rss-feed`. Article metadata and full text are stored under the user's local application data folder in `OnlineReports/articles.json`.

## Run

Prerequisite: .NET 10 SDK.

```sh
dotnet run --project src/OnlineReports.Client
```

The first launch updates the feed before showing the stories. If the feed is unavailable, previously archived stories remain available and the sync status explains the problem.

## Test

```sh
dotnet test tests/OnlineReports.Client.Tests
```

