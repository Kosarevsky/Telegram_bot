namespace Services.Interfaces
{
    public interface ILocalizationService
    {
        public string GetText(string language, string key);
    }
}
