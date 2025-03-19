using BezKolejki_bot.Interfaces;
using BezKolejki_bot.Models;
using Microsoft.Extensions.DependencyInjection;

namespace BezKolejki_bot.Services
{
    public class SiteProcessorFactory : ISiteProcessorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public SiteProcessorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public SiteProcessorResult GetProcessor(string url)
        {
            if (url.Contains("bezkolejki"))
            {
                return new SiteProcessorResult(_serviceProvider.GetRequiredService<BrowserSiteProcessor>(), "");
            }
            else if (url.Contains("https://olsztyn.uw.gov.pl/wizytakartapolaka/pokoj_A1.php"))
            {
                return new SiteProcessorResult(_serviceProvider.GetRequiredService<OlsztynPostRequestProcessor>(), "/OlsztynKP");
            }
            else if (url.Contains("https://kolejka.gdansk.uw.gov.pl/admin/API/date/5/304/pl")) 
            {
                return new SiteProcessorResult(_serviceProvider.GetRequiredService<GdanskPostRequestProcessor>(), "/Gdansk01");
            }
            else if (url.Contains("https://rezerwacja.gdansk.uw.gov.pl:8445/qmaticwebbooking/"))
            {
                return new SiteProcessorResult(_serviceProvider.GetRequiredService<GdanskQmaticPostRequestProcessor>(), "/Gdansk02");
            }
            else if (url.Contains("https://kolejka.gdansk.uw.gov.pl/admin/API/date/8/198/pl")) 
            {
                return new SiteProcessorResult(_serviceProvider.GetRequiredService<GdanskPostRequestProcessor>(), "/Slupsk01");
            }
            else if (url.Contains("https://kolejka.gdansk.uw.gov.pl/admin/API/date/8/202/pl"))
            {
                return new SiteProcessorResult(_serviceProvider.GetRequiredService<GdanskPostRequestProcessor>(), "/Slupsk02");
            }
            else if (url.Contains("https://kolejka.gdansk.uw.gov.pl/admin/API/date/8/199/pl"))
            {
                return new SiteProcessorResult(_serviceProvider.GetRequiredService<GdanskPostRequestProcessor>(), "/Slupsk03");
            }
            else if (url.Contains("https://api.e-konsulat.gov.pl/api/rezerwacja-wizyt-karta-polaka/terminy/1769/1"))
            {
                return new SiteProcessorResult(_serviceProvider.GetRequiredService<MoskwaKpPostRequestProcessor>(), "/MoskwaKP");
            }
            else if (url.Contains("https://api.e-konsulat.gov.pl/api/rezerwacja-wizyt-karta-polaka/terminy/416/1"))
            {
                return new SiteProcessorResult(_serviceProvider.GetRequiredService<MoskwaKpPostRequestProcessor>(), "/AlmatyKP");
            }
            throw new NotSupportedException($"No processor found for URL: {url}");
        }
    }
}
