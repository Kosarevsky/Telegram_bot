using Services.Interfaces;
using Services.Models;

namespace Services.Services
{
    public class LocalizationService : ILocalizationService
    {
        public string GetText(string language, string key)
        {
            if (string.IsNullOrEmpty(language)) { language = "en"; }
            return Localization.Texts[language][key] ?? key;
        }
    }
}
