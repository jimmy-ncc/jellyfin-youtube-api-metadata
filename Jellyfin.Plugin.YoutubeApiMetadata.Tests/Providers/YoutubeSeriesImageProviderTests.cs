using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using Jellyfin.Plugin.YoutubeApiMetadata.Providers;
using Jellyfin.Plugin.YoutubeApiMetadata.YouTube;
using MediaBrowser.Controller.Entities.TV;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Tests.Providers
{
    public class YoutubeSeriesImageProviderTests
    {
        private const string ChannelId = "UCuAXFkgsw1L7xaCfnd5JJOw";

        [Fact]
        public async Task GetImages_ReturnsAvatar_WhenChannelIdKnownFromProviderIds()
        {
            var channel = new Channel
            {
                Id = ChannelId,
                Snippet = new ChannelSnippet
                {
                    Title = "Rick Astley",
                    Thumbnails = new ThumbnailDetails { High = new Thumbnail { Url = "https://example.com/avatar.jpg" } }
                }
            };
            var resolver = new Mock<IYoutubeMetadataResolver>();
            resolver.Setup(r => r.GetChannelAsync(ChannelId, It.IsAny<CancellationToken>())).ReturnsAsync(channel);

            var provider = new YoutubeSeriesImageProvider(resolver.Object);
            var item = new Series
            {
                Path = "/media/Rick Astley",
                ProviderIds = new Dictionary<string, string> { { Constants.PluginName, ChannelId } }
            };

            var images = (await provider.GetImages(item, CancellationToken.None)).ToList();

            Assert.Single(images);
            Assert.Equal("https://example.com/avatar.jpg", images[0].Url);
        }

        [Fact]
        public async Task GetImages_ReturnsEmpty_WhenNoChannelIdResolvable()
        {
            var resolver = new Mock<IYoutubeMetadataResolver>(MockBehavior.Strict);
            var provider = new YoutubeSeriesImageProvider(resolver.Object);
            var item = new Series { Path = "/media/Some Random Folder", Name = "Some Random Folder" };

            var images = await provider.GetImages(item, CancellationToken.None);

            Assert.Empty(images);
            resolver.VerifyNoOtherCalls();
        }
    }
}
