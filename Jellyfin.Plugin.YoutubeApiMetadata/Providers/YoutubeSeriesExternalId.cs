using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Providers
{
    /// <summary>
    /// Exposes the stored YouTube channel ID as an "External ID" field in the Series metadata editor.
    /// </summary>
    public class YoutubeSeriesExternalId : IExternalId
    {
        public string ProviderName => "YouTube";

        public string Key => Constants.PluginName;

        public ExternalIdMediaType? Type => ExternalIdMediaType.Series;

        public bool Supports(IHasProviderIds item) => item is Series;
    }
}
