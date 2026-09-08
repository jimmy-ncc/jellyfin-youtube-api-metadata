using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Providers
{
    /// <summary>
    /// Builds the clickable "YouTube" external link shown on Series and Episode pages, from the
    /// channel/video ID already stored in <see cref="BaseItem.ProviderIds"/>.
    /// </summary>
    public class YoutubeExternalUrlProvider : IExternalUrlProvider
    {
        public string Name => "YouTube";

        public IEnumerable<string> GetExternalUrls(BaseItem item)
        {
            if (!item.ProviderIds.TryGetValue(Constants.PluginName, out var id) || string.IsNullOrEmpty(id))
            {
                yield break;
            }

            switch (item)
            {
                case Episode:
                    yield return string.Format(Constants.VideoUrl, id);
                    break;
                case Series:
                    yield return string.Format(Constants.ChannelUrl, id);
                    break;
            }
        }
    }
}
