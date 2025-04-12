using BezKolejki_bot.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Server;
using Services.Interfaces;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace BezKolejki_bot.Services
{
    public class OlsztynPostRequestProcessor : ISiteProcessor
    {
        private readonly ILogger<OlsztynPostRequestProcessor> _logger;
        private readonly IHttpService _httpService;
        private readonly IBezKolejkiService _bezKolejkiService;
        private bool _useProxy = true; 

        public OlsztynPostRequestProcessor(ILogger<OlsztynPostRequestProcessor> logger, IHttpService httpService, IBezKolejkiService bezKolejkiService)
        {
            _logger = logger;
            _httpService = httpService;
            _bezKolejkiService = bezKolejkiService;
        }

        public async Task ProcessSiteAsync(string url, string code)
        {
/*            var testProxy = new HttpClient(new HttpClientHandler
            {
                Proxy = new WebProxy("http://185.95.230.237:8000")
                {
                    Credentials = new NetworkCredential("9s8cAt", "55rPLxx")
                },
                UseProxy = true,
                PreAuthenticate = true
            });
            var testResponse = await testProxy.GetAsync("http://2ip.ru");
            var test = testResponse.Content.ReadAsStringAsync();*/


/*            var response2 = await _httpService.SendGetRequest<string>(
                    "http://2ip.ru",
                    useProxy: true,
                    additionalSuccessPredicate: r => r.Content.Headers.ContentType?.MediaType?.Contains("text/html") ?? false
                );*/

            var countByActiveUsers = await _bezKolejkiService.GetCountActiveUsersByCode(code);

            if (countByActiveUsers <= 0)
            {
                _logger.LogInformation($"{code} count subscribers = 0. skipping....");
                return;
            }

            _logger.LogInformation($"{code} count subscribers has {countByActiveUsers} {_bezKolejkiService.TruncateText(url, 40)}");

            ConcurrentBag<DateTime> dates = new ConcurrentBag<DateTime>();
            bool dataSaved = false;
            try
            {
                var response = await _httpService.SendGetRequest<string>(
                    url,
                    useProxy: _useProxy,
                    additionalSuccessPredicate: r => r.Content.Headers.ContentType?.MediaType?.Contains("text/html") ?? false
                );
                if (!response.IsSuccess)
                {
                    _logger.LogError($"Failed to get content from {url}: {response.ErrorMessage}");
                    return;
                }

                string content = response.Data;

                DateOnly minDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
                DateOnly maxDate =minDate.AddDays(90);

                (minDate, maxDate) = ExtractMinMaxDates(content);

                List<DateOnly> disabledDays = await DatesFromSite(content, minDate);
                if (disabledDays == null || !disabledDays.Any())
                {
                    _logger.LogWarning("The `disabledDays` list was not found or is empty.");
                    disabledDays = new List<DateOnly>();
                }


                for (DateOnly date = minDate; date <= maxDate; date = date.AddDays(1))
                {
                    if (!disabledDays.Contains(date) && date.DayOfWeek != DayOfWeek.Sunday && date.DayOfWeek != DayOfWeek.Saturday)
                    {
                        var isAvailableDate = await GetAvailableTimeByDate(date);
                        if (isAvailableDate)
                        {
                            dates.Add(date.ToDateTime(TimeOnly.MinValue));
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

        private async Task<bool> GetAvailableTimeByDate(DateOnly date)
        {
            var formData = new Dictionary<string, string>
            {
                { "godzina", date.ToString("yyyy-MM-dd") }
            };

            var response = await _httpService.SendFormPostRequest<string>(
                "https://olsztyn.uw.gov.pl/wizytakartapolaka/godziny_pokoj_A1.php",
                formData, useProxy: _useProxy
            );

            if (!response.IsSuccess)
            {
                _logger.LogError($"Failed to get available times: {response.ErrorMessage}");
                return false;
            }

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(response.Data);
            var stanowiskoElement = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='stanowiskoA']");
                   
            var availableTimes = new List<string>();

            if (stanowiskoElement != null)
            {
                var inputs = stanowiskoElement.SelectNodes(".//input[@type='radio']");
                if (inputs != null && inputs.Any())
                {

                    foreach (var input in inputs)
                    {
                        var time = input.GetAttributeValue("value", string.Empty);
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

        public static (DateOnly minDate, DateOnly maxDate) ExtractMinMaxDates(string html)
        {
            var minRegex = new Regex(@"minDate:\s*new Date\(""(?<min>\d{4}/\d{2}/\d{2})""\)", RegexOptions.IgnoreCase);
            var maxRegex = new Regex(@"maxDate:\s*new Date\(""(?<max>\d{4}/\d{2}/\d{2})""\)", RegexOptions.IgnoreCase);

            DateOnly minDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1));
            DateOnly maxDate = minDate.AddDays(90);

            var minMatch = minRegex.Match(html);
            if (minMatch.Success)
            {
                var minDateStr = minMatch.Groups["min"].Value;
                if (DateOnly.TryParseExact(minDateStr, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedMin))
                {
                    minDate = parsedMin;
                }
            }

            var maxMatch = maxRegex.Match(html);
            if (maxMatch.Success)
            {
                var maxDateStr = maxMatch.Groups["max"].Value;
                if (DateOnly.TryParseExact(maxDateStr, "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedMax))
                {
                    maxDate = parsedMax;
                }
            }

            return (minDate, maxDate);
        }

    }
}
