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

            if (countByActiveUsers <= 0 && clients?.Count == 0)
            {
                _logger.LogInformation($"{code} count subscribers = 0. skipping....");
                return;
            }
            _logger.LogInformation($"{code} count subscribers has {countByActiveUsers} {_bezKolejkiService.TruncateText(url, 40)}");

            var resultDates = await _httpService.SendGetRequest<GdanskWebModel>(url, useProxy: false);
            if (resultDates != null && resultDates.Data?.DATES != null)
            {
                var dates = new List<DateTime>();

                foreach (var date in resultDates.Data.DATES)
                {
                    if (DateTime.TryParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                    {
                        dates.Add(parsedDate);
                    }
                    else
                    {
                        _logger.LogWarning($"{code}. error parsing date {date}");
                    }
                }


                if (dates != null)
                {
                    bool dataSaved = false;
                    dataSaved = await _bezKolejkiService.ProcessingDate(dataSaved, dates, code);
                    if (dates.Count > 0)
                    {
                        if (code == "/Gdansk01")
                            await _telegramBotService.SendAdminTextMessage($"есть дата.{DateTime.Now.ToString()}");

                        if (clients?.Count > 0)
                        {
                            var (SedcoBranchID, SedcoServiceID, BranchID, ServiceID) = GetServiceIdsByCode(code);
                            var clientIndex = 0;
                            //DateTime.TryParseExact("13/06/2025", "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date);
                            foreach (var date in dates)
                            {
                                string reformattedDate = date.ToString("yyyy-MM-dd");

                                if (clientIndex >= clients?.Count)
                                {
                                    _logger.LogInformation("No more clients left, skipping remaining dates.");
                                    break;
                                }

                                var urlTime = $"https://kolejka.gdansk.uw.gov.pl/admin/API/time/{BranchID}/{ServiceID}/{reformattedDate}";
                                var availableTime = await _httpService.SendGetRequest<GdanskTimeWebModel>(urlTime);

                                if (availableTime != null && availableTime.Data != null)
                                {
                                    var timeSlots = string.Join(", ", availableTime.Data.TIMES.Select(x => x.time));
                                    var message = $"Available time slots for {reformattedDate}: {timeSlots}";
                                    await _telegramBotService.SendAdminTextMessage(message);

                                    foreach (var time in availableTime.Data.TIMES)
                                    {
                                        if (clientIndex < clients?.Count)
                                        {
                                            var client = clients[clientIndex];

                                            DateTime ignoreDate = DateTime.ParseExact("04/04/2025", "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                            DateTime startDate = DateTime.ParseExact("12/04/2025", "dd/MM/yyyy", CultureInfo.InvariantCulture);
                                            DateTime endDate = DateTime.ParseExact("27/04/2025", "dd/MM/yyyy", CultureInfo.InvariantCulture);

                                            bool isRevina = string.Equals(client.Surname, "REVIN", StringComparison.OrdinalIgnoreCase) ||
                                                           string.Equals(client.Surname, "REVINA", StringComparison.OrdinalIgnoreCase);

                                            bool isAllowedForRevina = isRevina && (date >= startDate && date <= endDate); // REVIN/REVINA только 12–27 апреля
                                            bool isAllowedForOthers = !isRevina && date > DateTime.Now && date >= client.RegistrationDocumentStartDate.ToDateTime(TimeOnly.MinValue); // Остальные — стандартные условия

                                            if (isAllowedForRevina || isAllowedForOthers)
                                            {
                                                var jsonPayload = CreateJsonPayload(SedcoBranchID, SedcoServiceID, BranchID, ServiceID, reformattedDate, time.time, client);
                                                var result = await ProcessRegistration(jsonPayload, client);


                                                if (result?.Data?.RESPONSE?.TakeAppointmentResult?.Code == 0)
                                                {
                                                    _logger.LogInformation($"Registration successful for {client.Surname} at {time.time}.");
                                                    clientIndex++;
                                                }
                                                else
                                                {
                                                    _logger.LogWarning($"Registration failed for {client.Surname} at {time.time}. Trying next slot.");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _logger.LogInformation("No more clients to assign for available slots.");
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogInformation($"No available dates. Message: {resultDates.Data.MSG}");
                }
            }
            else
            {
                _logger.LogWarning("The response content could not be deserialized into GdanskModel.");
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
                            Email = client?.Email?.ToLower(),
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

        private async Task<ApiResult<GdanskTakeAppointmentWebModel>> ProcessRegistration(GdanskAppointmentRequestWebModel jsonPayload, ClientModel client)
        {

            await _telegramBotService.SendAdminTextMessage($"Начало регистрации \n{client.Email} \n{JsonConvert.SerializeObject(jsonPayload)}");
            var formData = new MultipartFormDataContent();
            var jsonString = JsonConvert.SerializeObject(jsonPayload, _jsonSerializerSettings);

            formData.Add(new StringContent(jsonString, Encoding.UTF8, "application/json"), "JSONForm");
            var url = "https://kolejka.gdansk.uw.gov.pl/admin/API/take_appointment";

            var result = await _httpService.SendMultipartPostRequest<GdanskTakeAppointmentWebModel>(url, formData, useProxy: true);

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
