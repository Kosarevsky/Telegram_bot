using System.Net;

namespace Services
{
    public class GetRequest
    {
        private HttpClient _client;
        readonly string _address;
        public string UserAgent { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string Cookie { get; set; }
        public string Accept { get; set; }
        public string Host { get; set; }
        public GetRequest(string address)
        {
            _address = address;
            _client = new HttpClient();
            Headers = new Dictionary<string, string>();
        }

        public async void Run()
        {
            try
            {
                var response = await _client.GetAsync(_address);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseBody);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException");
                Console.WriteLine(e.Message);
            }
        }

        public async Task<string> Run(CookieContainer cookieContainer)
        {
            var baseAddress = new Uri(_address);
            var responseBody = string.Empty;
            try
            {
                using (var handler = new HttpClientHandler()
                {
                    CookieContainer = cookieContainer,
                    AllowAutoRedirect = false,
                    AutomaticDecompression = DecompressionMethods.All,
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
                })

                using (_client = new HttpClient(handler))
                {
                    _client.BaseAddress = baseAddress;
                    //_client.DefaultRequestHeaders.Add("Accept", Accept);
                    //_client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    //_client.DefaultRequestHeaders.Add("Host", Host);
                    _client.DefaultRequestHeaders.Add("Cookie", Cookie);
                    var message = new HttpRequestMessage(HttpMethod.Get, baseAddress.ToString());
                    //foreach (var item in Headers) { message.Headers.Add(item.Key, item.Value); }

                    var result = await _client.SendAsync(message);
                    result.EnsureSuccessStatusCode();

                    responseBody = await result.Content.ReadAsStringAsync();
                    //Console.WriteLine(responseBody);
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException");
                Console.WriteLine(e.Message);
            }
            return responseBody;
        }

    }
}
