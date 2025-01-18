using Newtonsoft.Json;

namespace Services.Models
{
    public class BezKolejkiJsonModel
    {
        [JsonProperty("operationId")]
        public int operationId { get; set; }

        [JsonProperty("availableDays")]
        public List<string> availableDays { get; set; } = new List<string>();

        [JsonProperty("disabledDays")]
        public IList<DisabledDays> disabledDays { get; set; } = [];

        [JsonProperty("minDate")]
        public string minDate { get; set; } = string.Empty;

        [JsonProperty("maxDate")]
        public string maxDate { get; set; } = string.Empty;
    }
    public class DisabledDays
    {
        [JsonProperty("start")]
        public string start { get; set; } = string.Empty;

        [JsonProperty("end")]
        public string end { get; set; } = string.Empty;

    }
}
