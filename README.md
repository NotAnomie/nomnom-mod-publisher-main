# Anomie NOMNOM Publisher

A small Windows desktop tool for preparing NOMNOM mod submissions without doing the same GitHub and JSON steps by hand every time.

It does **not** approve, merge, or bypass NOMNOM review.  
It only helps create proper releases, manifests, forks, commits, and pull requests so the submission is easier to check.

## What it does

Anomie NOMNOM Publisher is built around the normal NOMNOM contribution flow:

1. select a BepInEx plugin DLL
2. read the plugin metadata
3. build a release ZIP
4. upload the ZIP to GitHub Releases
5. generate a NOMNOM manifest
6. fork the target NOMNOM registry if needed
7. commit the manifest to the fork
8. open a pull request for review

The tool is meant for mod authors who already have a working mod and want to publish or update it cleanly.

## Main features

- GitHub sign-in through OAuth Device Flow
- no manual token copy/paste for normal use
- automatic GitHub account detection
- automatic per-mod release repository creation
- BepInEx metadata reading from DLLs
- release ZIP creation
- GitHub release upload
- release asset hash support
- NOMNOM manifest generation
- configurable PR target repository
- automatic fork sync/rebase before committing generated PR changes
- automatic fork and pull request creation
- mod browser for own releases and submitted manifests
- read-only official catalog browsing
- saved descriptions per mod ID
- catalog description pull by mod ID
- dependency, incompatibility, and add-on relation fields
- local settings storage

## What it does not do

This tool does **not**:

- merge pull requests
- approve submissions
- skip schema validation
- bypass human review
- verify that a mod is safe
- decide whether a mod is allowed in NOMNOM
- scan for malware
- hide the submitted manifest from reviewers

Every submitted manifest still goes through the normal NOMNOM pull request process.

## Requirements

- Windows
- .NET 8 SDK for building from source
- a GitHub account
- a GitHub OAuth App client ID
- a compiled BepInEx plugin DLL

For a portable release build, publish it as a self-contained Windows executable.

## GitHub OAuth setup

Create a GitHub OAuth App:

```text
GitHub
→ Settings
→ Developer settings
→ OAuth Apps
→ New OAuth App
```

Recommended fields:

```text
Application name:
Anomie NOMNOM Publisher

Homepage URL:
https://github.com/<your-name>/<your-repo>

Authorization callback URL:
http://localhost
```

Enable **Device Flow** in the OAuth App settings.

The tool only needs the OAuth **Client ID** for Device Flow.  
A client secret should not be placed inside the desktop app.

## GitHub permissions

For public repositories, the intended scopes are:

```text
public_repo read:user
```

Use broader access only if you intentionally want to work with private repositories.

## Building

From the project folder:

```powershell
dotnet restore
dotnet build -c Release
```

To create a single self-contained EXE:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

The published executable will be in:

```text
bin\Release\net8.0-windows\win-x64\publish\
```

Do not commit the published EXE to the source repository.

## Recommended `.gitignore`

Use a `.gitignore` similar to this:

```gitignore
.vs/
[Bb]in/
[Oo]bj/
publish/
*.user
*.suo
*.pdb
*.cache
*.log
*.tmp
*.exe
*.zip
```

The source repository should contain the source files, project file, models, and resources — not build output.

## Project files

The important source files are:

```text
Program.cs
AnomiePublisherForm.cs
GitHubClient.cs
GitHubAuthService.cs
ManifestBuilder.cs
ZipBuilder.cs
AppSettings.cs
Models/
Properties/
```

`Models/` is required if the project uses model classes from that folder.  
`Properties/` should also be committed when it exists and is part of the project.

## Basic workflow

### 1. Select the plugin DLL

Open the **Package** tab and choose your compiled BepInEx plugin DLL.

### 2. Read DLL metadata

Click **Read DLL metadata**.

The tool tries to read:

```csharp
[BepInPlugin("mod.id", "Display Name", "1.2.3")]
```

That becomes:

```text
Mod ID:       mod.id
Display Name: Display Name
Version:      1.2.3
Release Tag:  v1.2.3
```

The BepInPlugin version is preferred over the assembly version.

### 3. Check the manifest fields

Open the **Manifest** tab and check:

- Mod ID
- Display Name
- Description
- Tags
- Author
- Game Version
- Artifact Type
- Category
- Info URL

The manifest filename must match the mod ID:

```text
modManifests/<mod.id>.json
```

Example:

```json
"id": "anomie.cargorequest"
```

must be committed as:

```text
modManifests/anomie.cargorequest.json
```

### 4. Build the ZIP

Click **Build ZIP**.

NOMNOM artifacts should point to an archive such as a ZIP, not a loose DLL.

By default, the tool can place the plugin inside:

```text
BepInEx/plugins/
```

inside the release archive.

### 5. Upload the GitHub release

Click **Upload GitHub release**.

The tool creates or reuses the selected release tag and uploads the ZIP as a release asset.

Important: NOMNOM auto-update expects **one GitHub release repository per mod**. Do not put every mod release into one shared repository. The tool will generate names like `NOMNOM-anomie-cargorequest` so NOMNOM can treat that repository as one software package with multiple versions.

After upload, it stores:

- release download URL
- uploaded file name
- GitHub asset digest, if available
- local SHA-256 fallback, if needed

### 6. Generate the manifest

Click **Generate manifest**.

The artifact will include a hash field:

```json
"hash": "sha256:..."
```

The hash is taken from the GitHub release asset digest when available. If GitHub does not provide it, the tool calculates the SHA-256 hash from the local ZIP.

### 7. Fork and open PR

Click **Fork + open PR**.

The tool first syncs your fork from the configured target repository and resets the generated PR branch to the latest target branch before committing the manifest. This keeps generated pull requests based on the maintainer's current `main` instead of an older fork state.

After that, it commits the manifest into your fork and opens or reuses a pull request against the configured target repository.

Default target:

```text
KopterBuzz/NOMNOM
branch: main
folder: modManifests
```

The target owner/repository/branch are configurable for testing or future registry forks.

## Release repository rule

NOMNOM auto-update checks one GitHub repository, reads its releases, and treats newer release tags as newer artifacts for the same mod.

Because of that, every mod should have its **own** release repository.

Good:

```text
NotAnomie/NOMNOM-anomie-cargorequest
NotAnomie/NOMNOM-emi-shoshanas-spawntool
NotAnomie/NOMNOM-emi-shoshanas-maptargetselectguard
```

Bad:

```text
NotAnomie/Nomnom-Mod-Releases
```

A single shared release repository can still host differently named ZIP files, but NOMNOM auto-update has no concept of multiple different mods inside one repo.

## Updating a mod

For a normal update:

1. increase the version in your BepInPlugin attribute
2. rebuild the DLL
3. select the new DLL
4. read DLL metadata
5. build a new ZIP
6. upload a new GitHub release to that mod's own release repo
7. generate the updated manifest
8. open an update PR

Do not overwrite old versions unless you have a specific reason.

Good:

```text
v1.6.8
v1.6.9
v1.7.0
```

Avoid repeatedly replacing the asset under the same tag during normal public updates.

## Mod Browser

The Mod Browser is split so that official catalog entries are not treated as your own mods.

Typical sources:

- **My mods only**
- **My release repo**
- **My submitted manifests**
- **Official NOMNOM catalog**

Official catalog entries are read-only.  
They can be inspected, but the tool should not let you update or delete someone else's manifest as if it were yours.

## Saved descriptions

Descriptions are saved locally per mod ID.

Example:

```text
anomie.cargorequest
→ Anomie Cargo Request lets players call in AI-handled cargo logistics support...
```

When the same mod ID is loaded again, the description is restored automatically.

If no local description exists, the tool can try to pull the description from:

1. your NOMNOM fork
2. the configured PR target catalog

It searches by exact mod ID.

## Local settings

Settings are stored here:

```text
%APPDATA%\Anomie\NomnomPublisher\settings.json
```

Example:

```text
C:\Users\<you>\AppData\Roaming\Anomie\NomnomPublisher\settings.json
```

The GitHub access token is stored encrypted for the current Windows user.

Old test builds may have used:

```text
%APPDATA%\Anomie\NomnomPublisher\
```

That path can be deleted after confirming the new settings path works.

## Release ZIP output

By default, release ZIPs are created next to the selected DLL in:

```text
nomnom-release\
```

Example:

```text
C:\SteamLibrary\steamapps\common\Nuclear Option\BepInEx\plugins\nomnom-release\
```

## Notes for contributors

This project should stay strict about safety boundaries:

- never auto-merge
- never auto-approve
- never hide review-relevant data
- keep official catalog entries read-only
- validate that file names match mod IDs
- validate that download URLs match the configured release repository
- keep release repositories one-mod-only when auto-update fields are enabled
- prefer explicit user actions over silent destructive changes

The goal is to remove repetitive work, not to remove review.

## Troubleshooting

### GitHub says a file is over 100 MB

You probably tried to commit the published EXE.

Remove build output from Git and make sure `bin/`, `obj/`, and `publish/` are ignored.

### Asset already exists

A release asset with the same name already exists under that tag.

Use a new version tag for real updates, or delete the old asset manually if it was a test upload.

### Wrong mod data appears in the manifest

Reload the DLL metadata and check that the selected DLL is the one you intend to publish.

The tool should not copy descriptions, authors, relations, or repo data from unrelated catalog entries.

### Manifest validation fails because of the filename

The JSON filename must match the manifest ID.

Example:

```json
"id": "anomie.cargorequest"
```

must be:

```text
modManifests/anomie.cargorequest.json
```

### Version is wrong

Make sure your plugin has a proper BepInPlugin attribute:

```csharp
[BepInPlugin("anomie.cargorequest", "Anomie Cargo Request", "1.6.8")]
```

The version in the manifest must match the DLL metadata for plugin artifacts.

## Status

This tool is an independent helper for preparing NOMNOM submissions.  
It is not an official NOMNOM component unless the NOMNOM maintainers decide otherwise.
