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
        public string Date { get; set; }

        [JsonPropertyName("time")] // Указываем, что это свойство соответствует полю "time" в JSON
        public string Time { get; set; }
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

}
