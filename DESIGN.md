# Design

## Data model
We store progress per *user* and per *container* (playlist or collection):
- Last played item id
- Last position ticks
- Updated timestamp
- Optional: last order snapshot hash (to detect reorder)

`ProgressKey = {UserId}:{ContainerType}:{ContainerId}`

## Playback monitoring
We subscribe to `ISessionManager` events:
- PlaybackStart
- PlaybackProgress
- PlaybackStopped

These events exist on Jellyfin's `ISessionManager` in 10.10.x docs/doxygen. See fossies doxygen listing events for `ISessionManager`. (Search result snippet shows PlaybackStart/PlaybackProgress etc.)

### Detecting playlist/collection context
This is the hardest part and may vary by client.
In v1 we implement multiple heuristics:
1. If `PlaybackProgressEventArgs` includes a PlaylistId / ParentId / Container id, use it.
2. If not, attempt:
   - For playlists: check if the playing item is contained in any playlist owned by the user
   - For collections: check if the playing item is in any collection
3. If multiple matches, prefer:
   - the most recently updated playlist
   - or the one explicitly configured in “tracked list ids” (optional)

This heuristic is isolated in `ContainerResolver`.

## API
`/PlaylistNextUp/Resume`
- returns a list of resume candidates (containers with next item) for a user

`/PlaylistNextUp/Resume/playlist/{playlistId}`
- returns next item for that playlist

`/PlaylistNextUp/Resume/collection/{collectionId}`
- returns next item for that collection

## Security
- We require `userId` query param and validate it exists.
- Future: honor Jellyfin auth and infer user from token.
