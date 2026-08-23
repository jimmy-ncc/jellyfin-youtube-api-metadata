namespace Jellyfin.Plugin.YoutubeApiMetadata
{
    public class Constants
    {
        public const string PluginName = "YoutubeApiMetadata";
        public const string PluginGuid = "338cccea-4c27-474e-8934-4c7c3737d034";
        public const string ChannelUrl = "https://www.youtube.com/channel/{0}";
        public const string VideoUrl = "https://www.youtube.com/watch?v={0}";

        /// <summary>
        /// Cache subdirectory name under Jellyfin's CachePath. Deliberately different from the
        /// old ankenyr/jellyfin-youtube-metadata-plugin's "youtubemetadata" folder so both plugins
        /// can run side by side without fighting over the same cache files.
        /// </summary>
        public const string CacheDirectoryName = "youtubeapimetadata";

        public const string YTCHANNEL_RE = @"(?<=\[)[a-zA-Z0-9\-_]{24}(?=\])";
        public const string YTID_RE = @"(?<=\[)[a-zA-Z0-9\-_]{11}(?=\])";
    }
}
