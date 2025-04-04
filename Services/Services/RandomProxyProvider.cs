using Microsoft.Extensions.Configuration;
using Services.Interfaces;
using System.Net;

namespace Services.Services
{
    public class RandomProxyProvider : IProxyProvider
    {
        private readonly List<string> _proxyList;
        private readonly Random _random = new();

        public RandomProxyProvider(IConfiguration configuration)
        {
            _proxyList = configuration.GetSection("ProxySettings:Proxies").Get<List<string>>() ?? new List<string>();
        }

        public WebProxy GetRandomProxy()
        {
            if (_proxyList.Count == 0)
                throw new InvalidOperationException("Proxy list is empty.");

            var proxyString = _proxyList[_random.Next(_proxyList.Count)];

            var uri = new Uri(proxyString);
            var proxy = new WebProxy($"{uri.Scheme}://{uri.Host}:{uri.Port}");

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var userInfo = uri.UserInfo.Split(':');
                if (userInfo.Length == 2)
                {
                    proxy.Credentials = new NetworkCredential(userInfo[0], userInfo[1]);
                }
            }

            return proxy;
        }
    }
}
