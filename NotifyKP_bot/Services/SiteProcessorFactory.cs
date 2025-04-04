using BezKolejki_bot.Interfaces;
using BezKolejki_bot.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BezKolejki_bot.Services
{
    public class SiteProcessorFactory : ISiteProcessorFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, Func<SiteProcessorResult>> _processors;

        public SiteProcessorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _processors = new Dictionary<string, Func<SiteProcessorResult>>
        {
            { "bezkolejki", () => new SiteProcessorResult(_serviceProvider.GetRequiredService<BrowserSiteProcessor>(), "") },
            { "https://olsztyn.uw.gov.pl/wizytakartapolaka/pokoj_A1.php", () => new SiteProcessorResult(_serviceProvider.GetRequiredService<OlsztynPostRequestProcessor>(), "/OlsztynKP") },
            { "https://kolejka.gdansk.uw.gov.pl/admin/API/date/5/304/pl", () => new SiteProcessorResult(_serviceProvider.GetRequiredService<GdanskPostRequestProcessor>(), "/Gdansk01") },
            { "https://rezerwacja.gdansk.uw.gov.pl:8445/qmaticwebbooking/", () => new SiteProcessorResult(_serviceProvider.GetRequiredService<GdanskQmaticPostRequestProcessor>(), "/Gdansk02") },
            { "https://kolejka.gdansk.uw.gov.pl/admin/API/date/8/198/pl", () => new SiteProcessorResult(_serviceProvider.GetRequiredService<GdanskPostRequestProcessor>(), "/Slupsk01") },
            { "https://kolejka.gdansk.uw.gov.pl/admin/API/date/8/202/pl", () => new SiteProcessorResult(_serviceProvider.GetRequiredService<GdanskPostRequestProcessor>(), "/Slupsk02") },
            { "https://kolejka.gdansk.uw.gov.pl/admin/API/date/8/199/pl", () => new SiteProcessorResult(_serviceProvider.GetRequiredService<GdanskPostRequestProcessor>(), "/Slupsk03") },
            { "https://api.e-konsulat.gov.pl/api/rezerwacja-wizyt-karta-polaka/terminy/1769/1", () => new SiteProcessorResult(_serviceProvider.GetRequiredService<MoskwaKpPostRequestProcessor>(), "/MoskwaKP") },
            { "https://api.e-konsulat.gov.pl/api/rezerwacja-wizyt-karta-polaka/terminy/416/1", () => new SiteProcessorResult(_serviceProvider.GetRequiredService<MoskwaKpPostRequestProcessor>(), "/AlmatyKP") }
        };
        }

        public SiteProcessorResult GetProcessor(string url)
        {
            foreach (var entry in _processors)
            {
                if (url.Contains(entry.Key))
                {
                    return entry.Value();
                }
            }

            throw new NotSupportedException($"No processor found for URL: {url}");
        }
    }
}
