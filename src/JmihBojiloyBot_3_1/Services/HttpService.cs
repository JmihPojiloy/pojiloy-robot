using Microsoft.Extensions.DependencyInjection;

namespace JmihPojiloyBot.Services
{
    public static class HttpService
    {
        public static HttpClient GetHttpClient()
        {
            var services = new ServiceCollection();
            services.AddHttpClient();

            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            var httpClient = httpClientFactory?.CreateClient();

            return httpClient!;
        }
    }
}
