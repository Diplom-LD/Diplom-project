namespace OrderService.Services.GeoLocation.HTTPClient
{
    public class HttpClientWrapper(HttpClient httpClient) : IHttpClient
    {
        private readonly HttpClient _httpClient = httpClient;

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            return _httpClient.SendAsync(request);
        }
    }
}
