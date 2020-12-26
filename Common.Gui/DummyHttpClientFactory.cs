using System.Net.Http;

namespace Vltk.Common.Gui
{
    public sealed class DummyHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }
}
