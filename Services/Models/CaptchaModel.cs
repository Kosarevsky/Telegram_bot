using Microsoft.ML.Data;

namespace BezKolejki_bot.Models
{
    public class CaptchaData
    {
        [LoadColumn(0)]
        public string ImagePath { get; set; } = string.Empty;
        [LoadColumn(1)]
        public string Label { get; set; } = string.Empty;
    }
    public class CaptchaPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedLabel { get; set; } = string.Empty;
    }

}
