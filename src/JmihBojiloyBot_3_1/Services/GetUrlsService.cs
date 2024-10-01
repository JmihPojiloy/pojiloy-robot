using System.Text.Json;
using JmihBojiloyBot_3_1.Loggers;
using JmihPojiloyBot_3_1.Models;

namespace JmihPojiloyBot.Services
{
    public class GetUrlsService
    {

        private readonly HttpClient httpClient;
        private readonly TimeSpan interval;

        public GetUrlsService(HttpClient httpClient, TimeSpan interval)
        {
            this.httpClient = httpClient;
            this.interval = interval;
        }

        public async Task<UrlModel?> GetUrlsAsync(string request, CancellationToken ct)
        {
            try
            {
                var response = await httpClient.GetAsync(request, ct);
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var urlModel = JsonSerializer.Deserialize<UrlModel>(jsonResponse)!;

                urlModel.Description = request[(request.LastIndexOf('=') + 1)..];

                if (ct.IsCancellationRequested)
                {
                    ct.ThrowIfCancellationRequested();
                }

                if (urlModel.error != null)
                {
                    throw new HttpRequestException(urlModel.ToString());
                }
                
                await Logger.Log(urlModel.ToString());
                
                return urlModel!;
            }
            catch (TaskCanceledException ex) when (ct.IsCancellationRequested)
            {
                var urlModelCancel = new UrlModel
                    { Description = ex.Message, error = new Error { code = 0, description = "TIME IS UP!" } };
                await Logger.ConsoleLog($"{urlModelCancel.url} {urlModelCancel.url} Time is up!");
                await Logger.ConsoleLog($"{urlModelCancel.url} {ex.Message}");
                return urlModelCancel;
            }
            catch (HttpRequestException ex)
            {

                if (ex.Message.Contains("500"))
                {
                    await Logger.Log(ex.Message);
                    await Logger.ConsoleLog($"Download error 500; {ex.Message} next try at 5 min.");
                    await Task.Delay(interval, ct);
                    return await GetUrlsAsync(request, ct);
                }

                await Logger.Log(ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                await Logger.Log(ex.Message);
                await Logger.ConsoleLog($"Download error: {ex.Message}");
                return null;
            }
        }
    }
}
