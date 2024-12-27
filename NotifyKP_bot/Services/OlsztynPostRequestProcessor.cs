using BezKolejki_bot.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace BezKolejki_bot.Services
{
    public class OlsztynPostRequestProcessor : ISiteProcessor
    {
        private readonly ILogger<OlsztynPostRequestProcessor> _logger;
        private readonly IHttpClientFactory _httpClient;
        private readonly IBezKolejkiService _bezKolejkiService;

        public OlsztynPostRequestProcessor(ILogger<OlsztynPostRequestProcessor> logger, IHttpClientFactory httpClientFactory, IBezKolejkiService bezKolejkiService)
        {
            _logger = logger;
            _httpClient = httpClientFactory;
            _bezKolejkiService = bezKolejkiService;
        }

        public async Task ProcessSiteAsync(string url, string code)
        {
            var client = _httpClient.CreateClient();
            ConcurrentBag<string> dates = new ConcurrentBag<string>();
            bool dataSaved = false;
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();

                DateOnly minDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
                DateOnly maxDate =minDate.AddDays(90);

                List<DateOnly> disabledDays = await DatesFromSite(content, minDate);
                if (disabledDays == null || !disabledDays.Any())
                {
                    _logger.LogWarning("The `disabledDays` list was not found or is empty.");
                    return;
                }

                for (DateOnly date = minDate; date <= maxDate; date = date.AddDays(1))
                {
                    if (!disabledDays.Contains(date))
                    {
                        var isAvailableDate = await GetAvailableTimeByDate(date, client);
                        if (isAvailableDate)
                        {
                            dates.Add(date.ToString());
                        }
                        await Task.Delay(500);
                    }
                }

                dataSaved = await _bezKolejkiService.ProcessingDate(dataSaved, dates.ToList(), code);
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

        private async Task<bool> GetAvailableTimeByDate(DateOnly date, HttpClient client)
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://olsztyn.uw.gov.pl/wizytakartapolaka/godziny_pokoj_A1.php")
            {
                Content = new StringContent($"godzina={date:yyyy-MM-dd}", Encoding.UTF8, "application/x-www-form-urlencoded")
            };
            HttpResponseMessage response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string responseText = await response.Content.ReadAsStringAsync();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(responseText);
            var stanowiskoElement = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='stanowiskoA']");
                   
            var availableTimes = new List<string>();

            if (stanowiskoElement != null)
            {
                var inputs = stanowiskoElement.SelectNodes(".//input[@type='radio']");
                if (inputs != null && inputs.Any())
                {

                    foreach (var input in inputs)
                    {
                        var time = input.GetAttributeValue("value", null);
                        if (!string.IsNullOrEmpty(time))
                        {
                            var formattedTime = Regex.Match(time, @"\d{2}:\d{2}")?.Value; // Extract time (e.g., "11:00")
                            if (!string.IsNullOrEmpty(formattedTime))
                            {
                                availableTimes.Add(formattedTime);
                            }
                        }
                    }

                    if (availableTimes.Any())
                    {
                        _logger.LogInformation($"Available times for {date:yyyy-MM-dd}: {string.Join(", ", availableTimes)}");
                        return true;
                    }
                }
                else
                {
                    _logger.LogInformation($"No available times for {date:yyyy-MM-dd}");
                }
            }
            else
            {
                _logger.LogInformation("The element with class 'stanowiskoA' was not found.");
            }
            return false;
        }

        private Task<List<DateOnly>> DatesFromSite(string content, DateOnly minDate)
        {
            List<DateOnly> dateList = new List<DateOnly>();
            string pattern = @"var\s+disabledDays\s*=\s*\[\s*([\s\S]*?)\s*\];";
            var match = Regex.Match(content, pattern, RegexOptions.Singleline);
            if (match.Success)
            {
                string datesString = match.Groups[1].Value;

                foreach (var dateString in datesString.Split(','))
                {
                    var date = dateString.Trim().Trim('"');

                    if (DateOnly.TryParse(date, out DateOnly dateFromSite) && dateFromSite >= minDate /*&& dateFromSite <= maxDate*/)
                    {
                        dateList.Add(dateFromSite);
                    }
                }
            }
            else
            {
                _logger.LogInformation("The 'disabledDays' array was not found in the response.");
            }

            return Task.FromResult(dateList);
        }
    }
}
