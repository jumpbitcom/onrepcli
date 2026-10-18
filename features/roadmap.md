I didn't change anything in the repo. Here's what each target would need, based on the current code: an Avalonia 12 / .NET 10 desktop app with one project, `MainWindow` at a minimum width of 900, and a JSON archive under `LocalApplicationData`.

## Where the repo stands now
- `dotnet publish` already gives you a self-contained build. On macOS, a custom MSBuild target already wraps it in an `OnlineReports.app` bundle ([OnlineReports.Client.csproj:38-60](src/OnlineReports.Client/OnlineReports.Client.csproj#L38-L60)).
- Icons already exist (`.ico`, `.icns`, a 1024px PNG).
- There's no installer, no code signing, no single version number, and no build/release CI. The only workflow is the UAT notifier.

---

## 1. Windows 11 installer

| Need | Details |
|---|---|
| **Publish per CPU type** | `dotnet publish -c Release -r win-x64 --self-contained` (plus `win-arm64` for ARM devices). Optional: `-p:PublishSingleFile=true`. |
| **Installer tool** | Pick one: **Velopack** (my recommendation; see below), **MSIX** (needed for the Microsoft Store; clean install/uninstall), **Inno Setup** (simple `Setup.exe`), or **WiX v5+** (MSI files that companies can deploy centrally). |
| **Code signing** | Without an Authenticode signature, SmartScreen shows "Unknown publisher". Cheapest option: **Azure Trusted Signing**. Otherwise an OV/EV certificate. Sign the `.exe`/`.dll` files *and* the installer. |
| **App metadata** | Add `Version`, `Company`, `Product`, `Copyright` to the csproj so the file properties and "Installed apps" entry look right. In [app.manifest](src/OnlineReports.Client/app.manifest), the Windows 10 GUID also covers Windows 11, so that part is fine. Update the leftover template comments. |
| **Install scope** | Install per user under `%LocalAppData%\Programs\OnlineReports`, so no admin rights are needed. Add a Start-menu shortcut. Decide whether uninstalling also deletes the `OnlineReports/articles.json` archive. |

## 2. macOS installer (Sonoma/Sequoia/Tahoe)

| Need | Details |
|---|---|
| **Builds** | `osx-arm64` and `osx-x64`. Either ship two downloads or merge them into one universal `.app` with `lipo`. |
| **Apple Developer Program** | 99 USD/year. Needed for a **Developer ID Application** certificate, plus **Developer ID Installer** if you ship a `.pkg`. |
| **Code signing** | Sign every `.dylib` and the executable inside `Contents/MacOS`, then the bundle. Use `codesign --options runtime` (hardened runtime) with entitlements .NET needs: `com.apple.security.cs.allow-jit` and usually `allow-unsigned-executable-memory`. |
| **Notarization** | Run `xcrun notarytool submit … --wait`, then `xcrun stapler staple`. Without this, Gatekeeper blocks the app. |
| **Package** | A **DMG** (`hdiutil` or `create-dmg`, with "drag to Applications") or a **PKG** (`pkgbuild`/`productbuild`). Sign and notarize the DMG/PKG too. |
| **Info.plist cleanup** | [Info.plist](src/OnlineReports.Client/Assets/Info.plist) is missing `NSHighResolutionCapable`, `NSPrincipalClass`, `LSApplicationCategoryType` (news), `NSHumanReadableCopyright`, and `NSRequiresAquaSystemAppearance=false`. `LSMinimumSystemVersion` is set to `11.0`, which is below what .NET 10 supports; raise it to match .NET 10's supported macOS versions. The version strings should come from the same source as the csproj. |
| **Optional: Mac App Store** | Needs the App Sandbox plus the `network.client` entitlement, and different certificates. The archive path moves into the sandbox container automatically. |

## Applies to both desktop platforms
- **Velopack** fits this repo best: one tool that builds `Setup.exe` on Windows and a `.pkg` on macOS, with built-in signing/notarization flags and **auto-updates**. It adds a NuGet package and a few lines in `Program.Main`.
- **GitHub Actions release workflow**: a `windows-latest` + `macos-latest` matrix that runs tests, publishes, signs, notarizes, packages, and uploads to a GitHub Release. Use OIDC for Azure Trusted Signing. The Apple certificate (.p12), its password, and an App Store Connect API key go in GitHub secrets. Per the repo rules, note that as a TODO in the README.
- One version number in the csproj, used for the installers, Info.plist, and CHANGELOG.
- README sections for "Build installers" and "Release".

---

## 3. iOS and Android apps

Avalonia also targets **iOS and Android**, so the models, services, and view models can be reused. The UI and project layout have to change, though.

### Changes in the code (both platforms)
1. **Split into several projects:**
   - `OnlineReports.Client` (shared library: Models, Services, ViewModels, Views, Localization)
   - `OnlineReports.Desktop` (the current `Program.cs`, `app.manifest`, macOS bundle target, `MacDockIcon`)
   - `OnlineReports.Android` (`net10.0-android`)
   - `OnlineReports.iOS` (`net10.0-ios`)

   The test project would then reference the shared library.
2. **Window → view:** phones have no windows. Move the content of `MainWindow.axaml` into a `MainView` UserControl. `App.OnFrameworkInitializationCompleted` would also handle `ISingleViewApplicationLifetime` (`MainView = …`), not only the desktop lifetime.
3. **Responsive layout:** the current layout assumes `MinWidth="900"` with a side menu. On phones it needs a hamburger/`SplitView` or bottom tabs, a list → detail navigation, a back-button/swipe-back handler, touch targets of at least 44pt/48dp, and safe-area insets for the notch and gesture bar.
4. **Trimming/AOT:** iOS doesn't allow JIT and Android release builds trim code. `NewsService` uses reflection-based `JsonSerializer`, so it needs a `JsonSerializerContext` source generator. Compiled bindings (`x:DataType`) are already in use, which helps.
5. **Platform-specific code:** compile `MacDockIcon` only for desktop. The archive path via `LocalApplicationData` works in both sandboxes, but the OS may delete cached images, so consider a separate cache folder.

### Android
- `dotnet workload install android`, plus the Android SDK and a JDK.
- `MainActivity`, `AndroidManifest.xml` with the `INTERNET` permission, application ID `ch.onlinereports.client`, and a target API level that meets Google Play's current minimum.
- Adaptive launcher icons (mipmap foreground/background) and a splash screen.
- A release keystore (kept out of the repo, stored in CI secrets), built as an **AAB**.
- A Google Play Console account (25 USD one-time), privacy policy, data-safety form, store listing and screenshots. New personal accounts must run closed testing before going public.

### iOS
- **You need a Mac with Xcode.** Run `dotnet workload install ios`.
- `AppDelegate`, its own `Info.plist`, an asset catalog for app icons (1024px base), a launch screen, and a **privacy manifest** (`PrivacyInfo.xcprivacy`).
- Apple Developer Program (same 99 USD/year as macOS), an App ID, distribution certificate and provisioning profile.
- App Store Connect: TestFlight, then App Review.

### Content rights (store review)
The app republishes onlinereports.ch content. Apple (guidelines 5.2 and 4.2) and Google both reject apps that show third-party content without permission or that are "just a website/feed wrapper". Get **written permission from OnlineReports** and publish under their account or with their approval. Also make sure the app offers real native features, such as offline reading, which it already has.

---

### Effort, roughly
| Target | Effort |
|---|---|
| Windows + macOS installers with signing & CI | Small–medium: mostly tooling and certificates, little code |
| Android | Medium: project split + mobile layout |
| iOS | Medium, building on the Android work, plus Apple signing/review overhead |

The order I'd suggest: desktop installers first with Velopack, then the project split, then Android (faster to iterate), then iOS.