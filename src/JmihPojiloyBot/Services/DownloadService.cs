using JmihPojiloyBot.Loggers;
using JmihPojiloyBot.Models;
using System.Diagnostics;

namespace JmihPojiloyBot.Services
{
    public class DownloadService
    {
        private readonly HttpClient _httpClient;
        private readonly Logger _logger;
        private readonly TimeSpan _retryInterval;

        public DownloadService(HttpClient httpClient, Logger logger, TimeSpan retryInterval)
        {
            _httpClient = httpClient;
            _logger = logger;
            _retryInterval = retryInterval;
        }

        public async Task<DownloadResult> DownloadFileAsync(UrlModel urlModel, CancellationToken ct)
        {
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
                    await SaveFile(response, urlModel.description);

                    _logger.Log(new Log(urlModel.description, response.StatusCode.ToString()));

                    stopWatch.Stop();
                    
                    return new DownloadResult(stopWatch.Elapsed, urlModel.description, "OK");
                }

                throw new HttpRequestException($"Fetch {urlModel.description} returned status {response.StatusCode}");
            }
            catch(OperationCanceledException ex)
            {
                stopWatch.Stop();
                _logger.Log(new Log(ex.Message, "TIME IS UP!"));
                return new DownloadResult(stopWatch.Elapsed, urlModel.description, "TIME IS UP!");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"{ex.Message}. Next try in {_retryInterval.TotalMinutes} minutes.");

                _logger.Log(new Log(ex.Message, $"Next try in {_retryInterval.TotalMinutes} minutes"));

                await Task.Delay(_retryInterval);

                return await DownloadFileAsync(urlModel, ct);
            }
            catch(Exception ex)
            {
                stopWatch.Stop();
                _logger.Log(new Log(ex.Message, "FAIL"));
                return new DownloadResult(stopWatch.Elapsed, urlModel.description, "FAIL");
            }
        }

        private async Task SaveFile(HttpResponseMessage reponse, string fileName)
        {
            string downloadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Downloads");

            if (!Directory.Exists(downloadsFolder))
            {
                Directory.CreateDirectory(downloadsFolder);
            }


            string destonationPath = Path.Combine(downloadsFolder, $"{fileName}.zip");

            using (var fs = new FileStream(destonationPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await reponse.Content.CopyToAsync(fs);
            }
        } 
    }
}
