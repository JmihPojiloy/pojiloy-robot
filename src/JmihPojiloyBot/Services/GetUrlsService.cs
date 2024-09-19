using JmihPojiloyBot.Loggers;
using JmihPojiloyBot.Models;
using System.Text.Json;

namespace JmihPojiloyBot.Services
{
    public class GetUrlsService
    {
        private readonly HttpClient _httpClient;

        public GetUrlsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<UrlModel?> GetUrlsAsync(string request, CancellationToken ct)
        {
            try
            {
                var response = await _httpClient.GetAsync(request);
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync();

                var urlModel = JsonSerializer.Deserialize<UrlModel>(jsonResponse)!;

                urlModel.description = request.Substring(request.LastIndexOf('=') + 1);

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
                
                var urlModelCancel = new UrlModel {description = ex.Message ,error = new Error { code = 0, description = "TIME IS UP!" } };
                await Logger.Log(ex.Message);
                return urlModelCancel;
            }
            catch (HttpRequestException ex)
            {

                if (ex.Message.Contains("500"))
                {
                    await Logger.Log(ex.Message);
                    await Task.Delay(10000);
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
