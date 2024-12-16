using BezKolejki_bot.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BezKolejki_bot.Services
{
    public class SiteProcessorFactory
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
                return _serviceProvider.GetRequiredService<PostRequestProcessor>();
            }
            throw new NotSupportedException($"No processor found for URL: {url}");
        }
    }

}
