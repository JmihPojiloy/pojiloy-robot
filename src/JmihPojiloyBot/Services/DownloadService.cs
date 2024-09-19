using JmihPojiloyBot.Loggers;
using JmihPojiloyBot.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace JmihPojiloyBot.Services
{
    public class DownloadService
    {
        private readonly HttpClient _httpClient;
        private readonly TimeSpan _retryInterval;
        private readonly string _downloadsPath;

        public ConcurrentDictionary<string,StatisticModel> statisticModels = new ConcurrentDictionary<string, StatisticModel>();

        public DownloadService(HttpClient httpClient, TimeSpan retryInterval, string downloadsPath)
        {
            _httpClient = httpClient;
            _downloadsPath = downloadsPath;
            _retryInterval = retryInterval;
        }

        public async Task<int> DownloadFileAsync(UrlModel urlModel, CancellationToken ct)
        {

            var statisticModel = statisticModels.GetOrAdd(urlModel.description!, new StatisticModel(urlModel.description!));

            if(statisticModel == null)
            {
                return 0;
            }

            var stopWatch = Stopwatch.StartNew();

            try
            {
                if(ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();
                }

                if(urlModel.url ==  null)
                {
                    throw new ArgumentNullException(nameof(urlModel));
                }

                var response = await _httpClient.GetAsync(urlModel.url);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    await SaveFile(response, urlModel.description!);

                    stopWatch.Stop();

                    statisticModels.AddOrUpdate(urlModel.description!, statisticModel,
                        (key, existingValue) =>
                        {
                            existingValue.TotalTime += stopWatch.Elapsed;
                            existingValue.Tries += 1;
                            existingValue.Description = "download";
                            existingValue.Statistic.TryAdd(
                                existingValue.Tries, (existingValue.Description, existingValue.TotalTime));
                            return existingValue;
                        });

                    await Logger.Log(statisticModel.ToString());

                    return 1;
                }

                throw new HttpRequestException($"Fetch {urlModel.description} returned status {response.StatusCode}");
            }
            catch(OperationCanceledException)
            {
                stopWatch.Stop();

                statisticModels.AddOrUpdate(urlModel.description!, statisticModel,
                    (key, existingValue) =>
                    {
                        existingValue.TotalTime += stopWatch.Elapsed;
                        existingValue.Tries += 1;
                        existingValue.Description = "TIME IS UP!";
                        existingValue.Statistic.TryAdd(
                            existingValue.Tries, (existingValue.Description, existingValue.TotalTime));
                        return existingValue;
                    });

                await Logger.Log(statisticModel.ToString());

                return 0;
            }
            catch (HttpRequestException ex)
            {
                await Task.Delay(_retryInterval);
                stopWatch.Stop();

                statisticModels.AddOrUpdate(urlModel.description!, statisticModel,
                    (key, existingValue) =>
                    {
                        existingValue.TotalTime += stopWatch.Elapsed;
                        existingValue.Tries += 1;
                        existingValue.Description = $"{ex.Message}. Next try in {_retryInterval.TotalMinutes} minutes.";
                        existingValue.Statistic.TryAdd(
                            existingValue.Tries, (existingValue.Description, existingValue.TotalTime));
                        return existingValue;
                    });

                await Logger.Log(statisticModel.ToString());
                return await DownloadFileAsync(urlModel, ct);
            }
            catch(Exception ex)
            {
                stopWatch.Stop();

                statisticModels.AddOrUpdate(urlModel.description!, statisticModel,
                    (key, existingValue) =>
                    {
                        existingValue.TotalTime += stopWatch.Elapsed;
                        existingValue.Tries += 1;
                        existingValue.Description = $"FAIL - {ex.Message}.";
                        existingValue.Statistic.TryAdd(
                            existingValue.Tries, (existingValue.Description, existingValue.TotalTime));
                        return existingValue;
                    });

                await Logger.Log(statisticModel.ToString());

                return 0;
            }
        }

        private async Task SaveFile(HttpResponseMessage reponse, string fileName)
        {
            string downloadsFolder = Path.Combine(Directory.GetCurrentDirectory(), _downloadsPath);

            if (!Directory.Exists(downloadsFolder))
            {
                Directory.CreateDirectory(downloadsFolder);
            }


            string destonationPath = Path.Combine(downloadsFolder, $"{fileName}_.zip");

            using (var fs = new FileStream(destonationPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await reponse.Content.CopyToAsync(fs);
            }
        } 
    }
}
