using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;

namespace Jellyfin.Plugin.YoutubeApiMetadata.YouTube
{
    /// <summary>
    /// Thin wrapper around the YouTube Data API v3, used by the metadata providers.
    /// </summary>
    public interface IYouTubeApiClient
    {
        /// <summary>
        /// Fetches a single video by ID (snippet, contentDetails and statistics parts).
        /// Returns null if no video with that ID exists (deleted/private/invalid ID).
        /// </summary>
        Task<Video?> GetVideoAsync(string videoId, CancellationToken cancellationToken);

        /// <summary>
        /// Fetches a single channel by ID (snippet, brandingSettings and statistics parts).
        /// Returns null if no channel with that ID exists.
        /// </summary>
        Task<Channel?> GetChannelAsync(string channelId, CancellationToken cancellationToken);

        /// <summary>
        /// Searches for videos matching a free-text query (used by Jellyfin's manual "Identify").
        /// </summary>
        Task<IReadOnlyList<SearchResult>> SearchVideosAsync(string query, int maxResults, CancellationToken cancellationToken);

        /// <summary>
        /// Searches for channels matching a free-text query (used by Jellyfin's manual "Identify").
        /// </summary>
        Task<IReadOnlyList<SearchResult>> SearchChannelsAsync(string query, int maxResults, CancellationToken cancellationToken);
    }
}
