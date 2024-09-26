using JmihPojiloyBot.Loggers;
using JmihPojiloyBot.Models;
using System.Text.Json;

namespace JmihPojiloyBot.Services
{
    public class GetUrlsService(HttpClient httpClient, TimeSpan interval)
    {

        public async Task<UrlModel?> GetUrlsAsync(string request, CancellationToken ct)
        {
            try
            {
                var response = await httpClient.GetAsync(request, ct);
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync(ct);

                var urlModel = JsonSerializer.Deserialize<UrlModel>(jsonResponse)!;

                urlModel.Description = request[(request.LastIndexOf('=') + 1)..];

                if (ct.IsCancellationRequested)
                {
                    throw new OperationCanceledException(urlModel.ToString());
                }

                if(urlModel.error != null)
                {
                    throw new HttpRequestException(urlModel.ToString());
                }


                await Logger.Log(urlModel.ToString()); 
                return urlModel!;
            }
            catch (OperationCanceledException ex)
            {
                var urlModelCancel = new UrlModel {Description = ex.Message ,error = new Error { code = 0, description = "TIME IS UP!" } };
                await Logger.Log(ex.Message);
                return urlModelCancel;
            }
            catch (HttpRequestException ex)
            {

                if (ex.Message.Contains("500"))
                {
                    await Logger.Log(ex.Message);
                    await Task.Delay(interval, ct);
                    return await GetUrlsAsync(request, ct);
                }

                await Logger.Log(ex.Message);
                return null;
            }
            catch(Exception ex)
            {
                await Logger.Log(ex.Message);
                return null;
            }
        }
    }
}
