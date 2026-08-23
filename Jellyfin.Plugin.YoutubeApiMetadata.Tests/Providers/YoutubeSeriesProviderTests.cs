using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using Jellyfin.Plugin.YoutubeApiMetadata.Providers;
using Jellyfin.Plugin.YoutubeApiMetadata.YouTube;
using MediaBrowser.Controller.Providers;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Tests.Providers
{
    public class YoutubeSeriesProviderTests
    {
        private const string ChannelId = "UCuAXFkgsw1L7xaCfnd5JJOw";

        private static readonly Channel SampleChannel = new()
        {
            Id = ChannelId,
            Snippet = new ChannelSnippet { Title = "Rick Astley", Description = "Official channel." }
        };

        [Fact]
        public async Task GetMetadata_ResolvesChannelIdFromFolderName()
        {
            var resolver = new Mock<IYoutubeMetadataResolver>();
            resolver.Setup(r => r.GetChannelAsync(ChannelId, It.IsAny<CancellationToken>())).ReturnsAsync(SampleChannel);
            var client = new Mock<IYouTubeApiClient>(MockBehavior.Strict);

            var provider = new YoutubeSeriesProvider(client.Object, resolver.Object);
            var info = new SeriesInfo { Path = $"/media/channels/Rick Astley [{ChannelId}]", Name = "Rick Astley" };

            var result = await provider.GetMetadata(info, CancellationToken.None);

            Assert.True(result.HasMetadata);
            Assert.Equal("Rick Astley", result.Item.Name);
            client.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetMetadata_PrefersStoredProviderIdOverFolderName()
        {
            var resolver = new Mock<IYoutubeMetadataResolver>();
            resolver.Setup(r => r.GetChannelAsync(ChannelId, It.IsAny<CancellationToken>())).ReturnsAsync(SampleChannel);
            var client = new Mock<IYouTubeApiClient>(MockBehavior.Strict);

            var provider = new YoutubeSeriesProvider(client.Object, resolver.Object);
            var info = new SeriesInfo
            {
                Path = "/media/channels/Some Other Folder Name",
                ProviderIds = new Dictionary<string, string> { { Constants.PluginName, ChannelId } }
            };

            var result = await provider.GetMetadata(info, CancellationToken.None);

            Assert.True(result.HasMetadata);
            resolver.Verify(r => r.GetChannelAsync(ChannelId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetMetadata_FallsBackToNameSearch_WhenFolderHasNoId()
        {
            // Matches the real-world "%(uploader)s" folder naming used by yt-dlp/the old plugin:
            // no channel ID embedded anywhere, so the only way in is a name search.
            var resolver = new Mock<IYoutubeMetadataResolver>();
            resolver.Setup(r => r.GetChannelAsync(ChannelId, It.IsAny<CancellationToken>())).ReturnsAsync(SampleChannel);
            var client = new Mock<IYouTubeApiClient>();
            client.Setup(c => c.SearchChannelsAsync("Rick Astley", 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SearchResult> { new() { Id = new ResourceId { ChannelId = ChannelId } } });

            var provider = new YoutubeSeriesProvider(client.Object, resolver.Object);
            var info = new SeriesInfo { Path = "/media/channels/Rick Astley", Name = "Rick Astley" };

            var result = await provider.GetMetadata(info, CancellationToken.None);

            Assert.True(result.HasMetadata);
            Assert.Equal("Rick Astley", result.Item.Name);
        }

        [Fact]
        public async Task GetMetadata_ReturnsEmptyResult_WhenNameSearchFindsNothing()
        {
            var resolver = new Mock<IYoutubeMetadataResolver>(MockBehavior.Strict);
            var client = new Mock<IYouTubeApiClient>();
            client.Setup(c => c.SearchChannelsAsync(It.IsAny<string>(), 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SearchResult>());

            var provider = new YoutubeSeriesProvider(client.Object, resolver.Object);
            var info = new SeriesInfo { Path = "/media/channels/Unknown Channel", Name = "Unknown Channel" };

            var result = await provider.GetMetadata(info, CancellationToken.None);

            Assert.False(result.HasMetadata);
        }

        [Fact]
        public async Task GetSearchResults_FallsBackToNameSearch_WhenNoIdAvailable()
        {
            var resolver = new Mock<IYoutubeMetadataResolver>(MockBehavior.Strict);
            var client = new Mock<IYouTubeApiClient>();
            var searchResult = new SearchResult
            {
                Id = new ResourceId { ChannelId = ChannelId },
                Snippet = new SearchResultSnippet { Title = "Rick Astley", Description = "Official channel." }
            };
            client.Setup(c => c.SearchChannelsAsync("Rick Astley", 10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SearchResult> { searchResult });

            var provider = new YoutubeSeriesProvider(client.Object, resolver.Object);
            var searchInfo = new SeriesInfo { Name = "Rick Astley" };

            var results = (await provider.GetSearchResults(searchInfo, CancellationToken.None)).ToList();

            Assert.Single(results);
            Assert.Equal(ChannelId, results[0].ProviderIds[Constants.PluginName]);
        }
    }
}
