using BezKolejki_bot.Interfaces;
using BezKolejki_bot.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Services.Interfaces;
using Services.Models;
using System.Net.Http.Json;
using System.Text;


namespace BezKolejki_bot.Services
{
    public class GdanskQmaticPostRequestProcessor : ISiteProcessor
    {
        private readonly ILogger<GdanskPostRequestProcessor> _logger;
        private readonly IHttpClientFactory _httpClient;
        private readonly IBezKolejkiService _bezKolejkiService;
        private readonly ITelegramBotService _telegramBotService;
        private readonly IClientService _clientService;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private IEnumerable<object> serviceGroups;

        public GdanskQmaticPostRequestProcessor(ILogger<GdanskPostRequestProcessor> logger, IHttpClientFactory httpClientFactory, IBezKolejkiService bezKolejkiService, ITelegramBotService telegramBotService, IClientService clientService)
        {
            _logger = logger;
            _httpClient = httpClientFactory;
            _bezKolejkiService = bezKolejkiService;
            _telegramBotService = telegramBotService;
            _clientService = clientService;

            _jsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };
        }
        record Error(string Message);

        private Task<string?> GetPublicId(List<GdanskQmaticWebModel?> profiles, string searchName)
        {
            var publicId = profiles
                .SelectMany(g => g.serviceGroups)
                .SelectMany(s => s.services)
                .Where(service => service.name == searchName)
                .FirstOrDefault()?.publicId;

            return Task.FromResult(publicId);
        }

        private Task<string?> GetBranchPublicId(List<GdanskQmaticWebModel?> profiles, string searchName)
        {
            var branchPublicId = profiles
                .Where(branch => branch.serviceGroups
                .SelectMany(group => group.services)
                .Any(service => service.name == searchName)) 
                .Select(branch => branch.branchPublicId)
                .FirstOrDefault(); 

            return Task.FromResult(branchPublicId);
        }

        public async Task ProcessSiteAsync(string url, string code)
        {
            string searchName = "Składanie wniosków i dokumentacji do wniosków już złożonych w sprawie obywatelstwa polskiego";
            var countByActiveUsers = await _bezKolejkiService.GetCountActiveUsersByCode(code);
            var publicId = string.Empty;
            var branchPublicId = string.Empty;
            var requestUrl = string.Empty;
            var profiles = await GetProfilesAsync(url, code);
            if (profiles != null)
            {
                publicId = await GetPublicId(profiles, searchName);
                if (!string.IsNullOrEmpty(publicId))
                {
                    branchPublicId = await GetBranchPublicId(profiles, searchName);
                }
            }
            else
            {
                _logger.LogWarning("The response content could not be deserialized into GdanskQmaticWebModel.");
            }

            if (!string.IsNullOrEmpty(publicId) && !string.IsNullOrEmpty(branchPublicId))
            {
                requestUrl = $"https://rezerwacja.gdansk.uw.gov.pl:8445/qmaticwebbooking/rest/schedule/branches/" +
                   branchPublicId +
                   "/dates;servicePublicId=" +
                   publicId +
                   ";customSlotLength=40";
            }
            else
            {
                _logger.LogWarning("The publicId or branchPublicId is empty.");
            }

            if (!string.IsNullOrEmpty(requestUrl))
            {
                var dates = await GetDatesAsync(requestUrl);
                if (dates != null)
                {
                    bool dataSaved = false;
                    List<string> dateStrings = dates.Select(d => d.Date.ToString("yyyy-MM-dd")).ToList();
                    dataSaved = await _bezKolejkiService.ProcessingDate(dataSaved, dateStrings, code);
                }
            }

          ;
        }


        public async Task<GdanskTimeWebModel?> GetTimeAsync(string date, int BranchID, int ServiceID)
        {
            var client = _httpClient.CreateClient();
            var url = $"https://kolejka.gdansk.uw.gov.pl/admin/API/time/{BranchID}/{ServiceID}/{date}";
            var response = await SendGetRequest<GdanskTimeWebModel>(url);
            return response;
        }
        public async Task<List<GdanskQmaticWebModel?>> GetProfilesAsync(string url, string code)
        {
            var client = _httpClient.CreateClient();

            try
            {
                var response = await client.GetAsync(url);
                return await ProcessHttpResponse<List<GdanskQmaticWebModel?>>(response);
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
            return null;
        }

        public async Task<List<GdanskQmaticDateWebModel?>> GetDatesAsync(string url)
        {
            var client = _httpClient.CreateClient();

            try
            {
                var response = await client.GetAsync(url);
                return await ProcessHttpResponse<List<GdanskQmaticDateWebModel?>>(response);
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
            return null;
        }

        private async Task<T?> SendGetRequest<T>(string url) where T : class
        {
            var client = _httpClient.CreateClient();
            try
            {
                var response = await client.GetAsync(url);
                return await ProcessHttpResponse<T>(response);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning($"An error occurred during the second POST request: {ex.Message}");
            }
            return default;
        }


        private async Task<T?> ProcessHttpResponse<T>(HttpResponseMessage response) where T: class
        {
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogWarning("HTTP response is empty.");
                    return null;
                }
                try
                {
                    return JsonConvert.DeserializeObject<T>(content);
                }
                catch (JsonException ex)
                {
                    _logger.LogError($"Failed to deserialize response: {ex.Message}");
                    _logger.LogError($"Response content: {content}");
                }
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                var error1 = JsonConvert.DeserializeObject<T>(content);
                await _telegramBotService.SendAdminTextMessage($" error45 {content}");
                await _telegramBotService.SendAdminTextMessage($" error55 {error1}");

                var error = await response.Content.ReadFromJsonAsync<Error>();
                _logger.LogWarning(message: $"HTTP Error {response.StatusCode}: {error?.Message} {error?.ToString()}");
            }
            return null;
        }
    }
}
