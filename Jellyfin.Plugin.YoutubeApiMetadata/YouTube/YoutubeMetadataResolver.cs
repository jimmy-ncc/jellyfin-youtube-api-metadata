using System.Threading;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using Jellyfin.Plugin.YoutubeApiMetadata.Caching;

namespace Jellyfin.Plugin.YoutubeApiMetadata.YouTube
{
    /// <inheritdoc cref="IYoutubeMetadataResolver" />
    public sealed class YoutubeMetadataResolver : IYoutubeMetadataResolver
    {
        private readonly IYouTubeApiClient _client;
        private readonly IMetadataCache _cache;

        public YoutubeMetadataResolver(IYouTubeApiClient client, IMetadataCache cache)
        {
            _client = client;
            _cache = cache;
        }

        public async Task<Video?> GetVideoAsync(string videoId, CancellationToken cancellationToken)
        {
            var cached = await _cache.GetVideoAsync(videoId, cancellationToken).ConfigureAwait(false);
            if (cached != null)
            {
                return cached;
            }

            var video = await _client.GetVideoAsync(videoId, cancellationToken).ConfigureAwait(false);
            if (video != null)
            {
                await _cache.SaveVideoAsync(videoId, video, cancellationToken).ConfigureAwait(false);
            }

            return video;
        }

        public async Task<Channel?> GetChannelAsync(string channelId, CancellationToken cancellationToken)
        {
            var cached = await _cache.GetChannelAsync(channelId, cancellationToken).ConfigureAwait(false);
            if (cached != null)
            {
                return cached;
            }

            var channel = await _client.GetChannelAsync(channelId, cancellationToken).ConfigureAwait(false);
            if (channel != null)
            {
                await _cache.SaveChannelAsync(channelId, channel, cancellationToken).ConfigureAwait(false);
            }

            return channel;
        }
    }
}
