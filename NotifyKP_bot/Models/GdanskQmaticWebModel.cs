using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace BezKolejki_bot.Models
{
    public class GdanskQmaticWebModel
    {
        public string branchName { get; set; } = string.Empty;
        public string branchPublicId { get; set; } = string.Empty;
        public List<GdanskQmaticServiceGroup> serviceGroups { get; set; } = new List<GdanskQmaticServiceGroup>();
    }
    public class GdanskQmaticService
    {
        public string publicId { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public int duration { get; set; }
        public int additionalCustomerDuration { get; set; }
        public string custom { get; set; } = string.Empty;
    }

    public class GdanskQmaticServiceGroup
    {
        public List<GdanskQmaticService> services { get; set; } = new List<GdanskQmaticService>();
    }

    public class GdanskQmaticDateWebModel
    {
        public DateOnly Date { get; set; }
    }

    public class GdanskQmaticDateTimeWebModel
    {
        [JsonPropertyName("date")] // Указываем, что это свойство соответствует полю "date" в JSON
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("time")] // Указываем, что это свойство соответствует полю "time" в JSON
        public string Time { get; set; } = string.Empty;
    }

    public class GdanskQmaticServiceWebModel
    {
        [JsonPropertyName("additionalDuration")]
        public int AdditionalDuration { get; set; }

        [JsonPropertyName("custom")]
        public string Custom { get; set; } = string.Empty;

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("internalId")]
        public int InternalId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("publicId")]
        public string PublicId { get; set; } = string.Empty;

        [JsonPropertyName("qpId")]
        public string QpId { get; set; } = string.Empty;
    }
    public class GdanskQmaticPostRequestReserveWebModel
    {
        [JsonProperty("allday")]
        public bool AllDay { get; set; }

        [JsonProperty("appId")]
        public string AppId { get; set; }

        [JsonProperty("blocking")]
        public bool Blocking { get; set; }

        [JsonProperty("branch")]
        public GdanskQmaticPostRequestReserveBranchWebModel Branch { get; set; }

        [JsonProperty("branchName")]
        public string BranchName { get; set; }

        [JsonProperty("created")]
        public long Created { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("peopleServices")]
        public List<object> PeopleServices { get; set; } = new List<object>();

        [JsonProperty("publicBranchId")]
        public string PublicBranchId { get; set; }

        [JsonProperty("publicId")]
        public string PublicId { get; set; }

        [JsonProperty("publicServiceId")]
        public string PublicServiceId { get; set; }

        [JsonProperty("serviceName")]
        public string ServiceName { get; set; }

        [JsonProperty("services")]
        public List<GdanskQmaticPostRequestReserveServiceWebModel> Services { get; set; } = new List<GdanskQmaticPostRequestReserveServiceWebModel>();

        [JsonProperty("startTime")]
        public string StartTime { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("time")]
        public string Time { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        // Дополнительные вычисляемые свойства
        public DateTime CreatedDateTime => DateTimeOffset.FromUnixTimeMilliseconds(Created).DateTime;
        public DateTime StartDateTime => DateTime.Parse(StartTime);
    };

    public class GdanskQmaticPostRequestReserveServiceWebModel
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("publicId")]
        public string PublicId { get; set; } = string.Empty;
    }
    public class GdanskQmaticPostRequestReserveBranchWebModel
    {
        [JsonProperty("addressCity")]
        public string AddressCity { get; set; } = string.Empty;

        [JsonProperty("addressCountry")]
        public string AddressCountry { get; set; } = string.Empty;

        [JsonProperty("addressLine1")]
        public string AddressLine1 { get; set; } = string.Empty;

        [JsonProperty("addressLine2")]
        public string AddressLine2 { get; set; } = string.Empty;

        [JsonProperty("addressZip")]
        public string AddressZip { get; set; } = string.Empty;

        [JsonProperty("custom")]
        public string Custom { get; set; } = string.Empty;

        [JsonProperty("email")]
        public string Email { get; set; } = string.Empty;

        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("internalId")]
        public int InternalId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("phone")]
        public string Phone { get; set; } = string.Empty;

        [JsonProperty("qpId")]
        public string QpId { get; set; } = string.Empty;

        [JsonProperty("timeZone")]
        public string TimeZone { get; set; } = string.Empty;
    };
    public class GdanskQmaticPostRequestCheckMultipleWebModel
    {
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        public bool IsMultipleOk => Message == "MULTIPLE_OK";
    }

    public class GdanskQmaticPostRequestMatchCustomerWebModel 
    {
        [JsonProperty("message")]
        public bool allowOverwrite { get; set; } = false;
    }
}