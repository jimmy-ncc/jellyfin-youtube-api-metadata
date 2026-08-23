using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Http;

namespace Jellyfin.Plugin.YoutubeApiMetadata.Tests.YouTube
{
    /// <summary>
    /// Returns a canned JSON response for every request, regardless of URL, and records the last
    /// request so tests can assert on how the client built it (query params, API key, ...).
    /// </summary>
    internal sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public FakeHttpMessageHandler(string jsonResponse)
            : this(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            })
        {
        }

        public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responder(request));
        }
    }

    /// <summary>
    /// Wraps a <see cref="FakeHttpMessageHandler"/> so it can be plugged into
    /// <c>BaseClientService.Initializer.HttpClientFactory</c>, bypassing real network calls.
    /// </summary>
    internal sealed class FakeGoogleHttpClientFactory : Google.Apis.Http.IHttpClientFactory
    {
        private readonly HttpMessageHandler _innerHandler;

        public FakeGoogleHttpClientFactory(HttpMessageHandler innerHandler)
        {
            _innerHandler = innerHandler;
        }

        public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args)
        {
            var configurableHandler = new ConfigurableMessageHandler(_innerHandler);
            var client = new ConfigurableHttpClient(configurableHandler);
            foreach (var initializer in args.Initializers)
            {
                initializer.Initialize(client);
            }

            return client;
        }
    }
}
