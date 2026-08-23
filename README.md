# Jellyfin Plugin: YouTube API Metadata

![Build and Test Plugin](https://github.com/jimmy-ncc/jellyfin-youtube-api-metadata/actions/workflows/build.yml/badge.svg)

Provides metadata (title, description, thumbnails, channel info, tags, duration...) for local YouTube video libraries, fetched from the **official YouTube Data API v3** — no [`yt-dlp`](https://github.com/yt-dlp/yt-dlp) dependency, no cookies, no age-gate/anti-bot fragility for the metadata step.

Inspired by [ankenyr/jellyfin-youtube-metadata-plugin](https://github.com/ankenyr/jellyfin-youtube-metadata-plugin), which this plugin can run side by side with (different plugin GUID, different cache directory).

## Compatibility

Built and tested against **Jellyfin 10.11.x** (server 10.11.11). **Not compatible with Jellyfin 12.x** — that's a new major server version with breaking API changes; this plugin targets the 10.x `Jellyfin.Controller`/`Jellyfin.Data` assemblies and will not load correctly on 12.x.

## How it maps to Jellyfin

- A YouTube **channel** = a Jellyfin **Series**
- A **video** = an **Episode**

## File naming convention

This plugin only fetches **metadata** — it does not download videos. Download the videos themselves with [`yt-dlp`](https://github.com/yt-dlp/yt-dlp), using an output template that matches the layout below:

```
yt-dlp -o "<library>/%(uploader)s/%(uploader)s - %(upload_date)s - %(title)s [%(id)s].%(ext)s" <url>
```

Which produces:

```
<library>/<Channel Name>/<Channel Name> - <upload_date> - <title> [<videoId>].<ext>
```

Only the 11-character video ID between square brackets in the file name is required — the rest is cosmetic. Channel folders can optionally be named `<Channel Name> [<channelId>]` (24-char channel ID) to skip a name-based lookup on first import; without it, the plugin resolves the channel by searching YouTube for the folder name once, then remembers the ID.

## Setup

1. Get a YouTube Data API v3 key from the [Google Cloud Console](https://console.cloud.google.com/apis/credentials) (enable the "YouTube Data API v3" API on the project first).
2. Install the plugin (see below), then set the key in **Dashboard → Plugins → YouTube API Metadata**. You can also adjust the cache TTL there (default: 30 days).
3. Point a **Shows** library at your YouTube video folders.

## Installation

### Via a plugin repository (recommended)

Dashboard → Plugins → Repositories → Add:

```
https://raw.githubusercontent.com/jimmy-ncc/jellyfin-youtube-api-metadata/main/manifest.json
```

Then install "YouTube API Metadata" from Dashboard → Plugins → Catalog.

### Manual

Download the latest release zip from the [Releases page](https://github.com/jimmy-ncc/jellyfin-youtube-api-metadata/releases), extract it into `<jellyfin data dir>/plugins/YoutubeApiMetadata_<version>/`, and restart Jellyfin.

## Development

```bash
dotnet build
dotnet test
```

A devcontainer is provided (`.devcontainer/`) with a full Jellyfin dev environment (server + web client built from source).

## License

[GNU AGPL v3.0](LICENSE)
