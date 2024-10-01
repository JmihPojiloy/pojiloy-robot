using System.Collections.Concurrent;
using System.Diagnostics;
using JmihBojiloyBot_3_1.Loggers;
using JmihPojiloyBot_3_1.Models;
using Exception = System.Exception;

namespace JmihPojiloyBot_3_1.Services
{
    public class DownloadService
    {
        private readonly HttpClient httpClient;
        private readonly TimeSpan retryInterval;
        private readonly string downloadsPath;
        
        public readonly ConcurrentDictionary<string, StatisticModel> StatisticModels =
            new ConcurrentDictionary<string, StatisticModel>();

        public DownloadService(HttpClient httpClient, TimeSpan retryInterval, string downloadsPath)
        {
            this.httpClient = httpClient;
            this.retryInterval = retryInterval;
            this.downloadsPath = downloadsPath;
        }
        
        public async Task<int> DownloadFileAsync(UrlModel urlModel, CancellationToken ct)
        {

            var statisticModel = StatisticModels.GetOrAdd(urlModel.Description!, new StatisticModel(urlModel.Description!));

            if (statisticModel == null)
            {
                return 0;
            }

            var stopWatch = Stopwatch.StartNew();

            try
            {
                if (ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();
                }

                if (urlModel.url == null)
                {
                    throw new ArgumentNullException();
                }

                var response = await httpClient.GetAsync(urlModel.url, ct);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    await SaveFile(response, urlModel.Description!);

                    stopWatch.Stop();

                    var elapsed = stopWatch.Elapsed;

                    var logMessage = string.Empty;
                    
                    StatisticModels.AddOrUpdate(urlModel.Description!, statisticModel,
                        (key, existingValue) =>
                        {
                            existingValue.TotalTime += elapsed;
                            existingValue.Tries += 1;
                            existingValue.Description = "download";
                            existingValue.Statistic.TryAdd(
                                existingValue.Tries, (existingValue.Description, existingValue.TotalTime));
                            
                            logMessage = $@"Try: {existingValue.Tries} Name: {key} (Description: {existingValue.Description}, TotalTime: {existingValue.TotalTime:hh\:mm\:ss})";
                            
                            return existingValue;
                        });

                    await Logger.ConsoleLog(logMessage);
                    
                    return 1;
                }

                throw new HttpRequestException($"Fetch {urlModel.Description} returned status {response.StatusCode}");
            }
            catch (TaskCanceledException)
            {
                stopWatch.Stop();
                var elapsed = stopWatch.Elapsed;

                var logMessage = string.Empty;
                
                StatisticModels.AddOrUpdate(urlModel.Description!, statisticModel,
                    (key, existingValue) =>
                    {
                        existingValue.TotalTime += elapsed;
                        existingValue.Tries += 1;
                        existingValue.Description = "TIME IS UP!";
                        existingValue.Statistic.TryAdd(
                            existingValue.Tries, (existingValue.Description, existingValue.TotalTime));
                        
                        logMessage = $@"Try: {existingValue.Tries} Name: {key} (Description: {existingValue.Description}, TotalTime: {existingValue.TotalTime:hh\:mm\:ss})";
                        
                        return existingValue;
                    });

                await Logger.ConsoleLog(logMessage);
                
                return 0;
            }
            catch (HttpRequestException ex)
            {
                stopWatch.Stop();
                var elapsed = stopWatch.Elapsed;

                var logMessage = string.Empty;
                
                StatisticModels.AddOrUpdate(urlModel.Description!, statisticModel,
                    (key, existingValue) =>
                    {
                        existingValue.TotalTime += elapsed;
                        existingValue.Tries += 1;
                        existingValue.Description = $"{ex.Message}. Next try in {retryInterval.TotalMinutes} minutes.";
                        existingValue.Statistic.TryAdd(
                            existingValue.Tries, (existingValue.Description, existingValue.TotalTime));
                        
                        logMessage = $@"Try: {existingValue.Tries} Name: {key} (Description: {existingValue.Description}, TotalTime: {existingValue.TotalTime:hh\:mm\:ss})";
                        
                        return existingValue;
                    });
                
                await Logger.ConsoleLog(logMessage);
                
                await Task.Delay(retryInterval, ct);

                StatisticModels.AddOrUpdate(urlModel.Description!, statisticModel,
                    (key, existingValue) =>
                    {
                        existingValue.TotalTime += retryInterval;
                        return existingValue;
                    });

                return await DownloadFileAsync(urlModel, ct);
            }
            catch (Exception ex)
            {
                stopWatch.Stop();
                var elapsed = stopWatch.Elapsed;

                var logMessage = string.Empty;
                
                StatisticModels.AddOrUpdate(urlModel.Description!, statisticModel,
                    (key, existingValue) =>
                    {
                        existingValue.TotalTime += elapsed;
                        existingValue.Tries += 1;
                        existingValue.Description = $"FAIL - {ex.Message}.";
                        existingValue.Statistic.TryAdd(
                            existingValue.Tries, (existingValue.Description, existingValue.TotalTime));
                        
                        logMessage = $@"Try: {existingValue.Tries} Name: {key} (Description: {existingValue.Description}, TotalTime: {existingValue.TotalTime:hh\:mm\:ss})";
                        
                        return existingValue;
                    });

                await Logger.ConsoleLog(logMessage);
                
                return 0;
            }
        }

        private async Task SaveFile(HttpResponseMessage response, string fileName)
        { 
            var downloadsFolder = Path.Combine(Directory.GetCurrentDirectory(), downloadsPath);
            
            if (downloadsPath.Contains(Path.DirectorySeparatorChar))
            {
                if(downloadsPath.EndsWith(Path.DirectorySeparatorChar))
                {
                    downloadsFolder = this.downloadsPath;
                }else
                {
                    downloadsFolder = this.downloadsPath + Path.DirectorySeparatorChar;
                }
            }

            if (!Directory.Exists(downloadsFolder))
            {
                Directory.CreateDirectory(downloadsFolder);
            }


            var destonationPath = Path.Combine(downloadsFolder, $"{fileName}_.zip");

            await using var fs = new FileStream(destonationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fs);
        }
        public async Task SaveLogs()
        {
            foreach (var stat in StatisticModels.Values)
            {
                await Logger.Log(stat.ToString());
            }
        }
    }
}
