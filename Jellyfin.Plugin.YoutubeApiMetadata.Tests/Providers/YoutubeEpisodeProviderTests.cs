using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.YoutubeApiMetadata.Providers;
using Jellyfin.Plugin.YoutubeApiMetadata.YouTube;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Moq;
using Xunit;
using InternalItemsQuery = MediaBrowser.Controller.Entities.InternalItemsQuery;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Tests.Providers
{
    public class YoutubeEpisodeProviderTests
    {
        private static readonly Video SampleVideo = new()
        {
            Id = "dQw4w9WgXcQ",
            Snippet = new VideoSnippet { Title = "Never Gonna Give You Up", Description = "The official video." }
        };

        /// <summary>
        /// A loose <see cref="ILibraryManager"/> mock with no channel/sibling matches configured;
        /// suitable for tests that don't exercise the episode-numbering path.
        /// </summary>
        private static Mock<ILibraryManager> EmptyLibraryManager()
        {
            var mock = new Mock<ILibraryManager>();
            mock.Setup(m => m.GetItemList(It.IsAny<InternalItemsQuery>())).Returns(Array.Empty<MediaBrowser.Controller.Entities.BaseItem>());
            return mock;
        }

        [Fact]
        public async Task GetMetadata_ResolvesVideoIdFromFileName()
        {
            var resolver = new Mock<IYoutubeMetadataResolver>();
            resolver.Setup(r => r.GetVideoAsync("dQw4w9WgXcQ", It.IsAny<CancellationToken>())).ReturnsAsync(SampleVideo);

            var provider = new YoutubeEpisodeProvider(resolver.Object, EmptyLibraryManager().Object);
            var info = new EpisodeInfo { Path = "Some Video [dQw4w9WgXcQ].mkv" };

            var result = await provider.GetMetadata(info, CancellationToken.None);

            Assert.True(result.HasMetadata);
            Assert.Equal("Never Gonna Give You Up", result.Item.Name);
        }

        [Fact]
        public async Task GetMetadata_ReturnsEmptyResult_WhenPathHasNoId()
        {
            var resolver = new Mock<IYoutubeMetadataResolver>(MockBehavior.Strict);

            var provider = new YoutubeEpisodeProvider(resolver.Object, EmptyLibraryManager().Object);
            var info = new EpisodeInfo { Path = "no id here.mkv" };

            var result = await provider.GetMetadata(info, CancellationToken.None);

            Assert.False(result.HasMetadata);
            resolver.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetMetadata_ReturnsEmptyResult_WhenVideoNotFound()
        {
            var resolver = new Mock<IYoutubeMetadataResolver>();
            resolver.Setup(r => r.GetVideoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Video?)null);

            var provider = new YoutubeEpisodeProvider(resolver.Object, EmptyLibraryManager().Object);
            var info = new EpisodeInfo { Path = "Deleted Video [aaaaaaaaaaa].mkv" };

            var result = await provider.GetMetadata(info, CancellationToken.None);

            Assert.False(result.HasMetadata);
        }

        [Fact]
        public async Task GetMetadata_SetsEpisodeOne_WhenChannelHasNoOtherKnownVideos()
        {
            var video = new Video
            {
                Id = "dQw4w9WgXcQ",
                Snippet = new VideoSnippet
                {
                    Title = "Never Gonna Give You Up",
                    ChannelId = "UCuAXFkgsw1L7xaCfnd5JJOw",
                    PublishedAtDateTimeOffset = new DateTimeOffset(2009, 10, 25, 6, 57, 33, TimeSpan.Zero)
                }
            };

            var resolver = new Mock<IYoutubeMetadataResolver>();
            resolver.Setup(r => r.GetVideoAsync("dQw4w9WgXcQ", It.IsAny<CancellationToken>())).ReturnsAsync(video);

            var provider = new YoutubeEpisodeProvider(resolver.Object, EmptyLibraryManager().Object);
            var info = new EpisodeInfo
            {
                Path = "Some Video [dQw4w9WgXcQ].mkv",
                SeriesProviderIds = new Dictionary<string, string> { { Constants.PluginName, "UCuAXFkgsw1L7xaCfnd5JJOw" } }
            };

            var result = await provider.GetMetadata(info, CancellationToken.None);

            Assert.Equal(1, result.Item.ParentIndexNumber);
            Assert.Equal(1, result.Item.IndexNumber);
        }

        [Fact]
        public async Task GetMetadata_RanksAmongSiblingEpisodes_ByPremiereDate()
        {
            var video = new Video
            {
                Id = "newVideo111",
                Snippet = new VideoSnippet
                {
                    Title = "Third video",
                    ChannelId = "UCuAXFkgsw1L7xaCfnd5JJOw",
                    PublishedAtDateTimeOffset = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero)
                }
            };

            var resolver = new Mock<IYoutubeMetadataResolver>();
            resolver.Setup(r => r.GetVideoAsync("newVideo111", It.IsAny<CancellationToken>())).ReturnsAsync(video);

            var series = new Series { Id = Guid.NewGuid() };
            series.ProviderIds.Add(Constants.PluginName, "UCuAXFkgsw1L7xaCfnd5JJOw");

            var earlier = new Episode { PremiereDate = new DateTime(2020, 1, 1) };
            earlier.ProviderIds.Add(Constants.PluginName, "earlierVid1");

            var later = new Episode { PremiereDate = new DateTime(2025, 12, 1) };
            later.ProviderIds.Add(Constants.PluginName, "laterVid111");

            var libraryManager = new Mock<ILibraryManager>();
            libraryManager
                .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(q => q.IncludeItemTypes.Length == 1 && q.IncludeItemTypes[0] == BaseItemKind.Series)))
                .Returns(new MediaBrowser.Controller.Entities.BaseItem[] { series });
            libraryManager
                .Setup(m => m.GetItemList(It.Is<InternalItemsQuery>(q => q.IncludeItemTypes.Length == 1 && q.IncludeItemTypes[0] == BaseItemKind.Episode)))
                .Returns(new MediaBrowser.Controller.Entities.BaseItem[] { earlier, later });

            var provider = new YoutubeEpisodeProvider(resolver.Object, libraryManager.Object);
            var info = new EpisodeInfo
            {
                Path = "Some Video [newVideo111].mkv",
                SeriesProviderIds = new Dictionary<string, string> { { Constants.PluginName, "UCuAXFkgsw1L7xaCfnd5JJOw" } }
            };

            var result = await provider.GetMetadata(info, CancellationToken.None);

            Assert.Equal(1, result.Item.ParentIndexNumber);
            Assert.Equal(2, result.Item.IndexNumber);
        }

        [Fact]
        public async Task GetSearchResults_UsesStoredProviderIdWhenAvailable()
        {
            var resolver = new Mock<IYoutubeMetadataResolver>();
            resolver.Setup(r => r.GetVideoAsync("dQw4w9WgXcQ", It.IsAny<CancellationToken>())).ReturnsAsync(SampleVideo);

            var provider = new YoutubeEpisodeProvider(resolver.Object, EmptyLibraryManager().Object);
            var info = new EpisodeInfo
            {
                Path = "Some Other File Name.mkv",
                ProviderIds = new Dictionary<string, string> { { Constants.PluginName, "dQw4w9WgXcQ" } }
            };

            var results = await provider.GetSearchResults(info, CancellationToken.None);

            Assert.Single(results);
        }
    }
}
