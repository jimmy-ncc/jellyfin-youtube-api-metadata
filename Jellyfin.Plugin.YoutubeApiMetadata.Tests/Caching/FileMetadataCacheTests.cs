using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using Jellyfin.Plugin.YoutubeApiMetadata.Caching;
using MediaBrowser.Common.Configuration;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Tests.Caching
{
    public class FileMetadataCacheTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly FileMetadataCache _cache;

        public FileMetadataCacheTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "yt-api-metadata-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            var appPaths = new Mock<IApplicationPaths>();
            appPaths.SetupGet(a => a.CachePath).Returns(_tempDir);

            _cache = new FileMetadataCache(appPaths.Object, () => 10);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        [Fact]
        public async Task GetVideoAsync_ReturnsNullWhenNothingCached()
        {
            var result = await _cache.GetVideoAsync("dQw4w9WgXcQ", CancellationToken.None);
            Assert.Null(result);
        }

        [Fact]
        public async Task SaveThenGetVideoAsync_RoundTrips()
        {
            var video = new Video { Id = "dQw4w9WgXcQ", Snippet = new VideoSnippet { Title = "Never Gonna Give You Up" } };

            await _cache.SaveVideoAsync(video.Id, video, CancellationToken.None);
            var result = await _cache.GetVideoAsync(video.Id, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("dQw4w9WgXcQ", result!.Id);
            Assert.Equal("Never Gonna Give You Up", result.Snippet.Title);
        }

        [Fact]
        public async Task SaveThenGetChannelAsync_RoundTrips()
        {
            var channel = new Channel { Id = "UCuAXFkgsw1L7xaCfnd5JJOw", Snippet = new ChannelSnippet { Title = "Rick Astley" } };

            await _cache.SaveChannelAsync(channel.Id, channel, CancellationToken.None);
            var result = await _cache.GetChannelAsync(channel.Id, CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("Rick Astley", result!.Snippet.Title);
        }

        [Fact]
        public async Task GetVideoAsync_ReturnsNullWhenCacheEntryIsExpired()
        {
            var video = new Video { Id = "dQw4w9WgXcQ", Snippet = new VideoSnippet { Title = "Old" } };
            await _cache.SaveVideoAsync(video.Id, video, CancellationToken.None);

            var cachedFilePath = Path.Combine(_tempDir, Constants.CacheDirectoryName, video.Id, "video.json");
            File.SetLastWriteTimeUtc(cachedFilePath, DateTime.UtcNow.AddDays(-11));

            var result = await _cache.GetVideoAsync(video.Id, CancellationToken.None);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetVideoAsync_ReturnsCachedEntryWithinExpiration()
        {
            var video = new Video { Id = "dQw4w9WgXcQ", Snippet = new VideoSnippet { Title = "Still fresh" } };
            await _cache.SaveVideoAsync(video.Id, video, CancellationToken.None);

            var cachedFilePath = Path.Combine(_tempDir, Constants.CacheDirectoryName, video.Id, "video.json");
            File.SetLastWriteTimeUtc(cachedFilePath, DateTime.UtcNow.AddDays(-9));

            var result = await _cache.GetVideoAsync(video.Id, CancellationToken.None);
            Assert.NotNull(result);
        }
    }
}
