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
    public class YoutubeEpisodeProviderTests
    {
        private static readonly Video SampleVideo = new()
        {
            Id = "dQw4w9WgXcQ",
            Snippet = new VideoSnippet { Title = "Never Gonna Give You Up", Description = "The official video." }
        };

        [Fact]
        public async Task GetMetadata_ResolvesVideoIdFromFileName()
        {
            var resolver = new Mock<IYoutubeMetadataResolver>();
            resolver.Setup(r => r.GetVideoAsync("dQw4w9WgXcQ", It.IsAny<CancellationToken>())).ReturnsAsync(SampleVideo);

            var provider = new YoutubeEpisodeProvider(resolver.Object);
            var info = new EpisodeInfo { Path = "Some Video [dQw4w9WgXcQ].mkv" };

            var result = await provider.GetMetadata(info, CancellationToken.None);

            Assert.True(result.HasMetadata);
            Assert.Equal("Never Gonna Give You Up", result.Item.Name);
        }

        [Fact]
        public async Task GetMetadata_ReturnsEmptyResult_WhenPathHasNoId()
        {
            var resolver = new Mock<IYoutubeMetadataResolver>(MockBehavior.Strict);

            var provider = new YoutubeEpisodeProvider(resolver.Object);
            var info = new EpisodeInfo { Path = "no id here.mkv" };

            var result = await provider.GetMetadata(info, CancellationToken.None);

            Assert.False(result.HasMetadata);
            resolver.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetMetadata_ReturnsEmptyResult_WhenVideoNotFound()
        {
            var resolver = new Mock<IYoutubeMetadataResolver>();
            resolver.Setup(r => r.GetVideoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Video?)null);

            var provider = new YoutubeEpisodeProvider(resolver.Object);
            var info = new EpisodeInfo { Path = "Deleted Video [aaaaaaaaaaa].mkv" };

            var result = await provider.GetMetadata(info, CancellationToken.None);

            Assert.False(result.HasMetadata);
        }

        [Fact]
        public async Task GetSearchResults_UsesStoredProviderIdWhenAvailable()
        {
            var resolver = new Mock<IYoutubeMetadataResolver>();
            resolver.Setup(r => r.GetVideoAsync("dQw4w9WgXcQ", It.IsAny<CancellationToken>())).ReturnsAsync(SampleVideo);

            var provider = new YoutubeEpisodeProvider(resolver.Object);
            var info = new EpisodeInfo
            {
                Path = "Some Other File Name.mkv",
                ProviderIds = new System.Collections.Generic.Dictionary<string, string> { { Constants.PluginName, "dQw4w9WgXcQ" } }
            };

            var results = await provider.GetSearchResults(info, CancellationToken.None);

            Assert.Single(results);
        }
    }
}
