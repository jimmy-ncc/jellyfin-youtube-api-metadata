using System.Linq;
using Jellyfin.Plugin.YoutubeApiMetadata.Providers;
using MediaBrowser.Controller.Entities.TV;
using Xunit;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Tests.Providers
{
    public class YoutubeExternalUrlProviderTests
    {
        [Fact]
        public void GetExternalUrls_ReturnsVideoUrl_ForEpisode()
        {
            var provider = new YoutubeExternalUrlProvider();
            var item = new Episode();
            item.ProviderIds.Add(Constants.PluginName, "dQw4w9WgXcQ");

            var urls = provider.GetExternalUrls(item).ToList();

            Assert.Equal(new[] { "https://www.youtube.com/watch?v=dQw4w9WgXcQ" }, urls);
        }

        [Fact]
        public void GetExternalUrls_ReturnsChannelUrl_ForSeries()
        {
            var provider = new YoutubeExternalUrlProvider();
            var item = new Series();
            item.ProviderIds.Add(Constants.PluginName, "UCuAXFkgsw1L7xaCfnd5JJOw");

            var urls = provider.GetExternalUrls(item).ToList();

            Assert.Equal(new[] { "https://www.youtube.com/channel/UCuAXFkgsw1L7xaCfnd5JJOw" }, urls);
        }

        [Fact]
        public void GetExternalUrls_ReturnsEmpty_WhenNoProviderIdStored()
        {
            var provider = new YoutubeExternalUrlProvider();

            Assert.Empty(provider.GetExternalUrls(new Episode()));
        }

        [Fact]
        public void GetExternalUrls_ReturnsEmpty_ForUnsupportedItemType()
        {
            var provider = new YoutubeExternalUrlProvider();
            var item = new Season();
            item.ProviderIds.Add(Constants.PluginName, "dQw4w9WgXcQ");

            Assert.Empty(provider.GetExternalUrls(item));
        }
    }
}
