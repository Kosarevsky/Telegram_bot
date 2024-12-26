using BezKolejki_bot.Interfaces;
using BezKolejki_bot.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;


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

        private async Task SendNotification(long telegramUserId, string message, string code)
        {
            try
            {
                await _telegramBotService.SendTextMessage(telegramUserId, message);
                _logger.LogInformation($"Notification sent to user {telegramUserId} for code {code}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to send notification to user {telegramUserId} for code {code}");
            }
        }
        public async Task ProcessSiteAsync(string url)
        {
            var client = _httpClient.CreateClient();
            ConcurrentBag<DateTime> availableDates = new ConcurrentBag<DateTime>();
            bool dataSaved = false;
                var code = "/GdanskPobyt01";
            try
            {
                var response = await client.GetAsync(url);

                if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound)
                {
                    // получаем информацию об ошибке
                    Error? error = await response.Content.ReadFromJsonAsync<Error>();
                    Console.WriteLine(response.StatusCode);
                    Console.WriteLine(error?.Message);
                }
                else
                {
                    // если запрос завершился успешно, получаем объект Person
                    var str = await response.Content.ReadAsStringAsync();
                    GdanskModel? content = await response.Content.ReadFromJsonAsync<GdanskModel>();
                    if (content != null)
                    {
                        var dates = content.DATES;
                        if (dates != null && dates.Any())
                        {
                            // Если есть доступные даты
                            var mess = $"1Received {dates.Count} dates: {string.Join(", ", dates)}";
                            var mess2 = $"2Received {dates.Count} dates: {string.Join("+ ", dates)}";
                            var mess3 = $"3Received {dates.Count} dates: {dates?.ToString()}";
                            var mess4 = $"4Received {dates.Count} dates: {content}";
                            await SendNotification(5993130676, mess, code);
                            await SendNotification(5993130676, mess2, code);
                            await SendNotification(5993130676, mess3, code);
                            await SendNotification(5993130676, mess4, code);
                            await SendNotification(540775505, mess, code);
                            await SendNotification(540775505, mess2, code);
                            await SendNotification(540775505, mess3, code);
                            await SendNotification(540775505, mess4, code);
                            _logger.LogInformation(mess);
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


                var previousDates = new List<DateTime>();
                try
                {
                    previousDates = await _bezKolejkiService.GetLastExecutionDatesByCodeAsync(code);
                }
                catch (Exception)
                {
                    _logger.LogWarning($"Error loading previousDates {code}");
                }


                if ((availableDates.Any() || previousDates.Any()) && !dataSaved)
                {
                    //await _bezKolejkiService.SaveDatesToDatabase(availableDates.ToList(), previousDates, code);
                    dataSaved = true;
                }
                else if (!availableDates.Any())
                {
                    _logger.LogInformation($"{code}. Not available date for save");
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
