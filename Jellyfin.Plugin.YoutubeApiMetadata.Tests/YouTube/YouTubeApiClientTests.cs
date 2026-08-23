using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.YoutubeApiMetadata.YouTube;
using Xunit;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Tests.YouTube
{
    public class YouTubeApiClientTests
    {
        private const string VideoListResponse = @"{
            ""kind"": ""youtube#videoListResponse"",
            ""items"": [
                {
                    ""kind"": ""youtube#video"",
                    ""id"": ""dQw4w9WgXcQ"",
                    ""snippet"": {
                        ""publishedAt"": ""2009-10-25T06:57:33Z"",
                        ""channelId"": ""UCuAXFkgsw1L7xaCfnd5JJOw"",
                        ""title"": ""Rick Astley - Never Gonna Give You Up"",
                        ""description"": ""The official video."",
                        ""channelTitle"": ""Rick Astley""
                    },
                    ""contentDetails"": {
                        ""duration"": ""PT3M33S""
                    },
                    ""statistics"": {
                        ""viewCount"": ""1000000000"",
                        ""likeCount"": ""12000000""
                    }
                }
            ]
        }";

        private const string EmptyListResponse = @"{ ""kind"": ""youtube#videoListResponse"", ""items"": [] }";

        private const string ChannelListResponse = @"{
            ""kind"": ""youtube#channelListResponse"",
            ""items"": [
                {
                    ""kind"": ""youtube#channel"",
                    ""id"": ""UCuAXFkgsw1L7xaCfnd5JJOw"",
                    ""snippet"": {
                        ""title"": ""Rick Astley"",
                        ""description"": ""The official Rick Astley YouTube channel.""
                    },
                    ""statistics"": {
                        ""subscriberCount"": ""3000000""
                    }
                }
            ]
        }";

        [Fact]
        public void Constructor_DoesNotThrowWithoutApiKey()
        {
            // Jellyfin constructs providers (and this client) once at server startup to register
            // them, before the admin has necessarily configured a key yet. The constructor must
            // stay lenient - otherwise every provider silently fails to register until a restart.
            var exception = Record.Exception(() => new YouTubeApiClient(string.Empty));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetVideoAsync_ThrowsWhenApiKeyMissing(string apiKey)
        {
            using var client = new YouTubeApiClient(apiKey);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => client.GetVideoAsync("dQw4w9WgXcQ", CancellationToken.None));
        }

        [Fact]
        public async Task GetVideoAsync_ReturnsParsedVideo()
        {
            var handler = new FakeHttpMessageHandler(VideoListResponse);
            using var client = new YouTubeApiClient("TESTKEY", new FakeGoogleHttpClientFactory(handler));

            var video = await client.GetVideoAsync("dQw4w9WgXcQ", CancellationToken.None);

            Assert.NotNull(video);
            Assert.Equal("dQw4w9WgXcQ", video!.Id);
            Assert.Equal("Rick Astley - Never Gonna Give You Up", video.Snippet.Title);
            Assert.Equal("UCuAXFkgsw1L7xaCfnd5JJOw", video.Snippet.ChannelId);
            Assert.Equal("PT3M33S", video.ContentDetails.Duration);

            Assert.NotNull(handler.LastRequest);
            var query = handler.LastRequest!.RequestUri!.Query;
            Assert.Contains("id=dQw4w9WgXcQ", query);
            Assert.Contains("key=TESTKEY", query);
        }

        [Fact]
        public async Task GetVideoAsync_ReturnsNullWhenNotFound()
        {
            var handler = new FakeHttpMessageHandler(EmptyListResponse);
            using var client = new YouTubeApiClient("TESTKEY", new FakeGoogleHttpClientFactory(handler));

            var video = await client.GetVideoAsync("doesnotexist", CancellationToken.None);

            Assert.Null(video);
        }

        [Fact]
        public async Task GetChannelAsync_ReturnsParsedChannel()
        {
            var handler = new FakeHttpMessageHandler(ChannelListResponse);
            using var client = new YouTubeApiClient("TESTKEY", new FakeGoogleHttpClientFactory(handler));

            var channel = await client.GetChannelAsync("UCuAXFkgsw1L7xaCfnd5JJOw", CancellationToken.None);

            Assert.NotNull(channel);
            Assert.Equal("UCuAXFkgsw1L7xaCfnd5JJOw", channel!.Id);
            Assert.Equal("Rick Astley", channel.Snippet.Title);

            var query = handler.LastRequest!.RequestUri!.Query;
            Assert.Contains("id=UCuAXFkgsw1L7xaCfnd5JJOw", query);
        }

        [Fact]
        public async Task SearchVideosAsync_SetsVideoTypeAndQuery()
        {
            const string searchResponse = @"{ ""kind"": ""youtube#searchListResponse"", ""items"": [] }";
            var handler = new FakeHttpMessageHandler(searchResponse);
            using var client = new YouTubeApiClient("TESTKEY", new FakeGoogleHttpClientFactory(handler));

            // 10 is deliberately different from the API's default maxResults (5): the Google client
            // omits query parameters that equal their default value, so testing with 5 would pass
            // even if MaxResults were never wired up.
            var results = await client.SearchVideosAsync("3blue1brown", 10, CancellationToken.None);

            Assert.Empty(results);
            var query = handler.LastRequest!.RequestUri!.Query;
            Assert.Contains("q=3blue1brown", query);
            Assert.Contains("type=video", query);
            Assert.Contains("maxResults=10", query);
        }
    }
}
