# Online Reports Client

A cross-platform desktop client for the Basel news provider [Online Reports](https://www.onlinereports.ch). It displays the three latest stories and the site's rubrics in a native Avalonia app.

## Requirements

- .NET 10 SDK
- Windows, macOS, or Linux

## Run

```sh
dotnet run --project src/OnRepCli
```

The client loads the Online Reports homepage on launch. Use the rubrics list to browse a section and **Refresh** to fetch its latest headlines. Headlines are cached locally so the last successfully loaded stories remain available when the site is offline.

## Test

```sh
dotnet test tests/OnRepCli.Tests
```

The news client reads the site's rendered HTML because the project does not specify a public feed or API. If the site changes its page structure, the page parser may need updating.



