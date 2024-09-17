using JmihPojiloyBot.Loggers;
using JmihPojiloyBot.Services;
using System.Diagnostics;

class Program
{
    private static readonly TimeSpan retryInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan executionTimeout = TimeSpan.FromHours(1);
    private static readonly string requestUrl = "https://www.cruclub.ru/Data/Json/System/CosFeedProxy.ashx";
    private static readonly string[] requests = new[]
    {
            $"{requestUrl}?param=pricing.{1}",
            $"{requestUrl}?param=pricing.{2}",
            $"{requestUrl}?param=pricing.{3}",
            $"{requestUrl}?param=pricing.{4}",
            $"{requestUrl}?param=catalog",
            $"{requestUrl}?param=itinerary"
        };


    static async Task Main(string[] args)
    {
        var httpClient = HttpService.GetHttpClient();
        var logger = new Logger();
        var getUrlsService = new GetUrlsService(httpClient, logger);
        var downloadService = new DownloadService(httpClient, logger, retryInterval);

        using var ctsUrl = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ctUrl = ctsUrl.Token;

        var fetchGetTasks = requests.Select(r => getUrlsService.GetUrlsAsync(r, ctUrl)).ToArray();
        var urlsModelsResult = await Task.WhenAll(fetchGetTasks);

        foreach (var model in urlsModelsResult)
        {
            if (model?.error != null || model?.url == null)
            {
                Console.WriteLine($"{DateTime.Now} {model?.description} - {model?.error}");
                continue;
            }
            Console.WriteLine($"{DateTime.Now} {model?.description} - OK");
        }

        Console.WriteLine();

        using var cts = new CancellationTokenSource(executionTimeout);
        var ct = cts.Token;

        var stopWatch = Stopwatch.StartNew();

        var fetchTasks = urlsModelsResult.Select(model => downloadService.DownloadFileAsync(model!, ct)).ToArray();

        var results = await Task.WhenAll(fetchTasks);

        stopWatch.Stop();

        foreach (var result in results)
        {
            Console.WriteLine(result);
        }

        Console.WriteLine(
            $"\n[DESCRIPTION] completed {urlsModelsResult.Count()}/{results.Count()} " +
            $"time {stopWatch.Elapsed.ToString(@"hh\:mm\:ss")}");
    }

}