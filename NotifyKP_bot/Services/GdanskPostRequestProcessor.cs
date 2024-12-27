using BezKolejki_bot.Interfaces;
using BezKolejki_bot.Models;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using System.Net;
using System.Net.Http.Json;


namespace BezKolejki_bot.Services
{
    public class GdanskPostRequestProcessor : ISiteProcessor
    {
        private readonly ILogger<GdanskPostRequestProcessor> _logger;
        private readonly IHttpClientFactory _httpClient;
        private readonly IBezKolejkiService _bezKolejkiService;
        private readonly ITelegramBotService _telegramBotService;

        public GdanskPostRequestProcessor(ILogger<GdanskPostRequestProcessor> logger, IHttpClientFactory httpClientFactory, IBezKolejkiService bezKolejkiService, ITelegramBotService telegramBotService)
        {
            _logger = logger;
            _httpClient = httpClientFactory;
            _bezKolejkiService = bezKolejkiService;
            _telegramBotService = telegramBotService;
        }
        record Error(string Message);

        public async Task ProcessSiteAsync(string url, string code)
        {
            var client = _httpClient.CreateClient();
            bool dataSaved = false;

            try
            {
                var response = await client.GetAsync(url);

                if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound)
                {
                    Error? error = await response.Content.ReadFromJsonAsync<Error>();
                    Console.WriteLine(response.StatusCode);
                    Console.WriteLine(error?.Message);
                }
                else
                {
                    var str = await response.Content.ReadAsStringAsync();
                    GdanskWebModel? content = await response.Content.ReadFromJsonAsync<GdanskWebModel>();
                    if (content != null)
                    {
                        var dates = content.DATES;
                        if (dates != null && dates.Any())
                        {
                           dataSaved = await _bezKolejkiService.ProcessingDate(dataSaved, dates.ToList(), code);
                        }
                        else
                        {
                            _logger.LogInformation($"No available dates. Message: {content.MSG}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("The response content could not be deserialized into GdanskModel.");
                    }
                }

            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError($"HTTP error occurred while processing URL {url}: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while processing POST request to {url}: {ex.Message}");
                throw;
            }
        }
    }
}
