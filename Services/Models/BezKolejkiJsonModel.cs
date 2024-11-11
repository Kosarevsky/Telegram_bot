using Newtonsoft.Json;

namespace Services.Models
{
    public class BezKolejkiJsonModel
    {
        [JsonProperty("operationId")]
        public int operationId { get; set; }

        [JsonProperty("availableDays")]
        public List<string> availableDays { get; set; }

        [JsonProperty("disabledDays")]
        public IList<DisabledDays> disabledDays { get; set; }

        [JsonProperty("minDate")]
        public string minDate { get; set; }

        [JsonProperty("maxDate")]
        public string maxDate { get; set; }
    }
    public class DisabledDays
    {
        [JsonProperty("start")]
        public string start { get; set; }

        [JsonProperty("end")]
        public string end { get; set; }

    }
}
