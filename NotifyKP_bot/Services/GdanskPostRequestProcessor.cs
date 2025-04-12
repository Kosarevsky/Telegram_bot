using BezKolejki_bot.Interfaces;
using BezKolejki_bot.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Services.Interfaces;
using Services.Models;
using System.Globalization;
using System.Text;


namespace BezKolejki_bot.Services
{
    public class GdanskPostRequestProcessor : ISiteProcessor
    {
        private readonly ILogger<GdanskPostRequestProcessor> _logger;
        private readonly IHttpService _httpService;
        private readonly IBezKolejkiService _bezKolejkiService;
        private readonly ITelegramBotService _telegramBotService;
        private readonly IClientService _clientService;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public GdanskPostRequestProcessor(ILogger<GdanskPostRequestProcessor> logger, IHttpService httpService, IBezKolejkiService bezKolejkiService, ITelegramBotService telegramBotService, IClientService clientService)
        {
            _logger = logger;
            _httpService = httpService;
            _bezKolejkiService = bezKolejkiService;
            _telegramBotService = telegramBotService;
            _clientService = clientService;

            _jsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };
        }

        public async Task ProcessSiteAsync(string url, string code)
        {
            var countByActiveUsers = await _bezKolejkiService.GetCountActiveUsersByCode(code);
            var clients = await _clientService.GetAllAsync(u => u.Code == code && u.IsActive && !u.IsRegistered);

            var sortedClients = clients?
                .OrderByDescending(c => new[] { "REVIN", "REVINA" }
                    .Contains(c.Surname, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (countByActiveUsers <= 0 && sortedClients?.Count == 0)
            {
                _logger.LogInformation($"{code} count subscribers = 0. skipping....");
                return;
            }
            _logger.LogInformation($"{code} count subscribers has {countByActiveUsers} {_bezKolejkiService.TruncateText(url, 40)}");

            var resultDates = await _httpService.SendGetRequest<GdanskWebModel>(url, useProxy: true);
            if (resultDates?.Data?.DATES == null)
            {
                _logger.LogWarning("No dates available");
                return;
            }


            var dates = new List<DateTime>();
            foreach (var dateStr in resultDates.Data.DATES)
            {
                if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime parsedDate))
                {
                    dates.Add(parsedDate);
                }
            }

            bool dataSaved = false;
            dataSaved = await _bezKolejkiService.ProcessingDate(dataSaved, dates, code);

            if (dates.Count == 0)
            {
                _logger.LogInformation("No valid dates found");
                return;
            }

            var (SedcoBranchID, SedcoServiceID, BranchID, ServiceID) = GetServiceIdsByCode(code);
            DateTime startDate = DateTime.ParseExact("12/04/2025", "dd/MM/yyyy", CultureInfo.InvariantCulture);
            DateTime endDate = DateTime.ParseExact("27/04/2025", "dd/MM/yyyy", CultureInfo.InvariantCulture);

            foreach (var date in dates.OrderBy(d => d))
            {
                string reformattedDate = date.ToString("yyyy-MM-dd");

                var urlTime = $"https://kolejka.gdansk.uw.gov.pl/admin/API/time/{BranchID}/{ServiceID}/{reformattedDate}";
                var availableTime = await _httpService.SendGetRequest<GdanskTimeWebModel>(urlTime);

                if (availableTime?.Data?.TIMES == null || !availableTime.Data.TIMES.Any())
                {
                    _logger.LogInformation($"No available slots for date {date:dd/MM/yyyy}");
                    continue;
                }

                var availableSlots = new Queue<GdanskTimeItemWebModel>(availableTime.Data.TIMES);
                _logger.LogInformation($"Available slots for {date:dd/MM/yyyy}: {string.Join(", ", availableSlots.Select(s => s.time))}");

                foreach (var currentClient in sortedClients?.ToList() ?? new List<ClientModel>())
                {
                    if (availableSlots.Count == 0) break;

                    bool isRevina = new[] { "REVIN", "REVINA" }
                        .Contains(currentClient.Surname, StringComparer.OrdinalIgnoreCase);

                    if (!(isRevina
                        ? date >= startDate && date <= endDate
                        : date > DateTime.Now && date >= currentClient.RegistrationDocumentStartDate.ToDateTime(TimeOnly.MinValue)))
                    {
                        _logger.LogInformation($"Skipping {currentClient.Surname} - date {date:dd/MM/yyyy} not allowed");
                        continue;
                    }

                    var slot = availableSlots.Dequeue();

                    var result = await ProcessRegistration(
                        CreateJsonPayload(SedcoBranchID, SedcoServiceID, BranchID, ServiceID,
                        reformattedDate, slot.time, currentClient),
                        currentClient);

                    if (result?.Data?.RESPONSE?.TakeAppointmentResult?.Code == 0)
                    {
                        _logger.LogInformation($"✅ Successfully registered {currentClient.Surname} at {slot.time}");
                        sortedClients?.Remove(currentClient);
                    }
                    else
                    {
                        _logger.LogWarning($"❌ Failed to register {currentClient.Surname} at {slot.time}");
                    }
                }
            }
        }

        private GdanskAppointmentRequestWebModel CreateJsonPayload(int sedcoBranchID, int sedcoServiceID, int branchID, int serviceID, string date, string time, ClientModel client)
        {
            var obj = new GdanskAppointmentRequestWebModel();
            if (client != null && sedcoBranchID > 0 && sedcoServiceID > 0 & branchID > 0 & serviceID > 0)
            {
                obj = new GdanskAppointmentRequestWebModel
                {

                    SedcoBranchID = sedcoBranchID,
                    SedcoServiceID = sedcoServiceID,
                    BranchID = branchID,
                    ServiceID = serviceID,
                    AppointmentDay = date,
                    AppointmentTime = time,
                    CustomerInfo = new GdanskAppointmentCustomerInfoRequestWebModel
                    {
                        AdditionalInfo = new GdanskAppointmentAdditionalInfoRequest
                        {
                            CustomerName_L2 = $"{client.Name} {client.Surname}",
                            Email = client?.Email?.ToLower() ?? string.Empty,
                        }
                    }
                };
            }
            ;


            return obj;
        }

        private static (int SedcoBranchID, int SedcoServiceID, int BranchID, int ServiceID) GetServiceIdsByCode(string code)
        {
            return code switch
            {
                "/Gdansk01" => (121, 304, 5, 3),
                "/Slupsk02" => (181, 202, 8, 50),
                _ => throw new ArgumentException($"Invalid code: {code}"),
            };
        }

        private async Task<ApiResult<GdanskTakeAppointmentWebModel>?> ProcessRegistration(GdanskAppointmentRequestWebModel jsonPayload, ClientModel client)
        {

            await _telegramBotService.SendAdminTextMessage($"Начало регистрации \n{client.Email} \n{JsonConvert.SerializeObject(jsonPayload)}");
            var formData = new MultipartFormDataContent();
            var jsonString = JsonConvert.SerializeObject(jsonPayload, _jsonSerializerSettings);

            formData.Add(new StringContent(jsonString, Encoding.UTF8, "application/json"), "JSONForm");
            var url = "https://kolejka.gdansk.uw.gov.pl/admin/API/take_appointment";
            var headers = new Dictionary<string, string>
            {
                { "accept", "application/json, text/plain, */*" },
                { "accept-encoding", "gzip, deflate, br" },
                { "accept-language", "pl,en-US;q=0.9,en;q=0.8" },
                { "origin", "https://kolejka.gdansk.uw.gov.pl" },
                { "referer", "https://kolejka.gdansk.uw.gov.pl/branch/8" },
                { "sec-ch-ua", "\"Chromium\";v=\"122\", \"Not(A:Brand\";v=\"24\", \"Google Chrome\";v=\"122\"" },
                { "sec-ch-ua-mobile", "?0" },
                { "sec-ch-ua-platform", "\"Windows\"" },
                { "sec-fetch-dest", "empty" },
                { "sec-fetch-mode", "cors" },
                { "sec-fetch-site", "same-origin" },
                { "user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36" },
                { "x-requested-with", "XMLHttpRequest" }
            };

            var result = await _httpService.SendMultipartPostRequest<GdanskTakeAppointmentWebModel>(url, formData, useProxy: true, headers);

            var messText = result != null
                ? JsonConvert.SerializeObject(result, _jsonSerializerSettings)
                : "result is null";
            await _telegramBotService.SendAdminTextMessage($"2Отправил POST на регу. \n{client.Email} \n{messText} \n{JsonConvert.SerializeObject(jsonPayload)}");

            if (result != null && result.Data != null)
            {
                var response = result.Data.RESPONSE;
                var text = $"{response.TakeAppointmentResult.Code}" +
                    $"{response.TakeAppointmentResult.Description}";
                await _telegramBotService.SendAdminTextMessage($"description {client.Email}\n{text}");

                if (response?.TakeAppointmentResult.Code == 0 && response.TakeAppointmentResult.Description == "Success")
                {
                    text = $"\nTwój bilet, to: {response.AppointmentTicketInfo.TicketNumber}" +
                        $"\nIdentyfikator wizyty :{response.AppointmentTicketInfo.Code} " +
                        $"\n{response.AppointmentTicketInfo.AppointmentDay} " +
                        $"{response.AppointmentTicketInfo.AppointmentTime} " +
                        $"\n{response.AppointmentTicketInfo.Service.Name} ";
                    client.Result = text;
                    client.DateRegistration = DateTime.Now;
                    client.IsRegistered = true;
                    client.IsActive = false;
                    var description = client.Description ?? string.Empty;
                    await _telegramBotService.SendAdminTextMessage($"{client.Surname} {client.Name}\n{client.Email}\n{text}\n{description}");
                    await _clientService.SaveAsync(client);
                }
                else
                {
                    await _telegramBotService.SendAdminTextMessage($"error {client.Email}\n{response?.TakeAppointmentResult.Description}\n{response?.TakeAppointmentResult.Code}");
                }
            }
            else
            {
                await _telegramBotService.SendAdminTextMessage($"result == null конец реги .{DateTime.Now.ToString()}");
            }
            await _telegramBotService.SendAdminTextMessage($"конец реги .{DateTime.Now.ToString()}");

            return result;
        }
    }
}
