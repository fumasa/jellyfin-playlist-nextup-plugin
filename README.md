# Playlist/Collection Next Up (Jellyfin plugin) — scaffold

**Goal (v1):** Make *mixed* Playlists and Collections behave like a “series-like continue item” by tracking playback progress and exposing a **Resume** entry that clients can use (Web UI, custom JS, home-sections, etc.).

This repo is an **initial working scaffold** for Jellyfin **10.10.x** (you mentioned 10.10.3).
It includes:
- Playback event monitoring (`ISessionManager` events)
- Persistent progress store (simple JSON file store for now)
- REST API endpoints to query “resume” targets
- A Jellyfin Dashboard configuration page (embedded HTML)

> Note: Making a playlist appear in native **Next Up** across all official clients would likely require deeper server/client changes.
> This plugin focuses on delivering the “A” approach we discussed: a **Continue/Resume** item that can be surfaced on the home screen by UI customizations or other plugins.

## Build

Prereqs:
- .NET SDK 8.x
- Jellyfin 10.10.x dev references come from NuGet packages used in this project.

```bash
dotnet restore
dotnet build -c Release
```

Output DLL(s) will be in:
`Jellyfin.Plugin.PlaylistNextUp/bin/Release/net8.0/`

Install:
- Copy the built plugin folder (DLL + deps) into Jellyfin's plugins folder.
- Restart Jellyfin.

## Configuration

Dashboard → Plugins → Playlist/Collection Next Up

Options:
- Enable plugin
- Track Playlists / Track Collections
- Collection ordering (Release date, PremiereDate, SortName, etc.)
- Thresholds for when we consider something “in progress”
- Enable debug logs

## API quick test

After enabling the plugin, try:

- `GET /PlaylistNextUp/Resume?userId=<USER_GUID>`
- `GET /PlaylistNextUp/Resume/playlist/<PLAYLIST_ID>?userId=<USER_GUID>`
- `GET /PlaylistNextUp/Resume/collection/<COLLECTION_ID>?userId=<USER_GUID>`

These return the best “next item” to play and the last known progress.

## Where to plug into the UI (next steps)

- **Jellyfin Web:** use the bundled web plugin in `web/playlistnextup` (or custom JS) to render a “Continue playlists/collections” row calling this plugin’s endpoint.
- **Home Screen Sections / Modular Home:** depending on your setup, it can consume the endpoint to render a section.

## Web plugin (Jellyfin Web)

This repo includes a simple Jellyfin Web plugin under `web/playlistnextup`.

Install (example paths, adjust to your install):
- Copy the folder `web/playlistnextup` to a Jellyfin Web plugins folder (example: `/usr/share/jellyfin/web/plugins/playlistnextup`).
- Alternative example path: `/var/lib/jellyfin/web/plugins/playlistnextup`.
- Restart Jellyfin or reload the web client.

## Plugin repository (Jellyfin)

You can add this repository in Jellyfin:

- Repository URL: `https://raw.githubusercontent.com/fumasa/jellyfin-playlist-nextup-plugin/main/manifest.json`

Release assets:
- Server plugin zip: `Jellyfin.Plugin.PlaylistNextUp.zip`
- Web plugin zip: `playlistnextup-web.zip`

## Licensehttps://github.com/fumasa/jellyfin-playlist-nextup-plugin/blob/main/manifest.json

MIT.
