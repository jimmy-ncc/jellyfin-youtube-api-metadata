using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.YoutubeApiMetadata.YouTube;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Providers
{
    /// <summary>
    /// Supplies the channel avatar as the Primary image for a Series.
    /// </summary>
    public class YoutubeSeriesImageProvider : IRemoteImageProvider, IHasOrder
    {
        private readonly IYoutubeMetadataResolver _resolver;

        public YoutubeSeriesImageProvider(IYoutubeMetadataResolver resolver)
        {
            _resolver = resolver;
        }

        public string Name => Constants.PluginName;

        public int Order => 1;

        public bool Supports(BaseItem item) => item is Series;

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[] { ImageType.Primary };
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var channelId = Utils.ResolveChannelId(item.ProviderIds, item.Path, item.Name);
            if (string.IsNullOrEmpty(channelId))
            {
                return System.Array.Empty<RemoteImageInfo>();
            }

            var channel = await _resolver.GetChannelAsync(channelId, cancellationToken).ConfigureAwait(false);
            var url = channel == null ? null : Utils.GetBestThumbnailUrl(channel.Snippet.Thumbnails);
            if (string.IsNullOrEmpty(url))
            {
                return System.Array.Empty<RemoteImageInfo>();
            }

            return new[]
            {
                new RemoteImageInfo
                {
                    ProviderName = Name,
                    Url = url,
                    Type = ImageType.Primary
                }
            };
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return Plugin.Instance.GetHttpClient().GetAsync(url, cancellationToken);
        }
    }
}
