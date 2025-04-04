using BezKolejki_bot.Interfaces;
using BezKolejki_bot.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenQA.Selenium.DevTools.V85.ApplicationCache;
using Services.Interfaces;
using static System.Net.WebRequestMethods;

namespace BezKolejki_bot.Services
{
    public class GdanskQmaticPostRequestProcessor : ISiteProcessor
    {
        private readonly ILogger<GdanskQmaticPostRequestProcessor> _logger;
        private readonly IBezKolejkiService _bezKolejkiService;
        private readonly IHttpService _httpService;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public GdanskQmaticPostRequestProcessor(
            ILogger<GdanskQmaticPostRequestProcessor> logger, 
            IBezKolejkiService bezKolejkiService,
            ITelegramBotService telegramBotService,
            IHttpService httpService)
        {
            _logger = logger;
            _bezKolejkiService = bezKolejkiService;
            _httpService = httpService;

            _jsonSerializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };
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
            var urlAvailable = "https://rezerwacja.gdansk.uw.gov.pl:8445/qmaticwebbooking/rest/schedule/branches/available";
           // var available = await _httpService.SendGetRequest<List<>>(url);

            string searchName = "Składanie wniosków i dokumentacji do wniosków już złożonych w sprawie obywatelstwa polskiego";
            //string searchName = "Wydział Koordynacji Świadczeń";
            var countByActiveUsers = await _bezKolejkiService.GetCountActiveUsersByCode(code);
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
                .ToList(); ;
            
            bool dataSaved = false;
            dataSaved = await _bezKolejkiService.ProcessingDate(dataSaved, dates, code);

            if (dates?.Count == 0)
            {
                return;
            }


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

            foreach (var date in dates)
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

                    var payload = new
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
                                    name = "Wydział Koordynacji Świadczeń",
                                    child = 0
                                }
                            }
                        })
                    };


                    processRegistration(branchPublicId, slot, service.duration);
                }
            }




        }

        private void processRegistration(string branchPublicId,  GdanskQmaticDateTimeWebModel slot, int customSlotLength)
        {
            var reserveUrl = $"https://rezerwacja.gdansk.uw.gov.pl:8445/qmaticwebbooking/rest/schedule/branches/{branchPublicId}/dates/{slot.Date}/times/{slot.Time}/reserve;customSlotLength={customSlotLength}";
        }
    }
}
