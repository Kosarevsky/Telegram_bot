using BezKolejki_bot.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BezKolejki_bot.Services
{
    public class BrowserAutomationService : ISiteProcessor
    {
        private readonly ILogger<BrowserAutomationService> _logger;
        private readonly SiteProcessorFactory _siteProcessorFactory;
        private readonly ConcurrentDictionary<string, bool> _processingSite = new();

        public BrowserAutomationService(
            ILogger<BrowserAutomationService> logger,
            SiteProcessorFactory siteProcessorFactory)
        {
            _logger = logger;
            _siteProcessorFactory = siteProcessorFactory;
        }

        public async Task GetAvailableDateAsync(IEnumerable<string> urls)
        {
            var tasks = urls.Select(url => ProcessSiteAsync(url)).ToList();
            await Task.WhenAll(tasks);
        }

        public Task ProcessSiteAsync(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task ProcessSiteAsync(string url)
        {
            if (_processingSite.ContainsKey(url))
            {
                _logger.LogInformation($"Site '{url}' is already being processed. Skipping...");
                return;
            }
            _processingSite.TryAdd(url, true);

            try
            {
                var processor = _siteProcessorFactory.GetProcessor(url);
                await processor.ProcessSiteAsync(url, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing site: {url}");
            }
            finally
            {
                _processingSite.TryRemove(url, out _);
            }
        }
    }

}
