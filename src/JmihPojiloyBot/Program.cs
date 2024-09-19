using JmihPojiloyBot.Services;
using System.Diagnostics;

class Program
{
    //Default parameters
    private static string requestUrl = "https://www.cruclub.ru/Data/Json/System/CosFeedProxy.ashx";

    private static string downloadsPath = "Downloads";
    private static int interval = 5;
    private static int executionTime = 60;

    private static List<string> parameters = new List<string>
    {
        "pricing.PInd",
        "pricing.MyAllinc",
        "pricing.MyCruise",
        "catalog",
        "itinerary"
    }; 

    static async Task Main(string[] args)
    {
        Console.WriteLine("JP_BOT Started!");

        //Prepare
        if (args.Length > 0)
        {
            Prepare(args);
        }

        string[] requests = new string[parameters.Count];
        for (int i = 0; i < parameters.Count; i++)
        {
            requests[i] = $"{requestUrl}?param={parameters[i]}";
        }

        TimeSpan retryInterval = TimeSpan.FromMinutes(interval);
        TimeSpan executionTimeout = TimeSpan.FromMinutes(executionTime);

        var httpClient = HttpService.GetHttpClient();
        var getUrlsService = new GetUrlsService(httpClient);
        var downloadService = new DownloadService(httpClient,  retryInterval, downloadsPath);

        using var ctsUrl = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var ctUrl = ctsUrl.Token;

        var fetchGetTasks = requests.Select(r => getUrlsService.GetUrlsAsync(r, ctUrl)).ToArray();
        var urlsModelsResult = await Task.WhenAll(fetchGetTasks);

        foreach (var model in urlsModelsResult)
        {
            if (model?.error != null || model?.url == null)
            {
                Console.WriteLine($"{DateTime.Now} {model?.description} - {model?.error} - ERROR");
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

        Console.WriteLine();

        foreach (var stat in downloadService.statisticModels)
        {
            Console.WriteLine(stat.Value.ToString());
        }

        Console.WriteLine(
            $"\n[DESCRIPTION] completed {results.Count(x => x == 1)}/{urlsModelsResult.Count()} " +
            $"time {stopWatch.Elapsed.ToString(@"hh\:mm\:ss")}");
    }

    static void Prepare(string[] args)
    {
        downloadsPath = args.FirstOrDefault(arg => arg.StartsWith("--downloads="))?.Split('=')[1] ?? downloadsPath;

        interval = args
            .Where(arg => arg.StartsWith("--interval="))
            .Select(arg => int.TryParse(arg.Split('=')[1], out int parsedInterval) ? parsedInterval : (int?)null)
            .FirstOrDefault() ?? interval;

        executionTime = args
            .Where(arg => arg.StartsWith("--execution="))
            .Select(arg => int.TryParse(arg.Split('=')[1], out int parsedExecutionTime) ? parsedExecutionTime : (int?)null)
            .FirstOrDefault() ?? executionTime;

        var additionalParameters = args
            .Where(arg => arg.StartsWith("--parameter="))
            .Select(arg => arg.Split('=')[1])
            .ToList();

        if (additionalParameters.Any())
        {
            parameters = additionalParameters;
        }
    }
}