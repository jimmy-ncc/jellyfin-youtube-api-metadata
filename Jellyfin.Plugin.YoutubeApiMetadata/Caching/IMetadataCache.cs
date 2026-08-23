using System.Threading;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Caching
{
    /// <summary>
    /// Disk cache for YouTube Data API responses, so repeated Jellyfin scans don't burn API quota.
    /// </summary>
    public interface IMetadataCache
    {
        /// <summary>
        /// Returns the cached video, or null if there is no fresh cache entry for it.
        /// </summary>
        Task<Video?> GetVideoAsync(string videoId, CancellationToken cancellationToken);

        Task SaveVideoAsync(string videoId, Video video, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the cached channel, or null if there is no fresh cache entry for it.
        /// </summary>
        Task<Channel?> GetChannelAsync(string channelId, CancellationToken cancellationToken);

        Task SaveChannelAsync(string channelId, Channel channel, CancellationToken cancellationToken);
    }
}
