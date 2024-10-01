using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace JmihPojiloyBot.Services
{
    public static class HttpService
    {
        public static HttpClient GetHttpClient(string proxy)
        {
            var services = new ServiceCollection();
            HttpClientHandler httpClientHandler;

            if (string.IsNullOrEmpty(proxy))
            {
                httpClientHandler = new HttpClientHandler();
            }
            else
            {
                // Если прокси указан, создаем HttpClient с прокси
                httpClientHandler = new HttpClientHandler
                {
                    Proxy = new WebProxy(proxy, true), // Устанавливаем прокси
                    UseProxy = true, // Использовать прокси
                };
            }
            
            services.AddHttpClient("Client")
                .ConfigurePrimaryHttpMessageHandler(() => httpClientHandler);

            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
            
            var httpClient = httpClientFactory?.CreateClient("Client");

            return httpClient!;
        }
    }
}
