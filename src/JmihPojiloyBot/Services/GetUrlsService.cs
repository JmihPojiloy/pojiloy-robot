using JmihPojiloyBot.Loggers;
using JmihPojiloyBot.Models;
using System.Text.Json;

namespace JmihPojiloyBot.Services
{
    public class GetUrlsService
    {
        private readonly HttpClient _httpClient;
        private readonly Logger _logger;

        public GetUrlsService(HttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<UrlModel?> GetUrlsAsync(string request, CancellationToken ct)
        {
            try
            {
                if (ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();
                }

                var response = await _httpClient.GetAsync(request);
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var urlModel = JsonSerializer.Deserialize<UrlModel>(jsonResponse)!;

                if(urlModel.error != null)
                {
                    throw new HttpRequestException($"Code: {urlModel.error.code} Description: {urlModel.error.description}");
                }

                urlModel.description = request.Substring(request.LastIndexOf('=') + 1);

                _logger.Log(new Log(urlModel.description, response.StatusCode.ToString()));

                return urlModel!;
            }
            catch (OperationCanceledException ex)
            {
                _logger.Log(new Log(ex.Message, "TIME IS UP!"));
                return new UrlModel { error = new Error { code = 0, description = "TIME IS UP!" } };
            }
            catch (HttpRequestException ex)
            {
                var log = new Log(request, ex.Message);

                if (ex.Message.Contains("500"))
                {
                    _logger.Log(log);

                    await Task.Delay(10000);
                    return await GetUrlsAsync(request, ct);
                }

                _logger.Log(log);

                return null;
            }
            catch(Exception ex)
            {
                var log = new Log(request, $"error {ex.Message}");
                _logger.Log(log);

                return null;
            }
        }
    }
}
