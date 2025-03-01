using BezKolejki_bot.Interfaces;
using BezKolejki_bot.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Services.Interfaces;
using Services.Models;
using System.Net.Http.Json;
using System.Text;


namespace BezKolejki_bot.Services
{
    public class GdanskPostRequestProcessor : ISiteProcessor
    {
        private readonly ILogger<GdanskPostRequestProcessor> _logger;
        private readonly IHttpClientFactory _httpClient;
        private readonly IBezKolejkiService _bezKolejkiService;
        private readonly ITelegramBotService _telegramBotService;
        private readonly IClientService _clientService;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly long _adminTlgId;

        public GdanskPostRequestProcessor(ILogger<GdanskPostRequestProcessor> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory, IBezKolejkiService bezKolejkiService, ITelegramBotService telegramBotService, IClientService clientService)
        {
            _logger = logger;
            _httpClient = httpClientFactory;
            _bezKolejkiService = bezKolejkiService;
            _telegramBotService = telegramBotService;
            _clientService = clientService;
            _ = long.TryParse(configuration["Telegram:AdminTelegramId"], out long _adminTlgId);
            _jsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };
        }
        record Error(string Message);

        public async Task ProcessSiteAsync(string url, string code)
        {

            var countByActiveUsers = await _bezKolejkiService.GetCountActiveUsersByCode(code);

            if (countByActiveUsers <= 0)
            {
                _logger.LogInformation($"{code} count subscribers = 0. skipping....");
                return;
            }
            _logger.LogInformation($"{code} count subscribers has {countByActiveUsers} {_bezKolejkiService.TruncateText(url, 40)}");


            var resultDates = await GetDatesAsync(url, code);
            if (resultDates != null)
            {
                var dates = resultDates.DATES;
                if (dates != null)
                {
                    bool dataSaved = false;
                    dataSaved = await _bezKolejkiService.ProcessingDate(dataSaved, dates.ToList(), code);
                    if (dates.Count > 0) { 
                        if (code == "/Gdansk01")
                    await _telegramBotService.SendTextMessage(_adminTlgId, $"есть дата.{DateTime.Now.ToString()}");


                    var clients = await _clientService.GetAllAsync(u => u.Code == code && u.IsActive && !u.IsRegistered);

                        if (clients != null && clients.Count > 0)
                        {
                            var (SedcoBranchID, SedcoServiceID, BranchID, ServiceID) = GetServiceIdsByCode(code);
                            var clientIndex = 0;
                            foreach (var date in dates)
                            //var date = "17/04/2025";
                            {
                                var parsedDate = DateTime.ParseExact(date, "dd/MM/yyyy", null);
                                string reformattedDate = parsedDate.ToString("yyyy-MM-dd");
                                var availableTime = await GetTimeAsync(reformattedDate, BranchID, ServiceID);
                                if (availableTime != null)
                                {
                                    foreach (var time in availableTime.TIMES)
                                    {
                                        if (clientIndex < clients.Count)
                                        {
                                            var client = clients[clientIndex];
                                            var jsonPayload = CreateJsonPayload(SedcoBranchID, SedcoServiceID, BranchID, ServiceID, reformattedDate, time.time, client);
                                            var result = await ProcessRegistration(jsonPayload, client);
                                           
/*                                          var res = DateTime.Compare(parsedDate, new DateTime(2025, 03, 01));
                                            if (res > 0) { 

                                            }*/

                                            clientIndex++;
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
                    _logger.LogInformation($"No available dates. Message: {resultDates.MSG}");
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
            if (client != null && sedcoBranchID > 0 && sedcoServiceID > 0 & branchID > 0 & serviceID > 0 )
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
                _telegramBotService.SendTextMessage(_adminTlgId, $"создал POST на регу. \n{client?.Email} \n{JsonConvert.SerializeObject(obj)}");
            };


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

        private async Task<GdanskTakeAppointmentWebModel?> ProcessRegistration(GdanskAppointmentRequestWebModel jsonPayload, ClientModel client)
        {
            var result = await SendPostRequest<GdanskTakeAppointmentWebModel>(jsonPayload);

            var messText = result != null
                ? JsonConvert.SerializeObject(result, _jsonSerializerSettings)
                : "result is null";
            await _telegramBotService.SendTextMessage(_adminTlgId, $"2Отправил POST на регу. \n{client.Email} \n{messText} \n{JsonConvert.SerializeObject(jsonPayload)}");

            if (result != null)
            {
                if (result?.RESPONSE == null)
                {
                    await _telegramBotService.SendTextMessage(_adminTlgId, $"result.RESPONSE=null");
                }
                else
                {
                    if (result?.RESPONSE.TakeAppointmentResult == null)
                    {
                        await _telegramBotService.SendTextMessage(_adminTlgId, $"result.RESPONSE.TakeAppointmentResult=null");
                    }
                    else
                    {
                        if (result?.RESPONSE.TakeAppointmentResult.Code == null)
                        {
                            await _telegramBotService.SendTextMessage(_adminTlgId, $"result.RESPONSE.TakeAppointmentResult.code=null");
                        }
                        else
                        {
                            if (result.RESPONSE.TakeAppointmentResult.Description == null)
                            {
                                await _telegramBotService.SendTextMessage(_adminTlgId, $"result.RESPONSE.TakeAppointmentResult.code=null");
                            }
                        }

                    }
                }


                var text = $"{result?.RESPONSE?.TakeAppointmentResult.Code}" +
                    $"{result?.RESPONSE?.TakeAppointmentResult.Description}";
                await _telegramBotService.SendTextMessage(_adminTlgId, $"description {client.Email}\n{text}");


                if (result?.RESPONSE.TakeAppointmentResult.Code == 0 && result.RESPONSE.TakeAppointmentResult.Description == "Success")
                {
                    text = $"Twój bilet, to: {result.RESPONSE.AppointmentTicketInfo.TicketNumber}" +
                        $"\nIdentyfikator wizyty :{result.RESPONSE.AppointmentTicketInfo.Code} " +
                        $"\n{result.RESPONSE.AppointmentTicketInfo.AppointmentDay} " +
                        $"{result.RESPONSE.AppointmentTicketInfo.AppointmentTime} " +
                        $"\n{result.RESPONSE.AppointmentTicketInfo.Service.Name} ";
                    client.Result = text;
                    client.DateRegistration = DateTime.Now;
                    client.IsRegistered = true;
                    client.IsActive = false;
                    await _telegramBotService.SendTextMessage(_adminTlgId, $"{client.Email}\n{text}");
                    await _clientService.SaveAsync(client);
                }
            }
            else
            { 
                await _telegramBotService.SendTextMessage(_adminTlgId, $"result == null конец реги .{DateTime.Now.ToString()}");
            }
            await _telegramBotService.SendTextMessage(_adminTlgId, $"конец реги .{DateTime.Now.ToString()}");

            return result;
        }

        private async Task<T?> SendPostRequest<T>(GdanskAppointmentRequestWebModel jsonPayload) where T: class
        {

            await _telegramBotService.SendTextMessage(_adminTlgId, $"5Отправил POST на регу. \n{JsonConvert.SerializeObject(jsonPayload)}");

            var client = _httpClient.CreateClient();
            var formData = new MultipartFormDataContent();
            var jsonString = JsonConvert.SerializeObject(jsonPayload, _jsonSerializerSettings);
            await _telegramBotService.SendTextMessage(_adminTlgId, $"5Отправил POST на регу. \n{JsonConvert.SerializeObject(jsonString)}");

            //formData.Add(new StringContent(jsonString), "JSONForm");
            formData.Add(new StringContent(jsonString, Encoding.UTF8, "application/json"), "JSONForm");

            var response = await client.PostAsync("https://kolejka.gdansk.uw.gov.pl/admin/API/take_appointment", formData);

            return await ProcessHttpResponse<T>(response);
        }

        public async Task<GdanskTimeWebModel?> GetTimeAsync(string date, int BranchID, int ServiceID)
        {
            var client = _httpClient.CreateClient();
            var url = $"https://kolejka.gdansk.uw.gov.pl/admin/API/time/{BranchID}/{ServiceID}/{date}";
            var response = await SendGetRequest<GdanskTimeWebModel>(url);
            return response;
        }
        public async Task<GdanskWebModel?> GetDatesAsync(string url, string code)
        {
            var client = _httpClient.CreateClient();

            try
            {
                var response = await client.GetAsync(url);
                return await ProcessHttpResponse<GdanskWebModel>(response);
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
                await _telegramBotService.SendTextMessage(_adminTlgId, $" error45 {content}");
                await _telegramBotService.SendTextMessage(_adminTlgId, $" error55 {error1}");

                var error = await response.Content.ReadFromJsonAsync<Error>();
                _logger.LogWarning(message: $"HTTP Error {response.StatusCode}: {error?.Message} {error?.ToString()}");
            }
            return null;
        }
    }
}
