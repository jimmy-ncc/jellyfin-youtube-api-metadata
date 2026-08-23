using System.Threading;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using Jellyfin.Plugin.YoutubeApiMetadata.Caching;
using Jellyfin.Plugin.YoutubeApiMetadata.YouTube;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Tests.YouTube
{
    public class YoutubeMetadataResolverTests
    {
        private static readonly Video SampleVideo = new() { Id = "dQw4w9WgXcQ", Snippet = new VideoSnippet { Title = "Never Gonna Give You Up" } };
        private static readonly Channel SampleChannel = new() { Id = "UCuAXFkgsw1L7xaCfnd5JJOw", Snippet = new ChannelSnippet { Title = "Rick Astley" } };

        [Fact]
        public async Task GetVideoAsync_UsesCacheWhenFresh_AndDoesNotCallApi()
        {
            var cache = new Mock<IMetadataCache>();
            cache.Setup(c => c.GetVideoAsync("dQw4w9WgXcQ", It.IsAny<CancellationToken>())).ReturnsAsync(SampleVideo);
            var client = new Mock<IYouTubeApiClient>(MockBehavior.Strict);

            var resolver = new YoutubeMetadataResolver(client.Object, cache.Object);
            var result = await resolver.GetVideoAsync("dQw4w9WgXcQ", CancellationToken.None);

            Assert.Same(SampleVideo, result);
            client.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetVideoAsync_FallsBackToApiAndSavesToCache_WhenNotCached()
        {
            var cache = new Mock<IMetadataCache>();
            cache.Setup(c => c.GetVideoAsync("dQw4w9WgXcQ", It.IsAny<CancellationToken>())).ReturnsAsync((Video?)null);
            var client = new Mock<IYouTubeApiClient>();
            client.Setup(c => c.GetVideoAsync("dQw4w9WgXcQ", It.IsAny<CancellationToken>())).ReturnsAsync(SampleVideo);

            var resolver = new YoutubeMetadataResolver(client.Object, cache.Object);
            var result = await resolver.GetVideoAsync("dQw4w9WgXcQ", CancellationToken.None);

            Assert.Same(SampleVideo, result);
            cache.Verify(c => c.SaveVideoAsync("dQw4w9WgXcQ", SampleVideo, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetVideoAsync_DoesNotCacheWhenApiReturnsNothing()
        {
            var cache = new Mock<IMetadataCache>();
            cache.Setup(c => c.GetVideoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Video?)null);
            var client = new Mock<IYouTubeApiClient>();
            client.Setup(c => c.GetVideoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Video?)null);

            var resolver = new YoutubeMetadataResolver(client.Object, cache.Object);
            var result = await resolver.GetVideoAsync("deleted", CancellationToken.None);

            Assert.Null(result);
            cache.Verify(c => c.SaveVideoAsync(It.IsAny<string>(), It.IsAny<Video>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task GetChannelAsync_UsesCacheWhenFresh_AndDoesNotCallApi()
        {
            var cache = new Mock<IMetadataCache>();
            cache.Setup(c => c.GetChannelAsync("UCuAXFkgsw1L7xaCfnd5JJOw", It.IsAny<CancellationToken>())).ReturnsAsync(SampleChannel);
            var client = new Mock<IYouTubeApiClient>(MockBehavior.Strict);

            var resolver = new YoutubeMetadataResolver(client.Object, cache.Object);
            var result = await resolver.GetChannelAsync("UCuAXFkgsw1L7xaCfnd5JJOw", CancellationToken.None);

            Assert.Same(SampleChannel, result);
            client.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetChannelAsync_FallsBackToApiAndSavesToCache_WhenNotCached()
        {
            var cache = new Mock<IMetadataCache>();
            cache.Setup(c => c.GetChannelAsync("UCuAXFkgsw1L7xaCfnd5JJOw", It.IsAny<CancellationToken>())).ReturnsAsync((Channel?)null);
            var client = new Mock<IYouTubeApiClient>();
            client.Setup(c => c.GetChannelAsync("UCuAXFkgsw1L7xaCfnd5JJOw", It.IsAny<CancellationToken>())).ReturnsAsync(SampleChannel);

            var resolver = new YoutubeMetadataResolver(client.Object, cache.Object);
            var result = await resolver.GetChannelAsync("UCuAXFkgsw1L7xaCfnd5JJOw", CancellationToken.None);

            Assert.Same(SampleChannel, result);
            cache.Verify(c => c.SaveChannelAsync("UCuAXFkgsw1L7xaCfnd5JJOw", SampleChannel, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
