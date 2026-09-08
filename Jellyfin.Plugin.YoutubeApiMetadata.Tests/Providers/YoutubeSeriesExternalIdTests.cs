using Jellyfin.Plugin.YoutubeApiMetadata.Providers;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Providers;
using Xunit;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Tests.Providers
{
    public class YoutubeSeriesExternalIdTests
    {
        [Fact]
        public void ExposesExpectedProviderMetadata()
        {
            var externalId = new YoutubeSeriesExternalId();

            Assert.Equal("YouTube", externalId.ProviderName);
            Assert.Equal(Constants.PluginName, externalId.Key);
            Assert.Equal(ExternalIdMediaType.Series, externalId.Type);
        }

        [Fact]
        public void Supports_OnlySeries()
        {
            var externalId = new YoutubeSeriesExternalId();

            Assert.True(externalId.Supports(new Series()));
            Assert.False(externalId.Supports(new Episode()));
        }
    }
}
