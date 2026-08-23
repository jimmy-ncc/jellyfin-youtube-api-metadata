using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.YouTube.v3.Data;
using MediaBrowser.Common.Configuration;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Caching
{
    /// <inheritdoc cref="IMetadataCache" />
    public sealed class FileMetadataCache : IMetadataCache
    {
        private readonly string _cacheRoot;
        private readonly Func<int> _getCacheExpirationDays;

        public FileMetadataCache(IApplicationPaths applicationPaths, Func<int> getCacheExpirationDays)
        {
            _cacheRoot = Path.Combine(applicationPaths.CachePath, Constants.CacheDirectoryName);
            _getCacheExpirationDays = getCacheExpirationDays;
        }

        public Task<Video?> GetVideoAsync(string videoId, CancellationToken cancellationToken)
            => ReadAsync<Video>(GetPath(videoId, "video.json"), cancellationToken);

        public Task SaveVideoAsync(string videoId, Video video, CancellationToken cancellationToken)
            => WriteAsync(GetPath(videoId, "video.json"), video, cancellationToken);

        public Task<Channel?> GetChannelAsync(string channelId, CancellationToken cancellationToken)
            => ReadAsync<Channel>(GetPath(channelId, "channel.json"), cancellationToken);

        public Task SaveChannelAsync(string channelId, Channel channel, CancellationToken cancellationToken)
            => WriteAsync(GetPath(channelId, "channel.json"), channel, cancellationToken);

        private string GetPath(string id, string fileName) => Path.Combine(_cacheRoot, id, fileName);

        private async Task<T?> ReadAsync<T>(string path, CancellationToken cancellationToken)
            where T : class
        {
            var file = new FileInfo(path);
            if (!file.Exists)
            {
                return null;
            }

            var expiration = TimeSpan.FromDays(_getCacheExpirationDays());
            if (DateTime.UtcNow - file.LastWriteTimeUtc > expiration)
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(json);
        }

        private async Task WriteAsync<T>(string path, T value, CancellationToken cancellationToken)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonConvert.SerializeObject(value);
            await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
        }
    }
}
