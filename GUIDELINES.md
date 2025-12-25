# Guidelines (simple)

## What we are building (v1)
A Jellyfin server plugin that:
1. Observes playback sessions (start/progress/stop).
2. Detects when playback belongs to a **Playlist** or **Collection** (best-effort).
3. Persists per-user progress in a small store.
4. Provides an HTTP API to fetch a “Resume” target (the next item) for:
   - all tracked lists
   - a specific playlist
   - a specific collection
5. Provides a minimal Dashboard config page.

## Constraints / Reality check
- Native “Next Up” is a TV-show episode pipeline; playlists/collections are different objects.
- The server does not (currently) expose a clean API to inject arbitrary “Next Up” entries for all clients.
- So we implement a **resume feed** that can be shown in the Web UI via:
  - Custom JS injection
  - A home screen section plugin
  - (Later) a dedicated client plugin

## Design principles
- Keep it safe: never mutate library items.
- Keep it reversible: all state is stored under the plugin’s config folder.
- Keep it lightweight: avoid DB migrations in v1.
- Log enough for debugging but allow quiet mode.

## Definitions
- **Resume threshold**: Minimum seconds watched before we consider something “in progress”.
- **Completed threshold**: Percentage (e.g. 92%) after which we consider the item “watched” for the purpose of advancing.

## Future (v2+)
- Replace JSON store with a proper SQLite store.
- Add UI pages styled with "Plugin Pages" (optional).
- Add “row provider” if/when Jellyfin supports server-driven home rows broadly.
