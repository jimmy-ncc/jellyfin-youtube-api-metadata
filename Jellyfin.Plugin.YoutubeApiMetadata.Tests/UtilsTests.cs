using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.YouTube.v3.Data;
using Xunit;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Tests
{
    public class UtilsTests
    {
        [Theory]
        [InlineData("3Blue1Brown - 20190113 - The_most_unexpected_answer_to_a_counting_puzzle [HEfHFsfGXjs].mkv", "HEfHFsfGXjs")]
        [InlineData("Foo", "")]
        [InlineData("3Blue1Brown - NA - 3Blue1Brown_-_Videos [UCYO_jab_esuFRV4b17AJtAw].info.json", "UCYO_jab_esuFRV4b17AJtAw")]
        public void GetYTIDTest(string fileName, string expected)
        {
            Assert.Equal(expected, Utils.GetYTID(fileName));
        }
    }

    public class ConstantsTests
    {
        [Fact]
        public void PluginGuidIsValid()
        {
            Assert.True(System.Guid.TryParse(Constants.PluginGuid, out _));
        }

        [Fact]
        public void VideoUrlFormatsId()
        {
            Assert.Equal("https://www.youtube.com/watch?v=dQw4w9WgXcQ", string.Format(Constants.VideoUrl, "dQw4w9WgXcQ"));
        }

        [Fact]
        public void ChannelUrlFormatsId()
        {
            Assert.Equal("https://www.youtube.com/channel/UCYO_jab_esuFRV4b17AJtAw", string.Format(Constants.ChannelUrl, "UCYO_jab_esuFRV4b17AJtAw"));
        }
    }

    public class UtilsMappingTests
    {
        [Fact]
        public void VideoToEpisode_MapsCoreFields()
        {
            var video = new Video
            {
                Id = "dQw4w9WgXcQ",
                Snippet = new VideoSnippet
                {
                    Title = "Never Gonna Give You Up",
                    Description = "The official video.",
                    ChannelId = "UCuAXFkgsw1L7xaCfnd5JJOw",
                    ChannelTitle = "Rick Astley",
                    PublishedAtDateTimeOffset = new DateTimeOffset(2009, 10, 25, 6, 57, 33, TimeSpan.Zero),
                    Tags = new List<string> { "80s", "pop" }
                },
                ContentDetails = new VideoContentDetails { Duration = "PT3M33S" }
            };

            var result = Utils.VideoToEpisode(video);

            Assert.True(result.HasMetadata);
            Assert.Equal("Never Gonna Give You Up", result.Item.Name);
            Assert.Equal("The official video.", result.Item.Overview);
            Assert.Equal(2009, result.Item.ProductionYear);
            Assert.Equal(new DateTime(2009, 10, 25, 6, 57, 33, DateTimeKind.Utc), result.Item.PremiereDate);
            Assert.Equal("20091025-Never Gonna Give You Up", result.Item.ForcedSortName);
            Assert.Null(result.Item.IndexNumber);
            Assert.Null(result.Item.ParentIndexNumber);
            Assert.Equal("dQw4w9WgXcQ", result.Item.ProviderIds[Constants.PluginName]);
            Assert.Equal(TimeSpan.FromSeconds(213).Ticks, result.Item.RunTimeTicks);
            Assert.Contains("80s", result.Item.Tags);

            Assert.NotNull(result.People);
            Assert.Equal("Rick Astley", result.People[0].Name);
            Assert.Equal("UCuAXFkgsw1L7xaCfnd5JJOw", result.People[0].ProviderIds[Constants.PluginName]);
        }

        [Fact]
        public void VideoToEpisode_IgnoresUnparsableDuration()
        {
            var video = new Video
            {
                Id = "liveVideoId",
                Snippet = new VideoSnippet { Title = "Live stream" },
                ContentDetails = new VideoContentDetails { Duration = "P0D" }
            };

            var result = Utils.VideoToEpisode(video);

            Assert.True(result.HasMetadata);
            Assert.Null(result.Item.RunTimeTicks);
        }

        [Fact]
        public void ChannelToSeries_MapsCoreFields()
        {
            var channel = new Channel
            {
                Id = "UCuAXFkgsw1L7xaCfnd5JJOw",
                Snippet = new ChannelSnippet
                {
                    Title = "Rick Astley",
                    Description = "The official channel.",
                    PublishedAtDateTimeOffset = new DateTimeOffset(2006, 3, 14, 0, 0, 0, TimeSpan.Zero)
                }
            };

            var result = Utils.ChannelToSeries(channel);

            Assert.True(result.HasMetadata);
            Assert.Equal("Rick Astley", result.Item.Name);
            Assert.Equal("The official channel.", result.Item.Overview);
            Assert.Equal("UCuAXFkgsw1L7xaCfnd5JJOw", result.Item.ProviderIds[Constants.PluginName]);
            Assert.Equal(2006, result.Item.ProductionYear);
        }

        [Fact]
        public void GetBestThumbnailUrl_PrefersHighestResolution()
        {
            var thumbnails = new ThumbnailDetails
            {
                Default__ = new Thumbnail { Url = "default.jpg" },
                High = new Thumbnail { Url = "high.jpg" }
            };

            Assert.Equal("high.jpg", Utils.GetBestThumbnailUrl(thumbnails));
        }

        [Fact]
        public void GetBestThumbnailUrl_ReturnsNullWhenNoThumbnails()
        {
            Assert.Null(Utils.GetBestThumbnailUrl(null));
        }
    }

    public class ComputeEpisodeIndexTests
    {
        [Fact]
        public void ReturnsOne_WhenNoSiblings()
        {
            var index = Utils.ComputeEpisodeIndex(
                "current1111",
                new DateTime(2024, 6, 15),
                Array.Empty<(string, DateTime)>());

            Assert.Equal(1, index);
        }

        [Fact]
        public void RanksByPremiereDate_AcrossAllSiblings_RegardlessOfYear()
        {
            var siblings = new[]
            {
                ("olderVid111", new DateTime(2020, 1, 1)),
                ("newerVid111", new DateTime(2025, 12, 1))
            };

            var index = Utils.ComputeEpisodeIndex("current1111", new DateTime(2024, 6, 15), siblings);

            Assert.Equal(2, index);
        }

        [Fact]
        public void ExcludesItselfFromSiblingList()
        {
            var siblings = new[]
            {
                ("current1111", new DateTime(2024, 1, 1)),
                ("otherVid1111", new DateTime(2024, 3, 1))
            };

            var index = Utils.ComputeEpisodeIndex("current1111", new DateTime(2024, 6, 15), siblings);

            Assert.Equal(2, index);
        }

        [Fact]
        public void BreaksTiesOnSameDate_ByVideoIdOrdinal()
        {
            var indexAfter = Utils.ComputeEpisodeIndex(
                "zzzzzzzzzzz",
                new DateTime(2024, 6, 15),
                new[] { ("aaaaaaaaaaa", new DateTime(2024, 6, 15)) });

            var indexBefore = Utils.ComputeEpisodeIndex(
                "aaaaaaaaaaa",
                new DateTime(2024, 6, 15),
                new[] { ("zzzzzzzzzzz", new DateTime(2024, 6, 15)) });

            Assert.Equal(2, indexAfter);
            Assert.Equal(1, indexBefore);
        }

        [Fact]
        public void NeverRepeatsAcrossManySiblings()
        {
            var siblings = Enumerable.Range(0, 20)
                .Select(i => ($"vid{i:D8}", new DateTime(2020, 1, 1).AddDays(i)))
                .ToArray();

            var indexes = siblings
                .Select(s => Utils.ComputeEpisodeIndex(s.Item1, s.Item2, siblings))
                .ToList();

            Assert.Equal(indexes.Count, indexes.Distinct().Count());
        }
    }
}
