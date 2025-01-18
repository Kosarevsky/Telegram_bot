namespace BezKolejki_bot.Models
{
    public class GdanskWebModel
    {
        public string MSG { get; set; } = string.Empty;
        public string MSG_FULL { get; set; } = string.Empty;
        public IList<string> DATES { get; set; } = new List<string>();
    }
}
