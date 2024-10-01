using System.Diagnostics;
using JmihPojiloyBot.Services;
using JmihPojiloyBot_3_1.Services;

namespace JmihPojiloyBot_3_1;

internal static class Program
{
    //Default parameters
    private const string RequestUrl = "https://www.cruclub.ru/Data/Json/System/CosFeedProxy.ashx";

    private static string _downloadsPath = "Downloads";
    private static int _getUrlsInterval = 5;
    private static int _getUrlsExecutionTime = 25;
    private static int _downloadInterval = 10;
    private static int _downloadExecutionTime = 100;

    private static string _proxy = string.Empty;

    private static List<string> _parameters = 
    [
        "pricing.PInd",
        "pricing.MyAllinc",
        "pricing.MyCruise",
        "catalog",
        "itinerary"
    ];

    private static async Task Main(string[] args)
    {
        if (args.Contains("-h"))
        {
            PrintHelp();
            return;
        }

        Console.WriteLine("JP_BOT Started!");

        //Configuration
        if (args.Length > 0)
        {
            Configuration(args);
        }

        var requests = new string[_parameters.Count];
        for (var i = 0; i < _parameters.Count; i++)
        {
            requests[i] = $"{RequestUrl}?param={_parameters[i]}";
        }

        var downloadsRetryInterval = TimeSpan.FromMinutes(_downloadInterval);
        var downloadsExecutionTimeout = TimeSpan.FromMinutes(_downloadExecutionTime);

        var getUrlsRetryInterval = TimeSpan.FromMinutes(_getUrlsInterval);
        var getUrlsExecutionTimeout = TimeSpan.FromMinutes(_getUrlsExecutionTime);

        var httpClient = HttpService.GetHttpClient(_proxy);
        var getUrlsService = new GetUrlsService(httpClient, getUrlsRetryInterval);
        var downloadService = new DownloadService(httpClient, downloadsRetryInterval, _downloadsPath);

        using var ctsUrl = new CancellationTokenSource(getUrlsExecutionTimeout);
        var ctUrl = ctsUrl.Token;
        
        Console.WriteLine("\nGET urls for downloads\n");

        var stopWatch = Stopwatch.StartNew();

        var fetchGetTasks = requests
            .Select(r => getUrlsService.GetUrlsAsync(r, ctUrl)).ToArray();
        var urlsModelsResult = await Task.WhenAll(fetchGetTasks);

        foreach (var model in urlsModelsResult)
        {
            if (model?.error != null || model?.url == null)
            {
                Console.WriteLine($"{DateTime.Now} {model?.Description} - {model?.error} - ERROR");
                continue;
            }
            Console.WriteLine($"{DateTime.Now} GET url: {model.url} Short name: {model?.Description} - OK");
        }

        Console.WriteLine();

        using var cts = new CancellationTokenSource(downloadsExecutionTimeout);
        var ct = cts.Token;

        Console.WriteLine("\nStart downloads\n");
        
        var fetchTasks = urlsModelsResult
            .Select(model => downloadService.DownloadFileAsync(model!, ct)).ToArray();

        var results = await Task.WhenAll(fetchTasks);

        stopWatch.Stop();

        Console.WriteLine();

        foreach (var stat in downloadService.StatisticModels)
        {
            Console.WriteLine(stat.Value.ToString());
        }

        await downloadService.SaveLogs();

        Console.WriteLine(
            $"\n[DESCRIPTION] completed {results.Count(x => x == 1)}/{urlsModelsResult.Length} " +
            $@"time {stopWatch.Elapsed:hh\:mm\:ss}");
    }

    private static void Configuration(string[] args)
    {
        _downloadsPath = args
            .FirstOrDefault(arg => arg.StartsWith("--path="))?.Split('=')[1] ?? _downloadsPath;

        _downloadInterval = args
            .Where(arg => arg.StartsWith("--downloadInterval="))
            .Select(arg =>
                int.TryParse(arg.Split('=')[1], out var parsedInterval) ? parsedInterval : (int?)null)
            .FirstOrDefault() ?? _downloadInterval;

        _downloadExecutionTime = args
            .Where(arg => arg.StartsWith("--execution="))
            .Select(arg =>
                int.TryParse(arg.Split('=')[1], out var parsedExecutionTime) ? parsedExecutionTime : (int?)null)
            .FirstOrDefault() ?? _downloadExecutionTime;

        _getUrlsInterval = args
            .Where(arg => arg.StartsWith("--getUrlInterval="))
            .Select(arg =>
                int.TryParse(arg.Split('=')[1], out var parsedGetUrlInterval) ? parsedGetUrlInterval : (int?)null)
            .FirstOrDefault() ?? _getUrlsInterval;

        _getUrlsExecutionTime = args
            .Where(arg => arg.StartsWith("--getUrlExecution="))
            .Select(arg =>
                int.TryParse(arg.Split('=')[1], out var parsedGetExecution) ? parsedGetExecution : (int?)null)
            .FirstOrDefault() ?? _getUrlsExecutionTime;
        
        _proxy = args
            .FirstOrDefault(arg => arg.StartsWith("--proxy="))?.Split('=')[1] ?? _proxy;

        var additionalParameters = args
            .Where(arg => arg.StartsWith("--parameter="))
            .Select(arg => arg.Split('=')[1])
            .ToList();

        if (additionalParameters.Any())
        {
            _parameters = additionalParameters;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("JP_BOT Command-Line Usage:");
        Console.WriteLine("  -h                          Display this help message.");
        Console.WriteLine("  --path=<path>               Set the download path (default: 'Downloads').");
        Console.WriteLine("  --downloadInterval=<int>    Set the interval for downloads in minutes (default: 10).");
        Console.WriteLine("  --execution=<int>           Set the execution time for downloads in minutes (default: 100).");
        Console.WriteLine("  --getUrlInterval=<int>      Set the interval for GetUrls in minutes (default: 5).");
        Console.WriteLine("  --getUrlExecution=<int>     Set the execution time for GetUrls in minutes (default: 25).");
        Console.WriteLine("  --parameter=<parameter>     Add a custom parameter to the requests.");
        Console.WriteLine("  --proxy=<string>            Add proxy.");
    }
}