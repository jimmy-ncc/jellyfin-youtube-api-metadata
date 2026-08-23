using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.YoutubeApiMetadata.YouTube;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Providers
{
    /// <summary>
    /// Fetches Episode metadata (a single YouTube video) from the YouTube Data API v3.
    /// </summary>
    public class YoutubeEpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>
    {
        private readonly IYoutubeMetadataResolver _resolver;

        public YoutubeEpisodeProvider(IYoutubeMetadataResolver resolver)
        {
            _resolver = resolver;
        }

        public string Name => Constants.PluginName;

        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            var videoId = Utils.GetYTID(info.Path ?? string.Empty);
            if (string.IsNullOrEmpty(videoId))
            {
                return new MetadataResult<Episode>();
            }

            var video = await _resolver.GetVideoAsync(videoId, cancellationToken).ConfigureAwait(false);
            return video == null ? new MetadataResult<Episode>() : Utils.VideoToEpisode(video);
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            var videoId = searchInfo.ProviderIds.TryGetValue(Constants.PluginName, out var id)
                ? id
                : Utils.GetYTID(searchInfo.Path ?? string.Empty);

            if (string.IsNullOrEmpty(videoId))
            {
                return Array.Empty<RemoteSearchResult>();
            }

            var video = await _resolver.GetVideoAsync(videoId, cancellationToken).ConfigureAwait(false);
            if (video == null)
            {
                return Array.Empty<RemoteSearchResult>();
            }

            return new[]
            {
                new RemoteSearchResult
                {
                    Name = video.Snippet.Title,
                    Overview = video.Snippet.Description,
                    ProviderIds = new Dictionary<string, string> { { Constants.PluginName, video.Id } },
                    ImageUrl = Utils.GetBestThumbnailUrl(video.Snippet.Thumbnails)
                }
            };
        }

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return Plugin.Instance.GetHttpClient().GetAsync(url, cancellationToken);
        }
    }
}
