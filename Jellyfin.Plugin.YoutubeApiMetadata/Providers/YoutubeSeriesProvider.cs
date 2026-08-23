using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using Jellyfin.Plugin.YoutubeApiMetadata.YouTube;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Providers
{
    /// <summary>
    /// Fetches Series metadata (a YouTube channel = a "show") from the YouTube Data API v3.
    /// A series folder can be named "Channel Name [channelId]" (24-char channel ID between square
    /// brackets, same convention as episode files) to resolve without a search. Folders following
    /// yt-dlp's plain "%(uploader)s" convention (no ID) fall back to a name search on first import;
    /// the resolved channel ID is then persisted to ProviderIds so later scans skip the search.
    /// </summary>
    public class YoutubeSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>
    {
        private readonly IYouTubeApiClient _client;
        private readonly IYoutubeMetadataResolver _resolver;

        public YoutubeSeriesProvider(IYouTubeApiClient client, IYoutubeMetadataResolver resolver)
        {
            _client = client;
            _resolver = resolver;
        }

        public string Name => Constants.PluginName;

        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            var channelId = Utils.ResolveChannelId(info.ProviderIds, info.Path, info.Name);
            var channel = string.IsNullOrEmpty(channelId)
                ? await FindSingleChannelByNameAsync(info.Name, cancellationToken).ConfigureAwait(false)
                : await _resolver.GetChannelAsync(channelId, cancellationToken).ConfigureAwait(false);

            return channel == null ? new MetadataResult<Series>() : Utils.ChannelToSeries(channel);
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            var channelId = Utils.ResolveChannelId(searchInfo.ProviderIds, searchInfo.Path, null);
            if (!string.IsNullOrEmpty(channelId))
            {
                var channel = await _resolver.GetChannelAsync(channelId, cancellationToken).ConfigureAwait(false);
                return channel == null ? Array.Empty<RemoteSearchResult>() : new[] { ToSearchResult(channel) };
            }

            if (string.IsNullOrWhiteSpace(searchInfo.Name))
            {
                return Array.Empty<RemoteSearchResult>();
            }

            var matches = await _client.SearchChannelsAsync(searchInfo.Name, 10, cancellationToken).ConfigureAwait(false);
            return matches
                .Where(m => !string.IsNullOrEmpty(m.Id?.ChannelId))
                .Select(m => new RemoteSearchResult
                {
                    Name = m.Snippet.Title,
                    Overview = m.Snippet.Description,
                    ProviderIds = new Dictionary<string, string> { { Constants.PluginName, m.Id.ChannelId } },
                    ImageUrl = Utils.GetBestThumbnailUrl(m.Snippet.Thumbnails)
                })
                .ToList();
        }

        private static RemoteSearchResult ToSearchResult(Channel channel) => new()
        {
            Name = channel.Snippet.Title,
            Overview = channel.Snippet.Description,
            ProviderIds = new Dictionary<string, string> { { Constants.PluginName, channel.Id } },
            ImageUrl = Utils.GetBestThumbnailUrl(channel.Snippet.Thumbnails)
        };

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return Plugin.Instance.GetHttpClient().GetAsync(url, cancellationToken);
        }

        /// <summary>
        /// Last-resort lookup for folders with no known channel ID: searches YouTube by name and
        /// fetches full details for the top match.
        /// </summary>
        private async Task<Channel?> FindSingleChannelByNameAsync(string? name, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var matches = await _client.SearchChannelsAsync(name, 1, cancellationToken).ConfigureAwait(false);
            var channelId = matches.FirstOrDefault()?.Id?.ChannelId;
            return string.IsNullOrEmpty(channelId) ? null : await _resolver.GetChannelAsync(channelId, cancellationToken).ConfigureAwait(false);
        }
    }
}
