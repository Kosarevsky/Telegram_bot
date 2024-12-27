using BezKolejki_bot.Interfaces;

namespace BezKolejki_bot.Models
{
    public class SiteProcessorResult
    {
        public ISiteProcessor Processor { get; set; }
        public string Code { get; set; }
        public SiteProcessorResult(ISiteProcessor processor, string code)
        {
            Processor = processor;
            Code = code;
        }
    }
}
