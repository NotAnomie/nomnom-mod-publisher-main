# Anomie UI - NOMNOM Publisher

## Visual Studio template
Use this project type:

```text
C#
Windows Forms App
.NET 8.0
```

Do not use:

```text
Windows Forms App (.NET Framework)
WPF Application
Console App
Class Library
```

## Build EXE
Open PowerShell in the project folder and run:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Output:

```text
bin\Release\net8.0-windows\win-x64\publish\AnomieNomnomPublisher.exe
```

## GitHub authorization setup
Create one GitHub OAuth App:

```text
Application name: Anomie UI NOMNOM Publisher
Homepage URL: https://github.com
Authorization callback URL: http://localhost
Device Flow: enabled
```

Paste only the OAuth App Client ID into Anomie UI. The desktop app does not need a client secret.

Requested scopes:

```text
repo read:user
```

This lets the app detect your GitHub user, create/fork repositories, create releases, upload ZIP assets, commit manifests, open pull requests, and manage releases in your own release repo.

## Main classes

```text
Program.cs                  App entry point
AmoniePublisherForm.cs       Main Anomie UI, tabs, mod browser, publish/update/delete flow
AmonieTheme.cs               Custom dark HUD-like controls, buttons, panels and header
GitHubAuthService.cs         Sign in with GitHub through OAuth Device Flow
GitHubClient.cs              GitHub REST API client: user, repo, release, asset, fork, contents, PR
ZipBuilder.cs                DLL metadata reader and NOMNOM release ZIP builder
ManifestBuilder.cs           NOMNOM manifest generator and validation
AppSettings.cs               Saved settings and encrypted local token storage
Models/GitHubModels.cs       OAuth, GitHub and Mod Browser data models
Models/ManifestModels.cs     NOMNOM manifest input and relation models
```

## Main workflow

```text
1. GitHub Access tab: paste OAuth Client ID once
2. Click Sign in with GitHub
3. Package tab: select plugin DLL
4. Read DLL metadata
5. Build release ZIP
6. Upload GitHub release
7. Generate manifest
8. Fork NOMNOM and open PR
```

## Mod Browser workflow

```text
1. Open Mod Browser
2. Choose source: All sources, My Release Repo, My NOMNOM Fork, or Upstream NOMNOM
3. Click Refresh
4. Select a row
5. Load Selected to edit metadata again
6. Update Selected to republish using the current DLL/ZIP fields
7. Delete Release to remove a GitHub release from your release repo
8. Delete Manifest PR to open a pull request that removes a manifest from NOMNOM
```

## NOMNOM notes

The app builds archive assets because NOMNOM artifacts expect downloadable content files to be archives such as ZIP/RAR/7z. For plugins, the manifest version should match the DLL metadata version.
