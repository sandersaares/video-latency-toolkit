using System.Net.Http;

namespace Vltk.Common
{
    public sealed class DummyHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }
}
