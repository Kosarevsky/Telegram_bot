using BezKolejki_bot.Interfaces;
using BezKolejki_bot.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Services.Interfaces;
using Services.Models;

namespace BezKolejki_bot.Services
{
    public class GdanskQmaticPostRequestProcessor : ISiteProcessor
    {
        private readonly ILogger<GdanskQmaticPostRequestProcessor> _logger;
        private readonly IBezKolejkiService _bezKolejkiService;
        private readonly IHttpService _httpService;
        private readonly ITelegramBotService _telegramBotService;
        private readonly IClientService _clientService;

        public GdanskQmaticPostRequestProcessor(
            ILogger<GdanskQmaticPostRequestProcessor> logger, 
            IBezKolejkiService bezKolejkiService,
            ITelegramBotService telegramBotService,
            IHttpService httpService,
            IClientService clientService)
        {
            _logger = logger;
            _bezKolejkiService = bezKolejkiService;
            _httpService = httpService;
            _telegramBotService = telegramBotService;
            _clientService = clientService;
        }
        private string? GetBranchPublicId(List<GdanskQmaticWebModel?> profiles, string searchName)
        {
            return profiles
                .FirstOrDefault(branch => branch?.serviceGroups
                    .SelectMany(group => group.services)
                    .Any(service => service.name == searchName) == true)
                ?.branchPublicId;
        }

        private GdanskQmaticService? GetUrzand(List<GdanskQmaticWebModel?> profiles, string searchName)
        {
            return profiles
                .SelectMany(g => g?.serviceGroups ?? Enumerable.Empty<GdanskQmaticServiceGroup>())
                .SelectMany(s => s.services)
                .FirstOrDefault(service => service.name == searchName);
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

            var urlAvailable = "https://rezerwacja.gdansk.uw.gov.pl:8445/qmaticwebbooking/rest/schedule/branches/available";
           // var available = await _httpService.SendGetRequest<List<>>(url);

            string searchName = "Składanie wniosków i dokumentacji do wniosków już złożonych w sprawie obywatelstwa polskiego";
            //string searchName = "Wydział Koordynacji Świadczeń";

            var profiles = await _httpService.SendGetRequest<List<GdanskQmaticWebModel?>>(url);
            if (profiles?.Data == null)
            {
                _logger.LogWarning($"{code}.The response content could not be deserialized into GdanskQmaticWebModel.");
                return;
            }

            var service = GetUrzand(profiles.Data, searchName);
            var branchPublicId = GetBranchPublicId(profiles.Data, searchName);
            if (service?.publicId is null || branchPublicId is null)
            {
                _logger.LogWarning($"{code}. Service or branchPublicId not found.");
                return;
            }

            var publicId = service.publicId;

            if (string.IsNullOrEmpty(publicId) || string.IsNullOrEmpty(branchPublicId))
            {
                _logger.LogWarning("The publicId or branchPublicId is empty.");
                return;
            }

            var requestUrl = $"https://rezerwacja.gdansk.uw.gov.pl:8445/qmaticwebbooking/rest/schedule/branches/{branchPublicId}/dates;servicePublicId={publicId};customSlotLength={service.duration}";
            var result = await _httpService.SendGetRequest<List<GdanskQmaticDateWebModel?>>(requestUrl);

            if (result?.Data == null)
            {
                _logger.LogWarning($"{code}.No dates found.");
                return;
            }
            var dates = result.Data
                .Where(d => d?.Date != null)
                .Select(d => d!.Date.ToDateTime(TimeOnly.MinValue))
                .OrderBy(d => d)
                .ToList();
            
            bool dataSaved = false;
            dataSaved = await _bezKolejkiService.ProcessingDate(dataSaved, dates, code);

            if (dates?.Count == 0)
            {
                return;
            }

            return;

            //await _bezKolejkiService.ProcessingDate(false, dates, code);

            // https://rezerwacja.gdansk.uw.gov.pl:8445/qmaticwebbooking/rest/schedule/branches/d677d3884504d5b9eadbfa74c5e29b1a7bb44daf3d2cce7666747b58986d8eaf/services;validate=true
            var serviceUrl = $"https://rezerwacja.gdansk.uw.gov.pl:8445/qmaticwebbooking/rest/schedule/branches/{branchPublicId}/services;validate=true";

            var serviceValidate = await _httpService.SendGetRequest<List<GdanskQmaticServiceWebModel>>(serviceUrl);
          
            if (serviceValidate?.Data == null) {
                return;
            }

            var objService = serviceValidate?.Data
                .FirstOrDefault(d => d.Name == searchName);
            if (objService == null)
            {
                _logger.LogInformation($"{code}. Not found {searchName}");
                return;
            }

            foreach (var date in dates ?? new List<DateTime>())
            {
                var reformattedDate = date.Date.ToString("yyyy-MM-dd");
                var requestTimeUrl = $"https://rezerwacja.gdansk.uw.gov.pl:8445/qmaticwebbooking/rest/schedule/branches/{branchPublicId}/dates/{reformattedDate}/times;servicePublicId={publicId};customSlotLength={service.duration}";
                var responseSlots = await _httpService.SendGetRequest<List<GdanskQmaticDateTimeWebModel?>>(requestTimeUrl);

                List<GdanskQmaticDateTimeWebModel?>? slots = responseSlots?.Data;
                if (slots == null)
                {
                    _logger.LogWarning($"{code}. Slots not found");
                    return;
                }
                foreach (var slot in slots)
                {

                    var payload = createPayload(publicId, searchName, objService);
                    foreach (var client in clients)
                    {
                        processRegistration(branchPublicId, publicId, slot, service.duration, payload, client);

                    }
                }
            }
        }
        private object createPayload(string publicId, string searchName, GdanskQmaticServiceWebModel objService)
        {
            return new
            {
                services = new[]
                {
                    new { publicId }
                },
                custom = JsonConvert.SerializeObject(new
                {
                    peopleServices = new[]
                    {
                        new
                        {
                            publicId,
                            qpId = objService.QpId.ToString(),
                            adult = 1,
                            name = searchName,
                            child = 0
                        }
                    }
                })
            };
        }

        private async Task processRegistration(string branchPublicId, string publicServiceId, GdanskQmaticDateTimeWebModel slot, int customSlotLength, object payload, ClientModel client)
        {
            var reserveUrl = $"https://rezerwacja.gdansk.uw.gov.pl:8445/qmaticwebbooking/rest/schedule/branches/{branchPublicId}/dates/{slot.Date}/times/{slot.Time}/reserve;customSlotLength={customSlotLength}";
            var response = await _httpService.SendPostRequest<GdanskQmaticPostRequestReserveWebModel>(reserveUrl, payload);
            if (response?.Data == null)
            {
                _logger.LogWarning($"Failed to reserve appointment for {slot.Date} at {slot.Time}");
                return;
            }

            // https://rezerwacja.gdansk.uw.gov.pl:8445/qmaticwebbooking/rest/schedule/appointments/checkMultiple;phone=48572282565;email=fedormarinka2000@gmail.com;firstName=Oleg;lastName=Ivanov;branchPublicId=d677d3884504d5b9eadbfa74c5e29b1a7bb44daf3d2cce7666747b58986d8eaf;servicePublicId=8ef795bd2043733338fd5c6fd4ad045b08beb9ec239473b6fa2848b1f14695a9;date=2025-05-06;time=13:00
            var checkMultipleUrl = $"https://rezerwacja.gdansk.uw.gov.pl:8445/qmaticwebbooking/rest/schedule/appointments/checkMultiple;phone={client.PhoneNumber};email={client?.Email?.ToLower()};firstName={client?.Name};lastName={client?.Surname};branchPublicId={branchPublicId};servicePublicId={publicServiceId};date={slot.Date};time={slot.Time}";
            var checkMultipleResponse = await _httpService.SendGetRequest<GdanskQmaticPostRequestCheckMultipleWebModel>(reserveUrl);

            if (checkMultipleResponse?.Data == null)
            {
                _logger.LogWarning($"Failed to check appointment for {slot.Date} at {slot.Time}");
                return;
            }

            if (checkMultipleResponse.Data.IsMultipleOk == false)
            {
                _logger.LogWarning($"Failed to check appointment for {slot.Date} at {slot.Time}");
                return;
            }

            var matchCustomerUrl = $"https://rezerwacja.gdansk.uw.gov.pl:8445/qmaticwebbooking/rest/schedule/matchCustomer";
            
            var objPayload = new
            {
                addressCity = string.Empty,
                addressCountry = string.Empty,
                addressLine1 = string.Empty,
                addressLine2 = string.Empty,
                addressState = string.Empty,
                addressZip = string.Empty,
                custom = "{}",
                dateOfBirth = string.Empty,
                email = client?.Email?.ToLower() ?? string.Empty,
                externalId = string.Empty,
                firstName = client?.Name ?? string.Empty,
                lastName = client?.Surname ?? string.Empty,
                phone = client?.PhoneNumber ?? string.Empty,
            };
            var matchCustomerResponse = await _httpService.SendPostRequest<GdanskQmaticPostRequestMatchCustomerWebModel>(matchCustomerUrl, objPayload);
            if (matchCustomerResponse?.Data == null)
            {
                _logger.LogWarning($"Failed to match customer for {slot.Date} at {slot.Time}");
                return;
            }
            if (matchCustomerResponse.Data.allowOverwrite == false)
            {
                _logger.LogWarning($"Failed to match customer for {slot.Date} at {slot.Time} allowOverwrite=false");
                return;
            }

        }
    }
}
