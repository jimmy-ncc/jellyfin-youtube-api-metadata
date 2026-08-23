using System.Threading;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;

namespace Jellyfin.Plugin.YoutubeApiMetadata.YouTube
{
    /// <summary>
    /// Resolves a video/channel by ID, checking the disk cache first and falling back to the
    /// YouTube Data API (saving the result back to the cache) on a miss. Shared by every provider
    /// that needs video/channel data - metadata providers and image providers alike.
    /// </summary>
    public interface IYoutubeMetadataResolver
    {
        Task<Video?> GetVideoAsync(string videoId, CancellationToken cancellationToken);

        Task<Channel?> GetChannelAsync(string channelId, CancellationToken cancellationToken);
    }
}
