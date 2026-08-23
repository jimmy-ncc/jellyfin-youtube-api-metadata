using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Http;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace Jellyfin.Plugin.YoutubeApiMetadata.YouTube
{
    /// <inheritdoc cref="IYouTubeApiClient" />
    public sealed class YouTubeApiClient : IYouTubeApiClient, IDisposable
    {
        private const string VideoParts = "snippet,contentDetails,statistics";
        private const string ChannelParts = "snippet,brandingSettings,statistics";
        private const string SearchParts = "snippet";

        private readonly string _apiKey;
        private readonly IHttpClientFactory? _httpClientFactory;
        private YouTubeService? _service;

        public YouTubeApiClient(string apiKey, IHttpClientFactory? httpClientFactory = null)
        {
            // Jellyfin constructs providers (and therefore this client) once at server startup to
            // register them, regardless of whether the plugin has been configured yet. Validating
            // the API key here would make every provider silently fail to register on a fresh
            // install, until the next restart. So the key is only required lazily, on first actual
            // API call - which only happens during a real metadata fetch.
            _apiKey = apiKey;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<Video?> GetVideoAsync(string videoId, CancellationToken cancellationToken)
        {
            var request = GetService().Videos.List(VideoParts);
            request.Id = videoId;
            var response = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return response.Items?.FirstOrDefault();
        }

        public async Task<Channel?> GetChannelAsync(string channelId, CancellationToken cancellationToken)
        {
            var request = GetService().Channels.List(ChannelParts);
            request.Id = channelId;
            var response = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return response.Items?.FirstOrDefault();
        }

        public async Task<IReadOnlyList<SearchResult>> SearchVideosAsync(string query, int maxResults, CancellationToken cancellationToken)
        {
            var request = GetService().Search.List(SearchParts);
            request.Q = query;
            request.Type = "video";
            request.MaxResults = maxResults;
            var response = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return (IReadOnlyList<SearchResult>?)response.Items ?? Array.Empty<SearchResult>();
        }

        public async Task<IReadOnlyList<SearchResult>> SearchChannelsAsync(string query, int maxResults, CancellationToken cancellationToken)
        {
            var request = GetService().Search.List(SearchParts);
            request.Q = query;
            request.Type = "channel";
            request.MaxResults = maxResults;
            var response = await request.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return (IReadOnlyList<SearchResult>?)response.Items ?? Array.Empty<SearchResult>();
        }

        public void Dispose()
        {
            _service?.Dispose();
        }

        private YouTubeService GetService()
        {
            if (_service != null)
            {
                return _service;
            }

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new InvalidOperationException(
                    "No YouTube Data API v3 key configured. Set one in the plugin's settings page, then retry the scan.");
            }

            var initializer = new BaseClientService.Initializer
            {
                ApiKey = _apiKey,
                ApplicationName = Constants.PluginName
            };

            if (_httpClientFactory != null)
            {
                initializer.HttpClientFactory = _httpClientFactory;
            }

            _service = new YouTubeService(initializer);
            return _service;
        }
    }
}
