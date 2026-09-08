using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.YoutubeApiMetadata.YouTube;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
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
        private readonly ILibraryManager _libraryManager;

        public YoutubeEpisodeProvider(IYoutubeMetadataResolver resolver, ILibraryManager libraryManager)
        {
            _resolver = resolver;
            _libraryManager = libraryManager;
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
            if (video == null)
            {
                return new MetadataResult<Episode>();
            }

            var result = Utils.VideoToEpisode(video);
            if (result.Item.PremiereDate.HasValue)
            {
                var siblings = GetSiblingPremiereDates(info.SeriesProviderIds, videoId);
                result.Item.ParentIndexNumber = 1;
                result.Item.IndexNumber = Utils.ComputeEpisodeIndex(videoId, result.Item.PremiereDate.Value, siblings);
            }

            return result;
        }

        /// <summary>
        /// Looks up the channel's Series item by its stored provider ID, then returns the premiere
        /// dates of its other already-scanned Episode children (excluding the current video) — so
        /// every video in the channel can be assigned a distinct <see cref="Episode.IndexNumber"/>
        /// and none of them collapse into "alternate versions" of the same episode.
        /// </summary>
        private IEnumerable<(string VideoId, DateTime PremiereDate)> GetSiblingPremiereDates(
            IReadOnlyDictionary<string, string> seriesProviderIds,
            string currentVideoId)
        {
            if (seriesProviderIds == null
                || !seriesProviderIds.TryGetValue(Constants.PluginName, out var channelId)
                || string.IsNullOrEmpty(channelId))
            {
                return Array.Empty<(string, DateTime)>();
            }

            var series = _libraryManager.GetItemList(new MediaBrowser.Controller.Entities.InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Series },
                HasAnyProviderId = new Dictionary<string, string> { { Constants.PluginName, channelId } }
            }).FirstOrDefault();

            if (series == null)
            {
                return Array.Empty<(string, DateTime)>();
            }

            return _libraryManager.GetItemList(new MediaBrowser.Controller.Entities.InternalItemsQuery
                {
                    IncludeItemTypes = new[] { BaseItemKind.Episode },
                    AncestorIds = new[] { series.Id }
                })
                .Where(e => e.PremiereDate.HasValue
                    && e.ProviderIds.TryGetValue(Constants.PluginName, out var id)
                    && id != currentVideoId)
                .Select(e => (e.ProviderIds[Constants.PluginName], e.PremiereDate!.Value))
                .ToList();
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
