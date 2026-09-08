using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Providers
{
    /// <summary>
    /// Exposes the stored YouTube video ID as an "External ID" field in the Episode metadata editor.
    /// </summary>
    public class YoutubeEpisodeExternalId : IExternalId
    {
        public string ProviderName => "YouTube";

        public string Key => Constants.PluginName;

        public ExternalIdMediaType? Type => ExternalIdMediaType.Episode;

        public bool Supports(IHasProviderIds item) => item is Episode;
    }
}
