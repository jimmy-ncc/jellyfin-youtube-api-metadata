using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Google.Apis.YouTube.v3.Data;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using YTVideo = Google.Apis.YouTube.v3.Data.Video;

namespace Jellyfin.Plugin.YoutubeApiMetadata
{
    public static class Utils
    {
        /// <summary>
        /// Extracts a YouTube video ID (11 chars) or channel ID (24 chars) from a file name,
        /// expected between square brackets, e.g. "Some Video [dQw4w9WgXcQ].mkv".
        /// </summary>
        public static string GetYTID(string name)
        {
            var videoMatch = Regex.Match(name, Constants.YTID_RE);
            if (videoMatch.Success)
            {
                return videoMatch.Value;
            }

            var channelMatch = Regex.Match(name, Constants.YTCHANNEL_RE);
            return channelMatch.Success ? channelMatch.Value : string.Empty;
        }

        /// <summary>
        /// Picks the highest-resolution thumbnail URL available, or null if none is.
        /// </summary>
        public static string? GetBestThumbnailUrl(ThumbnailDetails? thumbnails)
        {
            if (thumbnails == null)
            {
                return null;
            }

            return thumbnails.Maxres?.Url
                ?? thumbnails.Standard?.Url
                ?? thumbnails.High?.Url
                ?? thumbnails.Medium?.Url
                ?? thumbnails.Default__?.Url;
        }

        /// <summary>
        /// Resolves a channel ID from (in order): an already-stored provider ID, the folder name
        /// convention "[channelId]", or (last resort) a bracketed ID in the series display name.
        /// </summary>
        public static string? ResolveChannelId(IReadOnlyDictionary<string, string> providerIds, string? path, string? name)
        {
            if (providerIds != null && providerIds.TryGetValue(Constants.PluginName, out var id) && !string.IsNullOrEmpty(id))
            {
                return id;
            }

            var fromPath = GetYTID(path ?? string.Empty);
            if (!string.IsNullOrEmpty(fromPath))
            {
                return fromPath;
            }

            return !string.IsNullOrEmpty(name) ? GetYTID(name) : null;
        }

        /// <summary>
        /// Maps a YouTube Data API video to a Jellyfin Episode (a video = one episode of its channel's "show").
        /// </summary>
        public static MetadataResult<Episode> VideoToEpisode(YTVideo video)
        {
            var snippet = video.Snippet;
            var item = new Episode
            {
                Name = snippet.Title,
                Overview = snippet.Description,
                IndexNumber = 1,
                ParentIndexNumber = 1
            };
            item.ProviderIds.Add(Constants.PluginName, video.Id);

            if (snippet.PublishedAtDateTimeOffset.HasValue)
            {
                var published = snippet.PublishedAtDateTimeOffset.Value.UtcDateTime;
                item.PremiereDate = published;
                item.ProductionYear = published.Year;
                item.ForcedSortName = $"{published:yyyyMMdd}-{snippet.Title}";
            }

            if (snippet.Tags != null && snippet.Tags.Count > 0)
            {
                item.Tags = snippet.Tags.ToArray();
            }

            if (!string.IsNullOrEmpty(video.ContentDetails?.Duration))
            {
                try
                {
                    var duration = XmlConvert.ToTimeSpan(video.ContentDetails.Duration);

                    // Live/upcoming videos report a zero duration (e.g. "P0D") until they actually
                    // air; treat that as "unknown" rather than a genuine zero-length video.
                    if (duration > TimeSpan.Zero)
                    {
                        item.RunTimeTicks = duration.Ticks;
                    }
                }
                catch (FormatException)
                {
                    // Unparsable duration string; skip silently.
                }
            }

            var result = new MetadataResult<Episode> { HasMetadata = true, Item = item };

            if (!string.IsNullOrEmpty(snippet.ChannelTitle))
            {
                var person = new PersonInfo
                {
                    Name = snippet.ChannelTitle,
                    Type = PersonKind.Director,
                    ProviderIds = new Dictionary<string, string> { { Constants.PluginName, snippet.ChannelId } }
                };
                result.People = new List<PersonInfo> { person };
            }

            return result;
        }

        /// <summary>
        /// Maps a YouTube Data API channel to a Jellyfin Series (a channel = one "show").
        /// </summary>
        public static MetadataResult<Series> ChannelToSeries(Channel channel)
        {
            var snippet = channel.Snippet;
            var item = new Series
            {
                Name = snippet.Title,
                Overview = snippet.Description
            };
            item.ProviderIds.Add(Constants.PluginName, channel.Id);

            if (snippet.PublishedAtDateTimeOffset.HasValue)
            {
                var published = snippet.PublishedAtDateTimeOffset.Value.UtcDateTime;
                item.PremiereDate = published;
                item.ProductionYear = published.Year;
            }

            return new MetadataResult<Series> { HasMetadata = true, Item = item };
        }
    }
}
