using BezKolejki_bot.Interfaces;
using Microsoft.Extensions.Logging;


namespace BezKolejki_bot.Services
{
    public class PostRequestProcessor : ISiteProcessor
    {
        private readonly ILogger<PostRequestProcessor> _logger;
        private readonly IHttpClientFactory _httpClient;

        public PostRequestProcessor(ILogger<PostRequestProcessor> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory;
        }
        public bool CanHandle(string url) => url.Contains("https://olsztyn.uw.gov.pl/wizytakartapolaka/"); 

        public async Task ProcessSiteAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
/*                var requestPayload = new { *//* добавьте необходимые параметры *//* };
                var json = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation($"Response from {url}: {responseContent}");

                // Логика обработки ответа
                var data = JsonSerializer.Deserialize<YourResponseModel>(responseContent);
                if (data != null)
                {
                    await SaveDatesToDatabase(data.AvailableDates, *//* другие данные *//*);
                }*/
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing POST request to {url}: {ex.Message}");
                throw;
            }
        }

/*        private async Task SaveDatesToDatabase(IEnumerable<DateTime> dates, *//* другие параметры *//*)
        {
            // Логика сохранения дат в базу данных
        }*/
    }

}
