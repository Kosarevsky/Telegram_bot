using BezKolejki_bot.Interfaces;
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

        public ISiteProcessor GetProcessor(string url)
        {
            if (url.Contains("bezkolejki"))
            {
                return _serviceProvider.GetRequiredService<BrowserSiteProcessor>();
            }
            else if (url.Contains("https://olsztyn.uw.gov.pl/wizytakartapolaka/pokoj_A1.php"))
            {
                return _serviceProvider.GetRequiredService<OlsztynPostRequestProcessor>();
            }
            else if (url.Contains("https://kolejka.gdansk.uw.gov.pl/admin/API/date/5/304/pl"))
            {
                return _serviceProvider.GetRequiredService<GdanskPostRequestProcessor>();
            }
            throw new NotSupportedException($"No processor found for URL: {url}");
        }
    }
}
