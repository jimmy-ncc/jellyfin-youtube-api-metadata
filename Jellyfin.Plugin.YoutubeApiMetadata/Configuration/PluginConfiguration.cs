using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Gets or sets the YouTube Data API v3 key, created in Google Cloud Console.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the number of days a cached API response stays valid before being refreshed.
        /// </summary>
        public int CacheExpirationDays { get; set; }

        public PluginConfiguration()
        {
            ApiKey = string.Empty;
            CacheExpirationDays = 30;
        }
    }
}
