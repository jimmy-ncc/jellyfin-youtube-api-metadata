using Jellyfin.Plugin.YoutubeApiMetadata.Providers;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Providers;
using Xunit;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Tests.Providers
{
    public class YoutubeEpisodeExternalIdTests
    {
        [Fact]
        public void ExposesExpectedProviderMetadata()
        {
            var externalId = new YoutubeEpisodeExternalId();

            Assert.Equal("YouTube", externalId.ProviderName);
            Assert.Equal(Constants.PluginName, externalId.Key);
            Assert.Equal(ExternalIdMediaType.Episode, externalId.Type);
        }

        [Fact]
        public void Supports_OnlyEpisodes()
        {
            var externalId = new YoutubeEpisodeExternalId();

            Assert.True(externalId.Supports(new Episode()));
            Assert.False(externalId.Supports(new Series()));
        }
    }
}
